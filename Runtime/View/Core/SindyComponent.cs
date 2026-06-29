using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// FeatureView 아키텍처의 허브이자 UI 트리 노드.
    ///
    /// 모델(<see cref="IViewModel"/>)을 <see cref="ReactiveProperty{T}"/>로 보유하며,
    /// 같은 GameObject에 부착된 <see cref="FeatureView{TFeature}"/>들이 이 스트림을 구독한다.
    /// 허브는 FeatureView의 존재를 모른다 (릴리스 빌드 기준 순수 pub/sub).
    ///
    /// Feature는 "한 오브젝트의 능력" 축, ViewModel 자식은 "UI 트리 구조" 축 — 두 축은 공존한다.
    /// Inspector의 <c>views</c> 리스트에 (자식 허브, "키이름") 쌍을 등록해두면, 모델이 바인딩될 때
    /// <c>model[키]</c>의 자식 모델이 각 허브에 주입되고 <see cref="SetParent"/>로 연쇄 해제가 연결된다.
    ///
    /// - 필드 초기화이므로 Awake 실행 여부와 무관하게 비활성 상태에서도 Bind가 동작한다.
    /// - 같은 인스턴스 재바인딩은 방출되지 않는다(same-instance 스킵). 강제 재초기화는 <see cref="Reload"/>.
    /// - <see cref="SetParent"/>로 부모에 연결하면 부모 재바인딩/파괴 시 자식이 연쇄적으로 Bind(null)된다.
    /// </summary>
    public class SindyComponent : MonoBehaviour
    {
        private readonly ReactiveProperty<IViewModel> model = new();

        [SerializeField] private List<ViewBehaviour> views;

        private IDisposable modelSubscription;

        /// <summary>모델 스트림 (읽기 전용). FeatureView가 구독한다.</summary>
        public ReadOnlyReactiveProperty<IViewModel> Model => model;

        /// <summary>현재 바인딩된 모델.</summary>
        public IViewModel CurrentModel => model.Value;

        public ComponentPreset Preset { get; set; }

        private SindyComponentLinkState links;
        internal SindyComponentLinkState LinkState => links ??= new(this);

        protected static bool IsComponentPrefab(SindyComponent com) => string.IsNullOrEmpty(com.gameObject.scene.name);
        public bool IsPrefab => IsComponentPrefab(this);

        protected virtual void Awake()
        {
            modelSubscription = Model.Subscribe(OnModelChanged);
        }

        private void OnModelChanged(IViewModel newModel)
        {
            // null 전파 시 자식 해제는 LinkState 연쇄가 이미 처리했다.
            // views는 AddComponent로 생성된 경우(Blueprint 자동 컨테이너 등) null일 수 있다.
            if (newModel == null || views == null) return;

            foreach (var view in views)
            {
                if (view.component == null) continue;

                // 키가 매칭되지 않으면 조용히 통과한다(pub/sub 설계). 미스매치 진단은 Inspector 표 참고.
                var childModel = newModel[view.name];
                if (childModel != null)
                {
                    view.component.Bind(childModel).SetParent(this);
                }
            }
        }

        /// <summary>키로 등록된 자식 허브를 조회한다.</summary>
        public bool TryGetView(string key, out SindyComponent component)
        {
            if (views != null)
            {
                foreach (var view in views)
                {
                    if (view.name == key && view.component != null)
                    {
                        component = view.component;
                        return true;
                    }
                }
            }
            component = null;
            return false;
        }

        /// <summary>
        /// 런타임에 자식 허브를 등록한다 (ComponentBlueprint 조립 등).
        /// 등록 이후의 재바인딩에서 Inspector 등록분과 동일하게 모델이 주입된다.
        /// </summary>
        public void AddView(string key, SindyComponent component)
        {
            views ??= new List<ViewBehaviour>();
            views.Add(new ViewBehaviour { name = key, component = component });
        }

        /// <summary>
        /// 주어진 모델을 이 컴포넌트에 바인딩합니다.
        /// 모델이 이전과 동일한 인스턴스인 경우 아무 일도 일어나지 않습니다.
        /// </summary>
        public SindyComponent Bind(IViewModel newModel)
        {
            if (ReferenceEquals(newModel, model.Value)) return this;

            // 이전 모델 기준의 부모-자식 연쇄 해제
            foreach (var child in LinkState.GetChildrenSnapshot())
            {
                child.Bind(null);
            }
            LinkState.ClearChildrenLinks();
            LinkState.DetachFromParent();

            model.Value = newModel;
            return this;
        }

        /// <summary>
        /// 다음 프레임에 모델을 교체합니다. 버튼 OnClick 방출 도중 호출해도 안전합니다 —
        /// 재바인딩과 이전 모델 정리는 방출 스택을 벗어난 뒤(<see cref="FrameDispatcher"/>) 실행됩니다.
        ///
        /// <paramref name="disposeOld"/>가 true면(기본) 교체 직전 바인딩돼 있던 모델을 Dispose합니다.
        /// 같은 모델을 다른 뷰와 공유·재사용하는 경우 false로 두고 호출부가 수명을 직접 관리하세요
        /// (이 컴포넌트는 caller-owns 모델 수명을 전제로 합니다).
        /// </summary>
        /// <param name="next">새로 바인딩할 모델.</param>
        /// <param name="disposeOld">이전 모델을 Dispose할지 여부. 기본 true.</param>
        /// <param name="onRebound">재바인딩 완료 후 실행할 후처리(선택). Bind 이후에 호출됩니다.</param>
        public SindyComponent RebindNextFrame(IViewModel next, bool disposeOld = true, Action onRebound = null)
        {
            var old = model.Value; // 교체 요청 시점에 바인딩돼 있던 모델
            FrameDispatcher.NextFrame(() =>
            {
                Bind(next);
                if (disposeOld && !ReferenceEquals(old, next))
                {
                    (old as IDisposable)?.Dispose();
                }
                onRebound?.Invoke();
            });
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
            modelSubscription?.Dispose();
            modelSubscription = null;

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

        [Serializable]
        public class ViewBehaviour
        {
            public string name;
            public SindyComponent component;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 디버깅 표 전용: 런타임 시점의 실제 views 목록(동적 AddView 포함)을 노출한다.
        /// 직렬화 데이터가 아닌 필드를 직접 읽어 Blueprint 등이 동적 추가한 항목까지 반영한다.
        /// </summary>
        internal IReadOnlyList<ViewBehaviour> ViewsForEditor => views;
#endif
    }
}
