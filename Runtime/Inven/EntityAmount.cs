using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif

namespace Sindy.Inven
{
    /// <summary>
    /// Entity의 수량을 나타내는 구조체
    /// </summary>
    [Serializable]
    public struct EntityAmount : IReadOnlyEntityStack
    {
        public Entity entity;
        public readonly Entity Entity => entity;
        public long amount;
        public readonly long Amount => amount;

        public override string ToString()
        {
            return $"{entity} x{amount}";
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EntityAmount))]
    public class EntityAmountDrawer : PropertyDrawer
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

            float amountWidth = 60f;
            float spacing = 4f;
            float itemWidth = position.width - amountWidth - spacing;

            Rect itemRect = new(position.x, position.y, itemWidth, position.height);
            Rect amountRect = new(position.x + itemWidth + spacing, position.y, amountWidth, position.height);

            EditorGUI.PropertyField(itemRect, property.FindPropertyRelative("entity"), GUIContent.none);
            EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("amount"), GUIContent.none);

            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }
    }
#endif
}
