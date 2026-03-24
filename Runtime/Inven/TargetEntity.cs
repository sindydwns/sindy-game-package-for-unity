using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sindy.Inven
{
    [Serializable]
    public class TargetEntity
    {
        public InventoryEntity inventory;
        public Entity target;
        public long amount;

        public TargetEntity(InventoryEntity inventory, Entity target, long amount)
        {
            this.inventory = inventory;
            this.target = target;
            this.amount = amount;
        }

        public EntityAmount ToEntityAmount()
        {
            return new EntityAmount
            {
                entity = target,
                amount = amount
            };
        }

        public bool IsEnough() => IsContains();
        public bool IsContains()
        {
            return inventory.Inventory.Contains(new EntityAmount
            {
                entity = target,
                amount = amount
            });
        }

        public void InvokeAdd() => GetStack().Add(amount);
        public long InvokeRemove() => GetStack().Remove(amount);
        public void InvokeSet() => GetStack().Set(amount);

        private IEntityStack stack;
        public IEntityStack Stack => GetStack();
        private IEntityStack GetStack()
        {
            if (stack != null)
            {
                return stack;
            }
            if (inventory == null || target == null)
            {
                Debug.LogWarning("TargetStack: Inventory or Target is null");
                return null;
            }
            stack = inventory.Inventory.GetEntityStack(target);
            return stack;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(TargetEntity))]
    public class TargetEntityPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty inventoryProp = property.FindPropertyRelative("inventory");
            SerializedProperty entityProp = property.FindPropertyRelative("target");
            SerializedProperty amountProp = property.FindPropertyRelative("amount");

            if (IsExpanded(property))
            {
                var xOffset = position.x;
                var yOffset = position.y;

                var invenHeight = EditorGUI.GetPropertyHeight(inventoryProp, true);
                EditorGUI.PropertyField(
                    new Rect(xOffset, yOffset, position.width, invenHeight),
                    inventoryProp, GUIContent.none, true);
                yOffset += invenHeight + 2f;

                var spacing = 4f;
                var amountWidth = 60f;
                var entityWidth = position.width - amountWidth - spacing;
                var entityHeight = EditorGUI.GetPropertyHeight(entityProp, true);
                EditorGUI.PropertyField(
                    new Rect(xOffset + 20, yOffset, entityWidth - 20, entityHeight),
                    entityProp, GUIContent.none, true);
                xOffset += entityWidth + spacing;

                var amountHeight = EditorGUI.GetPropertyHeight(amountProp, true);
                EditorGUI.PropertyField(
                    new Rect(xOffset, yOffset, amountWidth, amountHeight),
                    amountProp, GUIContent.none, true);
            }
            else
            {
                float spacing = 4f;
                float amountWidth = 60f;
                float entityWidth = position.width - amountWidth - spacing;
                var invenHeight = EditorGUI.GetPropertyHeight(inventoryProp, true);
                var secondYOffset = position.y + invenHeight + 2f;
                EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, position.height), inventoryProp, GUIContent.none, true);
                EditorGUI.PropertyField(new Rect(position.x + spacing, secondYOffset, entityWidth, position.height), entityProp, GUIContent.none, true);
                EditorGUI.PropertyField(new Rect(position.x + entityWidth + spacing * 2, secondYOffset, amountWidth, position.height), amountProp, GUIContent.none, true);
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsExpanded(property))
            {
                SerializedProperty inventoryProp = property.FindPropertyRelative("inventory");
                SerializedProperty entityProp = property.FindPropertyRelative("target");
                SerializedProperty amountProp = property.FindPropertyRelative("amount");

                float invenHeight = EditorGUI.GetPropertyHeight(inventoryProp, true);
                float entityHeight = EditorGUI.GetPropertyHeight(entityProp, true);
                float amountHeight = EditorGUI.GetPropertyHeight(amountProp, true);

                return invenHeight + entityHeight + amountHeight + 6f; // 2f spacing between fields
            }
            return EditorGUIUtility.singleLineHeight * 2 + 4f; // 2 lines for inventory and entity, plus spacing
        }

        private bool IsExpanded(SerializedProperty prop)
        {
            var useConstProp = prop?.FindPropertyRelative("inventory")?.FindPropertyRelative("UseConstant");
            if (useConstProp == null)
            {
                return false;
            }
            if (useConstProp.boolValue == false)
            {
                return false; // If not using constant, do not expand
            }
            return prop
                    .FindPropertyRelative("inventory")
                    .FindPropertyRelative("ConstantValue")
                    .FindPropertyRelative("entities")
                    .isExpanded;
        }
    }
#endif

}
