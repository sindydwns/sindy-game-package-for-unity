using System;
using System.Collections.Generic;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sindy.View
{
    public class ViewComponent : SindyComponent<ViewModel>
    {
        [SerializeField] private List<ViewBehaviour> views;

        protected override void Init(ViewModel model)
        {
            foreach (var view in views)
            {
                var childModel = model[view.name];
                if (childModel != null)
                {
                    view.component.Bind(childModel).SetParent(this);
                }
                else
                {
                    Debug.LogWarning($"ViewComponent: Model for view '{view.name}' not found in ViewModel.");
                }
            }
        }

        protected override void Clear(ViewModel model)
        {
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
            private static GUIStyle modelTypeStyle;

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
                var modelType = ResolveModelType(componentProperty.objectReferenceValue);
                string modelTypeText = modelType?.Name;

                float componentWidth = 150f;
                float spacing = 4f;

                modelTypeStyle ??= new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = Color.gray }
                };

                float modelTypeWidth = 0f;
                if (!string.IsNullOrEmpty(modelTypeText))
                {
                    modelTypeWidth = modelTypeStyle.CalcSize(new GUIContent(modelTypeText)).x + spacing;
                }

                float nameWidth = position.width - componentWidth - spacing - modelTypeWidth;

                Rect componentRect = new(position.x, position.y, componentWidth, position.height);
                Rect nameRect = new(position.x + componentWidth + spacing, position.y, nameWidth, position.height);

                EditorGUI.PropertyField(componentRect, componentProperty, GUIContent.none);
                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);

                if (!string.IsNullOrEmpty(modelTypeText))
                {
                    Rect modelTypeRect = new(nameRect.xMax + spacing, position.y, modelTypeWidth, position.height);
                    GUI.Label(modelTypeRect, modelTypeText, modelTypeStyle);
                }

                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }

            private static Type ResolveModelType(UnityEngine.Object component)
            {
                if (component == null)
                    return null;

                Type type = component.GetType();
                while (type != null && type != typeof(SindyComponent))
                {
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(SindyComponent<>))
                    {
                        return type.GetGenericArguments()[0];
                    }
                    type = type.BaseType;
                }
                return null;
            }
        }
#endif
    }
}
