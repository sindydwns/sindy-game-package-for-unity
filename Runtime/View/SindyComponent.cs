using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sindy.View
{
    public class SindyComponent : MonoBehaviour
    {
        public ComponentPreset Preset { get; set; }
        protected List<IDisposable> disposables = new();
        private Dictionary<string, IDisposable> patches = new();
        public object Model { get; protected set; }
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;
        /// <summary>
        /// 자신의 모델이 null이 되면 자식 컴포넌트들의 모델도 null이 되도록 설정한 컴포넌트들.
        /// </summary>
        private HashSet<SindyComponent> linkChildren;
        private SindyComponent linkParent = null;

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
            if (linkChildren != null)
            {
                foreach (var child in linkChildren)
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
            foreach (var patch in patches.Values)
            {
                patch.Dispose();
            }
            patches.Clear();
        }

        private void OnDestroy()
        {
            if (Model != null)
            {
                Clear(Model);
                Model = null;
            }
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

        public void AddTo(SindyComponent parent)
        {
            RemoveLink();
            linkParent = parent;
            linkParent.linkChildren ??= new();
            linkParent.linkChildren.Add(this);
        }

        public SindyComponent RemoveLink()
        {
            if (linkParent != null)
            {
                linkParent.linkChildren.Remove(this);
                linkParent = null;
            }
            return this;
        }

        private void RemoveChildrenLink()
        {
            if (linkChildren == null) return;
            foreach (var child in linkChildren)
            {
                child.linkParent = null;
            }
            linkChildren.Clear();
        }

        public T AddPatch<T>(T patch, string name = default) where T : IDisposable
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GeneratePatchName(patch);
            }
            if (patches.ContainsKey(name))
            {
                patches[name].Dispose();
                disposables.Remove(patches[name]);
                patches.Remove(name);
            }
            patches[name] = patch;
            return patch;
        }

        public T GetPatch<T>(string name) where T : IDisposable
        {
            if (patches.TryGetValue(name, out var patch))
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

        protected abstract void Clear(T model);
        protected override void Clear(object model) => Clear(model as T);
    }
}
