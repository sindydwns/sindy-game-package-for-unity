using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Sindy.Inven
{
    [Serializable]
    public struct EntityRate
    {
        public Entity Entity;
        public float Rate;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EntityRate))]
    public class EntityRateDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float rateWidth = 60f;
            float spacing = 4f;
            float itemWidth = position.width - rateWidth - spacing;

            Rect itemRect = new(position.x, position.y, itemWidth, position.height);
            Rect rateRect = new(position.x + itemWidth + spacing, position.y, rateWidth, position.height);

            EditorGUI.PropertyField(itemRect, property.FindPropertyRelative("Entity"), GUIContent.none);
            EditorGUI.PropertyField(rateRect, property.FindPropertyRelative("Rate"), GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif
}
