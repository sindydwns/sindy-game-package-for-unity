using R3;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// FeatureView 아키텍처의 허브.
    ///
    /// 모델(<see cref="IViewModel"/>)을 <see cref="ReactiveProperty{T}"/>로 보유하며,
    /// 같은 GameObject에 부착된 <see cref="FeatureView{TFeature}"/>들이 이 스트림을 구독한다.
    /// 허브는 FeatureView의 존재를 모른다 (릴리스 빌드 기준 순수 pub/sub).
    ///
    /// - 필드 초기화이므로 Awake 실행 여부와 무관하게 비활성 상태에서도 Bind가 동작한다.
    /// - 같은 인스턴스 재바인딩은 방출되지 않는다(same-instance 스킵). 강제 재초기화는 <see cref="Reload"/>.
    /// - <see cref="SetParent"/>로 부모에 연결하면 부모 재바인딩/파괴 시 자식이 연쇄적으로 Bind(null)된다.
    /// </summary>
    public class SindyComponent : MonoBehaviour
    {
        private readonly ReactiveProperty<IViewModel> model = new();

        /// <summary>모델 스트림 (읽기 전용). FeatureView가 구독한다.</summary>
        public ReadOnlyReactiveProperty<IViewModel> Model => model;

        /// <summary>현재 바인딩된 모델.</summary>
        public IViewModel CurrentModel => model.Value;

        public ComponentPreset Preset { get; set; }

        private SindyComponentLinkState links;
        internal SindyComponentLinkState LinkState => links ??= new(this);

        protected static bool IsComponentPrefab(SindyComponent com) => string.IsNullOrEmpty(com.gameObject.scene.name);
        public bool IsPrefab => IsComponentPrefab(this);

        /// <summary>
        /// 주어진 모델을 이 컴포넌트에 바인딩합니다.
        /// 모델이 이전과 동일한 인스턴스인 경우 아무 일도 일어나지 않습니다.
        /// </summary>
        public SindyComponent Bind(IViewModel newModel)
        {
            if (ReferenceEquals(newModel, model.Value))
            {
                return this;
            }

            // 이전 모델 기준의 부모-자식 연쇄 해제
            foreach (var child in LinkState.GetChildrenSnapshot())
            {
                child.Bind(null);
            }
            LinkState.ClearChildrenLinks();
            LinkState.DetachFromParent();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ValidateFeatureViews(newModel);
#endif
            model.Value = newModel;
            return this;
        }

        /// <summary>
        /// 현재 모델을 강제로 다시 방출하여 모든 FeatureView를 재초기화합니다.
        /// 모델 인스턴스는 같지만 내부 상태가 크게 바뀌어 전체 재구성이 필요한 경우에 사용합니다.
        /// </summary>
        public void Reload() => model.ForceNotify();

        /// <summary>
        /// 부모 컴포넌트에 연결합니다. 부모가 재바인딩되거나 파괴되면 이 컴포넌트도 Bind(null)됩니다.
        /// </summary>
        public void SetParent(SindyComponent parent)
        {
            LinkState.AttachTo(parent);
        }

        protected virtual void OnDestroy()
        {
            foreach (var child in LinkState.GetChildrenSnapshot())
            {
                child.Bind(null);
            }
            LinkState.ClearChildrenLinks();
            LinkState.DetachFromParent();

            // 스트림으로 null을 전파해 FeatureView들이 스스로 정리하게 한 뒤 스트림을 닫는다.
            model.Value = null;
            model.Dispose();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Editor/Dev 빌드 한정 일회 스캔 검증 (릴리스 빌드 비용 0).
        /// 모델의 Feature 목록과 부착된 FeatureView 목록의 미스매치를 경고한다.
        /// FeatureView가 하나도 없는 허브(ViewComponent 트리 노드 등)는 검증 대상에서 제외한다.
        /// </summary>
        private void ValidateFeatureViews(IViewModel newModel)
        {
            if (newModel == null) return;
            var views = GetComponents<IFeatureView>();
            if (views.Length == 0) return;

            foreach (var featureType in newModel.GetFeatureTypes())
            {
                var matched = false;
                foreach (var view in views)
                {
                    if (view.FeatureType == featureType) { matched = true; break; }
                }
                if (!matched)
                {
                    Debug.LogWarning($"[SindyComponent] 모델의 {featureType.Name}에 매칭되는 FeatureView가 없습니다. ({name})", this);
                }
            }

            foreach (var view in views)
            {
                var matched = false;
                foreach (var featureType in newModel.GetFeatureTypes())
                {
                    if (view.FeatureType == featureType) { matched = true; break; }
                }
                if (!matched)
                {
                    Debug.LogWarning($"[SindyComponent] {view.GetType().Name}가 있으나 모델에 {view.FeatureType.Name}가 없습니다. ({name})", this);
                }
            }
        }
#endif
    }
}
