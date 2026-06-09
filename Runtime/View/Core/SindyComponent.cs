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
        public IViewModel Model { get; protected set; }
        public ComponentPreset Preset { get; set; }
        protected readonly List<IDisposable> disposables = new();
        private readonly SindyComponentNamedHandleStore handles = new();
        private SindyComponentLinkState links;
        internal SindyComponentLinkState LinkState => links ??= new(this);
        private readonly SindyComponentDeferredActionQueue deferredActions = new();
        private bool isInitialized = false;
        public bool IsInitialized => isInitialized;

        protected static bool IsComponentPrefab(SindyComponent com) => string.IsNullOrEmpty(com.gameObject.scene.name);
        public bool IsPrefab => IsComponentPrefab(this);

        /// <summary>
        /// 주어진 모델을 이 컴포넌트에 바인딩합니다. 모델이 이전과 동일한 경우 초기화 로직을 건너뜁니다.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public virtual SindyComponent Bind(IViewModel model)
        {
            if (isInitialized && ReferenceEquals(model, Model))
            {
                return this;
            }

            isInitialized = true;
            ClearModel();
            Model = model;
            if (Model != null)
            {
                BindCommonFeatures(Model);
                Init(Model);
            }

            return this;
        }

        /// <summary>
        /// 현재 모델을 초기화 상태로 다시 로드합니다. 모델이 변경되지 않았지만 내부 상태가 업데이트되어야 하는 경우에 유용합니다.
        /// </summary>
        public void Reload()
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
                child.Bind(null);
            }

            LinkState.ClearChildrenLinks();
            LinkState.DetachFromParent();
        }

        protected virtual void Init(object model) { }
        protected virtual void Clear(object model) { }

        /// <summary>
        /// 모델이 ViewModel인 경우 공통 Feature(Visibility, Interactable)를 자동 바인딩합니다.
        /// 개별 컴포넌트에서 이 Feature들을 직접 처리하는 경우 오버라이드하여 비활성화할 수 있습니다.
        /// </summary>
        protected virtual void BindCommonFeatures(IViewModel viewModel)
        {
            var visibility = viewModel.Feature<VisibilityFeature>();
            visibility?.Show.Subscribe(v => gameObject.SetActive(v)).AddTo(disposables);

            var layout = viewModel.Feature<LayoutFeature>();
            layout?.Apply(transform as RectTransform);
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

    public abstract class SindyComponent<T> : SindyComponent where T : class, IViewModel
    {
        public new T Model
        {
            get => base.Model as T;
            protected set => base.Model = value;
        }

        public override SindyComponent Bind(IViewModel model)
        {
            if (model == null || model is T)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                ValidateSupportedFeatures((T)model);
#endif
                SetModel((T)model);
            }
            else
            {
                throw new ArgumentException($"{GetType()} Model must be of type {typeof(T).Name} but was {model.GetType().Name}", nameof(model));
            }
            return this;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void ValidateSupportedFeatures(T model)
        {
            if (model == null) return;

            var declared = new HashSet<Type>
            {
                typeof(VisibilityFeature),
                typeof(LayoutFeature),
            };
            foreach (var raw in GetType().GetCustomAttributes(typeof(SupportedFeatureAttribute), inherit: true))
            {
                if (raw is SupportedFeatureAttribute attr && attr.FeatureType != null)
                    declared.Add(attr.FeatureType);
            }

            foreach (var featureType in model.GetFeatureTypes())
            {
                if (!declared.Contains(featureType))
                {
                    Debug.LogWarning($"[{GetType().Name}] Feature {featureType.Name}는 SupportedFeatureAttribute로 선언되지 않았습니다.", this);
                }
            }
        }
#endif
        public virtual SindyComponent SetModel(T model)
        {
            base.Bind(model);
            return this;
        }
        protected abstract void Init(T model);
        protected override void Init(object model) => Init(model as T);

        protected virtual void Clear(T model) { }
        protected override void Clear(object model) => Clear(model as T);
    }
}
