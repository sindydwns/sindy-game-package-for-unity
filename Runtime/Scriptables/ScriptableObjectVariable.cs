using System;
using UnityEngine;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sindy.Scriptables
{
    public abstract class ScriptableObjectVariable<T> : ScriptableObject
    {
#if UNITY_EDITOR
        [Multiline]
        public string description = ""; // for developer
#endif
        public T Value;
        public event Action<T> OnChange;
        public void Dirty() => OnChange?.Invoke(Value);
        public static implicit operator T(ScriptableObjectVariable<T> variable) => variable.Value;

#if UNITY_EDITOR
        private void OnValidate()
        {
            Dirty();
        }
#endif
    }

    [Serializable]
    public abstract class ScriptableObjectReference<T, V> where V : ScriptableObjectVariable<T>
    {
        public bool UseConstant = true;
        public T ConstantValue;
        public V Variable;

        public ScriptableObjectReference() { }

        public ScriptableObjectReference(T value)
        {
            UseConstant = true;
            ConstantValue = value;
        }

        public ScriptableObjectReference(V value)
        {
            UseConstant = false;
            Variable = value;
        }

        public T Value => UseConstant ? ConstantValue : Variable;
        public static implicit operator T(ScriptableObjectReference<T, V> reference) => reference.Value;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ScriptableObjectReference<,>), true)]
    public class ScriptableObjectReferenceDrawer : PropertyDrawer
    {
        private const float popupWidth = 55f;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // 1) 서브 프로퍼티 가져오기
            var useConstProp = property.FindPropertyRelative("UseConstant");
            var constProp = property.FindPropertyRelative("ConstantValue");
            var varProp = property.FindPropertyRelative("Variable");

            EditorGUI.BeginProperty(position, label, property);

            // 2) 라벨과 케밥버튼/필드 영역 분할
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            var kebabRect = new Rect(position.x, position.y, EditorGUIUtility.singleLineHeight, position.height);
            var fieldRect = new Rect(position.x + kebabRect.width + 2, position.y,
                                     position.width - kebabRect.width - 2, position.height);

            // 3) 케밥버튼 (⋮) 표시 및 팝업
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (GUI.Button(kebabRect, EditorGUIUtility.IconContent("_Popup"), GUIStyle.none))
            {
                GenericMenu menu = new();
                menu.AddItem(new GUIContent("Constant"), useConstProp.boolValue, () =>
                {
                    useConstProp.boolValue = true;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.AddItem(new GUIContent("Reference"), !useConstProp.boolValue, () =>
                {
                    useConstProp.boolValue = false;
                    property.serializedObject.ApplyModifiedProperties();
                });
                menu.DropDown(kebabRect);
            }

            var prop = useConstProp.boolValue ? constProp : varProp;
            EditorGUI.PropertyField(fieldRect, prop, GUIContent.none, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.FindPropertyRelative("UseConstant").boolValue)
            {
                return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("ConstantValue"), includeChildren: true);
            }
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("Variable"), includeChildren: true);
        }
    }
#endif
}
