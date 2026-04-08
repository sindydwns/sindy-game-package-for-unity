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
                var component = model[view.name];
                if (component is IViewModel childModel)
                {
                    view.component.SetModel(childModel).SetParent(this);
                }
                else
                {
                    Debug.LogWarning($"ViewComponent: Model for view '{view.name}' is not an IViewModel. it is {component?.GetType().Name ?? "null"}");
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
        public class EntityAmountDrawer : PropertyDrawer
        {
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

                float componentWidth = 150f;
                float spacing = 4f;
                float nameWidth = position.width - componentWidth - spacing;

                Rect nameRect = new(position.x + componentWidth + spacing, position.y, nameWidth, position.height);
                Rect componentRect = new(position.x, position.y, componentWidth, position.height);

                EditorGUI.PropertyField(nameRect, property.FindPropertyRelative("name"), GUIContent.none);
                EditorGUI.PropertyField(componentRect, property.FindPropertyRelative("component"), GUIContent.none);

                EditorGUI.indentLevel = indent;
                EditorGUI.EndProperty();
            }
        }
#endif
    }
}
