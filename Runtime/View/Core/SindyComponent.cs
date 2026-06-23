using System;
using System.Collections.Generic;
using System.Text;
using R3;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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

                var childModel = newModel[view.name];
                if (childModel != null)
                {
                    view.component.Bind(childModel).SetParent(this);
                }
                else
                {
                    Debug.LogWarning($"SindyComponent: Model for view '{view.name}' not found in ViewModel.", this);
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// Editor/Dev 빌드 한정 일회 스캔 검증 (릴리스 빌드 비용 0).
        /// 모델의 Feature 목록과 부착된 FeatureView 목록의 미스매치를 경고한다.
        /// FeatureView가 하나도 없는 허브(트리 노드 등)는 검증 대상에서 제외한다.
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

        [Serializable]
        public class ViewBehaviour
        {
            public string name;
            public SindyComponent component;
        }

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ViewBehaviour))]
        public class ViewBehaviourDrawer : PropertyDrawer
        {
            private static GUIStyle featureListStyle;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                EditorGUI.BeginProperty(position, label, property);

                position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), GUIContent.none);

                int indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                var componentProperty = property.FindPropertyRelative("component");
                var featureListText = ResolveFeatureViewList(componentProperty.objectReferenceValue);

                float componentWidth = 150f;
                float spacing = 4f;

                featureListStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.gray }
                };

                float featureListWidth = 0f;
                if (!string.IsNullOrEmpty(featureListText))
                {
                    featureListWidth = featureListStyle.CalcSize(new GUIContent(featureListText)).x + spacing;
                }

                float nameWidth = position.width - componentWidth - spacing - featureListWidth;

                Rect componentRect = new(position.x, position.y, componentWidth, position.height);
                Rect nameRect = new(position.x + componentWidth + spacing, position.y, nameWidth, position.height);

                EditorGUI.PropertyField(componentRect, componentProperty, GUIContent.none);
                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);

                if (!string.IsNullOrEmpty(featureListText))
                {
                    Rect featureListRect = new(nameRect.xMax + spacing, position.y, featureListWidth, position.height);
                    GUI.Label(featureListRect, featureListText, featureListStyle);
                }

                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }

            /// <summary>
            /// 연결된 허브 오브젝트의 FeatureView 목록을 "Text, Button" 형태로 표시한다.
            /// 어떤 Feature를 가진 모델을 연결해야 하는지 한눈에 확인할 수 있다.
            /// FeatureView가 없는 트리 노드는 "View"로 표시한다.
            /// </summary>
            private static string ResolveFeatureViewList(UnityEngine.Object component)
            {
                if (component is not SindyComponent sindy) return null;

                var featureViews = sindy.GetComponents<IFeatureView>();
                if (featureViews.Length == 0)
                {
                    return "View";
                }

                var sb = new StringBuilder();
                foreach (var view in featureViews)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(ShortFeatureName(view.FeatureType.Name));
                }
                return sb.ToString();
            }

            private static string ShortFeatureName(string featureTypeName)
            {
                const string suffix = "Feature";
                return featureTypeName.EndsWith(suffix)
                    ? featureTypeName.Substring(0, featureTypeName.Length - suffix.Length)
                    : featureTypeName;
            }
        }
#endif
    }
}
