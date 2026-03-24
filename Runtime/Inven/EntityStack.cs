using System;
using UnityEngine;
using R3;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sindy.Inven
{
    [Serializable]
    public class EntityStack : IEntityStack
    {
        [NonSerialized] private Inventory inventory;
        [SerializeField] private Entity entity;
        [SerializeField] private long amount;

        private ReactiveProperty<long> _amountProp;
        public Inventory Inventory { get => inventory; set => inventory = value; }
        public Entity Entity { get => entity; }
        public long Amount
        {
            get
            {
                _amountProp ??= new ReactiveProperty<long>(amount);
                return _amountProp.Value;
            }
            set
            {
                amount = value;
                _amountProp ??= new ReactiveProperty<long>(amount);
                _amountProp.Value = value;
            }
        }
        private Observable<ChangeEvent> _onChange;
        public Observable<ChangeEvent> OnChange
        {
            get
            {
                _amountProp ??= new ReactiveProperty<long>(amount);
                _onChange ??= _amountProp.Pairwise().Select(x => new ChangeEvent
                {
                    stack = this,
                    oldAmount = x.Previous,
                });
                return _onChange;
            }
        }

        public EntityStack(Inventory inventory, Entity entity, long amount)
        {
            this.inventory = inventory;
            this.entity = entity;
            this.amount = amount;
        }

        public EntityStack(Inventory inventory, Entity entity)
        {
            this.inventory = inventory;
            this.entity = entity;
            amount = 0;
        }

        public override string ToString()
        {
            return $"{Entity} x{Amount}";
        }

        public static bool operator ==(EntityStack stack, Entity entity)
        {
            return stack?.Entity == entity;
        }

        public static bool operator !=(EntityStack stack, Entity entity)
        {
            return stack?.Entity != entity;
        }

        public override bool Equals(object obj)
        {
            if (obj is EntityStack other)
            {
                return Entity == other.Entity && Amount == other.Amount;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Entity, Amount);
        }

        public void Add(long amount)
        {
            inventory.Add(this, amount);
        }

        public long Remove(long amount)
        {
            return inventory.Remove(this, amount);
        }

        public void Set(long amount)
        {
            inventory.Set(this, amount);
        }
    }

    public interface IEntityStack : IReadOnlyEntityStack
    {
        public Observable<ChangeEvent> OnChange { get; }
        public Inventory Inventory { get; }
        public void Add(long amount);
        public long Remove(long amount);
        public void Set(long amount);
    }

    public struct ChangeEvent
    {
        public IEntityStack stack;
        public readonly IEntityStack Stack => stack;
        public readonly Inventory Inventory => stack.Inventory;
        public readonly Entity Entity => stack.Entity;
        public readonly long NewAmount => stack.Amount;
        public long oldAmount;
        public readonly long OldAmount => oldAmount;
        public readonly long Gap => stack.Amount - oldAmount;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(EntityStack))]
    public class EntityStackDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

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
