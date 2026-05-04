using System;
using System.Collections;
using System.Collections.Generic;
using R3;
using Sindy.Common;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Events;

namespace Sindy.View
{
    public class SindyComponent : MonoBehaviour
    {
        public object Model { get; protected set; }
        public ComponentPreset Preset { get; set; }
        protected readonly List<IDisposable> disposables = new();
        private readonly SindyComponentNamedHandleStore handles = new();
        private SindyComponentLinkState links;
        internal SindyComponentLinkState LinkState => links ??= new(this);
        private readonly SindyComponentDeferredActionQueue deferredActions = new();
        private Dictionary<Type, ViewModelFeature> features;
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

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
                BindCommonFeatures();
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
            if (Model != null)
            {
                Clear(Model);
            }

            ClearDisposables();

            foreach (var child in LinkState.GetChildrenSnapshot())
            {
                child.SetModel(null);
            }

            LinkState.ClearChildrenLinks();
            LinkState.DetachFromParent();
        }

        protected virtual void Init(object model) { }
        protected virtual void Clear(object model) { }

        /// <summary>
        /// 컴포넌트에 Feature를 부착합니다. 동일 타입이 이미 존재하면 기존 Feature는 Dispose되고 교체됩니다.
        /// 부착된 Feature는 컴포넌트가 파괴될 때 함께 Dispose됩니다.
        /// </summary>
        public SindyComponent With<T>(T feature) where T : ViewModelFeature
        {
            features ??= new();
            if (features.TryGetValue(typeof(T), out var existing) && existing != feature)
            {
                existing.Dispose();
            }
            features[typeof(T)] = feature;
            return this;
        }

        /// <summary>
        /// 컴포넌트에 부착된 Feature를 조회합니다. 컴포넌트에 없으면 모델(ViewModel)의 Feature를 조회합니다.
        /// </summary>
        public T Feature<T>() where T : ViewModelFeature
        {
            if (features != null && features.TryGetValue(typeof(T), out var f))
                return (T)f;
            if (Model is ViewModel viewModel)
                return viewModel.Feature<T>();
            return default;
        }

        private void DisposeComponentFeatures()
        {
            if (features == null) return;
            foreach (var f in features.Values)
            {
                f?.Dispose();
            }
            features.Clear();
        }

        /// <summary>
        /// 컴포넌트와 모델 양쪽에서 공통 Feature(Visibility, Layout)를 자동 바인딩합니다.
        /// 컴포넌트에 부착된 Feature가 모델 Feature보다 우선하며, 모델이 ViewModel이 아니어도
        /// (또는 아무 Feature도 부착되지 않아도) 안전하게 호출됩니다.
        /// 개별 컴포넌트에서 이 Feature들을 직접 처리하는 경우 오버라이드하여 비활성화할 수 있습니다.
        /// </summary>
        protected virtual void BindCommonFeatures()
        {
            var visibility = Feature<VisibilityFeature>();
            if (visibility != null)
            {
                visibility.Show.Subscribe(v => gameObject.SetActive(v)).AddTo(disposables);
            }

            var layout = Feature<LayoutFeature>();
            if (layout != null)
            {
                layout.Apply(transform as RectTransform);
            }
        }

        protected void ClearDisposables()
        {
            disposables.DisposeAllClear();
            handles.Clear();
        }

        protected virtual void OnDestroy()
        {
            ClearModel();
            Model = null;
            DisposeComponentFeatures();
        }

        protected void WaitCoroutine(Action action, float delay = 0)
        {
            if (gameObject.activeSelf)
            {
                StartCoroutine(WaitCoroutine_Cor(action, delay));
            }
            else
            {
                deferredActions.Enqueue(action, delay);
            }
        }

        protected virtual void OnEnable()
        {
            foreach (var (action, delay) in deferredActions.Drain())
            {
                StartCoroutine(WaitCoroutine_Cor(action, delay));
            }
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
            LinkState.AttachTo(parent);
        }

        public T AddHandle<T>(T handle, string name = default) where T : IDisposable => handles.Add(handle, name);
        public T GetHandle<T>(string name) where T : IDisposable => handles.Get<T>(name);

        /// <summary>
        /// UnityEvent에 리스너를 등록하고 disposables에 해제 로직을 추가합니다.
        /// </summary>
        protected void BindUnityEvent<T>(UnityEvent<T> unityEvent, UnityAction<T> handler)
        {
            unityEvent.AddListener(handler);
            disposables.Add(Disposable.Create(() => unityEvent.RemoveListener(handler)));
        }
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
