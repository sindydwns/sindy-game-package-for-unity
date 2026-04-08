using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sindy.View
{
    public class SindyComponent : MonoBehaviour
    {
        public ComponentPreset Preset { get; set; }
        protected readonly List<IDisposable> disposables = new();
        private readonly Dictionary<string, IDisposable> handles = new();
        public object Model { get; protected set; }
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;
        /// <summary>
        /// 자신의 모델이 null이 되면 자식 컴포넌트들의 모델도 null이 되도록 설정한 컴포넌트들.
        /// </summary>
        private HashSet<SindyComponent> children;
        private SindyComponent parent = null;

        protected static bool IsComponentPrefab(SindyComponent com) => string.IsNullOrEmpty(com.gameObject.scene.name);
        public bool IsPrefab => IsComponentPrefab(this);

        public virtual SindyComponent SetModel(object model)
        {
            if (isInitialized && model == Model)
            {
                return this;
            }
            isInitialized = true;
            ClearModel();
            Model = model;
            if (Model != null)
            {
                Init(Model);
            }
            return this;
        }
        public void ReloadModel()
        {
            ClearModel();
            if (Model != null)
            {
                Init(Model);
            }
        }
        private void ClearModel()
        {
            if (Model == null) return;
            Clear(Model);
            ClearDisposables();
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.SetModel(null);
                }
            }
            RemoveChildrenLink();
        }
        protected virtual void Init(object model) { }
        protected virtual void Clear(object model) { }

        protected void ClearDisposables()
        {
            for (int i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
            disposables.Clear();
            foreach (var patch in handles.Values)
            {
                patch.Dispose();
            }
            handles.Clear();
        }

        private void OnDestroy()
        {
            ClearModel();
            Model = null;
        }

        private List<(Action action, float delay)> waitCoroutineActions = new();
        protected void WaitCoroutine(Action action, float delay = 0)
        {
            if (gameObject.activeSelf)
            {
                StartCoroutine(WaitCoroutine_Cor(action, delay));
            }
            else
            {
                waitCoroutineActions.Add((action, delay));
            }
        }

        protected void OnEnable()
        {
            foreach (var (action, delay) in waitCoroutineActions)
            {
                StartCoroutine(WaitCoroutine_Cor(action, delay));
            }
            waitCoroutineActions.Clear();
        }

        private IEnumerator WaitCoroutine_Cor(Action action, float delay = 0)
        {
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }
            else
            {
                yield return null;
            }
            action?.Invoke();
        }

        public void SetParent(SindyComponent parent)
        {
            SetParentNull();
            this.parent = parent;
            if (parent != null)
            {
                this.parent.children ??= new();
                this.parent.children.Add(this);
            }
        }

        private SindyComponent SetParentNull()
        {
            if (parent != null)
            {
                parent.children.Remove(this);
                parent = null;
            }
            return this;
        }

        private void RemoveChildrenLink()
        {
            if (children == null) return;
            foreach (var child in children)
            {
                child.parent = null;
            }
            children.Clear();
        }

        public T AddHandle<T>(T patch, string name = default) where T : IDisposable
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GeneratePatchName(patch);
            }
            if (handles.ContainsKey(name))
            {
                handles[name].Dispose();
                disposables.Remove(handles[name]);
                handles.Remove(name);
            }
            handles[name] = patch;
            return patch;
        }

        public T GetHandle<T>(string name) where T : IDisposable
        {
            if (handles.TryGetValue(name, out var patch))
            {
                return (T)patch;
            }
            return default;
        }

        private string GeneratePatchName<T>(T disposable) where T : IDisposable => $"{typeof(T).Name}_{disposable.GetHashCode()}";
    }

    public abstract class SindyComponent<T> : SindyComponent where T : class
    {
        public new T Model
        {
            get => base.Model as T;
            protected set => base.Model = value;
        }

        public override SindyComponent SetModel(object model)
        {
            if (model == null || model is T)
            {
                SetModel((T)model);
            }
            else
            {
                throw new ArgumentException($"{GetType()} Model must be of type {typeof(T).Name} but was {model.GetType().Name}", nameof(model));
            }
            return this;
        }
        public virtual SindyComponent SetModel(T model)
        {
            base.SetModel(model);
            return this;
        }
        protected abstract void Init(T model);
        protected override void Init(object model) => Init(model as T);

        protected virtual void Clear(T model) { }
        protected override void Clear(object model) => Clear(model as T);
    }
}
