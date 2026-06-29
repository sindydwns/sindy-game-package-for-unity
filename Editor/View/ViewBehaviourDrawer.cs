#if UNITY_EDITOR
using System.Text;
using Sindy.View;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.View
{
    /// <summary>
    /// <see cref="SindyComponent.ViewBehaviour"/> 한 줄을 그리는 PropertyDrawer.
    ///
    /// (연결 허브 · 키이름) 쌍을 한 줄에 배치하고, 그 오른쪽에 연결된 허브가 가진
    /// FeatureView 목록(예: "Text, Button")을 회색 라벨로 덧붙인다.
    /// 어떤 Feature를 가진 모델을 연결해야 하는지 인스펙터에서 바로 확인할 수 있다.
    /// </summary>
    [CustomPropertyDrawer(typeof(SindyComponent.ViewBehaviour))]
    public class ViewBehaviourDrawer : PropertyDrawer
    {
        private const float ComponentWidth = 150f;
        private const float Spacing = 4f;

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

            float featureListWidth = string.IsNullOrEmpty(featureListText)
                ? 0f
                : FeatureListStyle.CalcSize(new GUIContent(featureListText)).x + Spacing;

            float nameWidth = position.width - ComponentWidth - Spacing - featureListWidth;

            Rect componentRect = new(position.x, position.y, ComponentWidth, position.height);
            Rect nameRect = new(position.x + ComponentWidth + Spacing, position.y, nameWidth, position.height);

            EditorGUI.PropertyField(componentRect, componentProperty, GUIContent.none);
            EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);

            if (!string.IsNullOrEmpty(featureListText))
            {
                Rect featureListRect = new(nameRect.xMax + Spacing, position.y, featureListWidth, position.height);
                GUI.Label(featureListRect, featureListText, FeatureListStyle);
            }

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        private static GUIStyle FeatureListStyle => featureListStyle ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.gray }
        };

        /// <summary>
        /// 연결된 허브의 FeatureView 목록을 "Text, Button" 형태 문자열로 만든다.
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
}
#endif
