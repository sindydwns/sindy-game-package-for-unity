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
    /// ViewModel의 자식들을 키로 하위 SindyComponent(허브)에 자동 매핑하는 트리 구조 컴포넌트.
    ///
    /// Feature는 "한 오브젝트의 능력" 축, ViewModel 자식은 "UI 트리 구조" 축 — 두 축은 공존한다.
    /// Inspector의 views 리스트에 (자식 허브, "키이름") 쌍을 등록해두면,
    /// 모델이 바인딩될 때 model[키]의 자식 모델이 각 허브에 주입되고 SetParent로 연쇄 해제가 연결된다.
    /// </summary>
    public class ViewComponent : SindyComponent
    {
        [SerializeField] private List<ViewBehaviour> views;

        private IDisposable modelSubscription;

        protected virtual void Awake()
        {
            modelSubscription = Model.Subscribe(OnModelChanged);
        }

        private void OnModelChanged(IViewModel model)
        {
            // null 전파 시 자식 해제는 허브의 LinkState 연쇄가 이미 처리했다.
            if (model == null) return;

            foreach (var view in views)
            {
                if (view.component == null) continue;

                var childModel = model[view.name];
                if (childModel != null)
                {
                    view.component.Bind(childModel).SetParent(this);
                }
                else
                {
                    Debug.LogWarning($"ViewComponent: Model for view '{view.name}' not found in ViewModel.", this);
                }
            }
        }

        protected override void OnDestroy()
        {
            modelSubscription?.Dispose();
            modelSubscription = null;
            base.OnDestroy();
        }

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
            /// </summary>
            private static string ResolveFeatureViewList(UnityEngine.Object component)
            {
                if (component is not SindyComponent sindy) return null;

                var featureViews = sindy.GetComponents<IFeatureView>();
                if (featureViews.Length == 0)
                {
                    return sindy is ViewComponent ? "View" : null;
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
