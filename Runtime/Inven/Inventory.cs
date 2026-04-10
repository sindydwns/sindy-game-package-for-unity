using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using R3;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sindy.Inven
{
    [Serializable]
    public class Inventory : IEnumerable<IReadOnlyEntityStack>
    {
        /// <summary>
        /// Amount가 0인 경우에 Items에서 제거되고 pool에 보관됨
        /// </summary>
        [SerializeField] private List<EntityStack> entities = new();
        /// <summary>
        /// Amount가 0인 경우에 pool에 보관됨
        /// (EntityStack의 OnChange 이벤트가 유실되지 않도록 하기 위함)
        /// </summary>
        [NonSerialized] private readonly List<EntityStack> pool = new();
        public IReadOnlyList<IReadOnlyEntityStack> Entities
        {
            get
            {
                RefreshInventory();
                return entities;
            }
        }
        private long totalAmount;
        public long TotalAmount
        {
            get
            {
                RefreshInventory();
                return totalAmount;
            }
            private set => totalAmount = value;
        }
        public long StackCount => entities.Count;


        private readonly Subject<ChangeEvent> changeSubject = new();
        private readonly Subject<IEntityStack> changeStackSubject = new();
        /// <summary>
        /// 수량 변화가 있을때 발생
        /// </summary>
        public Observable<ChangeEvent> OnChange => changeSubject;
        /// <summary>
        /// 스택이 생성되거나 삭제될 때 발생
        /// </summary>
        public Observable<IEntityStack> OnChangeStack => changeStackSubject;

        [NonSerialized] private bool initialized = false;
        /// <summary>
        /// 역직렬화 후 EntityStack에 Inventory 참조를 주입하고 TotalAmount를 갱신합니다.
        /// 여러 번 호출해도 최초 1회만 실행됩니다.
        /// </summary>
        public void RefreshInventory()
        {
            if (initialized)
            {
                return;
            }
            initialized = true;
            UpdateValues();
        }

        public Inventory() { }

        public Inventory(IEnumerable<IReadOnlyEntityStack> items)
        {
            entities = items == null ?
                new List<EntityStack>() :
                new List<EntityStack>(items.Select(x => new EntityStack(this, x.Entity, x.Amount)));
            RefreshInventory();
        }

        private void UpdateValues()
        {
            totalAmount = 0;
            foreach (var item in entities)
            {
                totalAmount += item.Amount;
                item.Inventory = this;
            }
        }

        public Inventory(string serial, Dictionary<int, Entity> items)
        {
            entities = new List<EntityStack>();
            Deserialize(serial, items);
            RefreshInventory();
        }

        public IEntityStack GetEntityStack(Entity target)
        {
            if (target == null)
            {
                return null;
            }
            RefreshInventory();
            for (var i = entities.Count - 1; i >= 0; i--)
            {
                var entity = entities[i];
                if (entity.Entity == target)
                {
                    if (entity.Inventory != this)
                    {
                        entities.RemoveAt(i);
                        var newEntity = new EntityStack(this, target, entity.Amount);
                        entities.Add(newEntity);
                        return newEntity;
                    }
                    return entity;
                }
            }

            foreach (var entity in pool)
            {
                if (entity.Entity == target)
                {
                    return entity;
                }
            }

            var res = new EntityStack(this, target);
            pool.Add(res);
            return res;
        }

        private void Change(IEntityStack entityStack, long newAmount)
        {
            newAmount = Math.Max(0, newAmount);
            var oldAmount = entityStack.Amount;
            if (oldAmount == newAmount)
            {
                return;
            }
            if (entityStack is not EntityStack stack)
            {
                throw new InvalidOperationException("IEntityStack is not of type EntityStack.");
            }
            if (oldAmount == 0)
            {
                pool.Remove(stack);
                entities.Add(stack);
            }
            else if (newAmount == 0)
            {
                entities.Remove(stack);
                pool.Add(stack);
            }
            TotalAmount += newAmount - oldAmount;
            stack.Amount = newAmount;
            if (oldAmount == 0 || newAmount == 0)
            {
                changeStackSubject.OnNext(stack);
            }
            if (oldAmount != newAmount)
            {
                changeSubject.OnNext(new ChangeEvent
                {
                    stack = entityStack,
                    oldAmount = oldAmount,
                });
            }
        }

        public void Add(Entity target, long amount = 1)
        {
            if (target == null || amount <= 0)
            {
                return;
            }
            var stack = GetEntityStack(target);
            Add(stack, amount);
        }

        public void Add(IReadOnlyEntityStack stack)
        {
            Add(stack.Entity, stack.Amount);
        }

        public void Add(IEnumerable<IReadOnlyEntityStack> stacks)
        {
            if (stacks == null)
            {
                return;
            }
            foreach (var stack in stacks)
            {
                Add(stack);
            }
        }

        public void Add(IEntityStack stack, long amount = 1)
        {
            if (stack == null || amount <= 0)
            {
                return;
            }
            RefreshInventory();
            if (stack.Inventory != this)
            {
                throw new InvalidOperationException("Cannot add stack from another inventory.");
            }
            Change(stack, stack.Amount + amount);
        }

        public long Remove(Entity item, long amount)
        {
            if (item == null || amount <= 0)
            {
                return 0;
            }
            var stack = GetEntityStack(item);
            return Remove(stack, amount);
        }

        public long Remove(IReadOnlyEntityStack stack)
        {
            return Remove(stack.Entity, stack.Amount);
        }

        public long Remove(IEnumerable<IReadOnlyEntityStack> stacks)
        {
            long removed = 0;
            foreach (var stack in stacks)
            {
                removed += Remove(stack);
            }
            return removed;
        }

        public long Remove(IEntityStack stack, long amount)
        {
            if (stack == null || amount <= 0)
            {
                return 0;
            }
            RefreshInventory();
            if (stack.Inventory != this)
            {
                throw new InvalidOperationException("Cannot remove stack from another inventory.");
            }
            var oldAmount = stack.Amount;
            Change(stack, stack.Amount - amount);
            var newAmount = stack.Amount;
            return oldAmount - newAmount;
        }

        public void Set(Entity item, long amount)
        {
            if (item == null)
            {
                return;
            }
            var stack = GetEntityStack(item);
            Set(stack, amount);
        }

        public void Set(IEntityStack stack, long amount)
        {
            if (stack == null || amount < 0)
            {
                throw new ArgumentException("Stack cannot be null and amount must be greater than 0.");
            }
            RefreshInventory();
            if (stack.Inventory != this)
            {
                throw new InvalidOperationException("Cannot set stack from another inventory.");
            }
            Change(stack, amount);
        }

        private IEntityStack GetItem(Entity target)
        {
            if (target == null)
            {
                return null;
            }
            RefreshInventory();
            return entities.FirstOrDefault(entity => entity.Entity == target);
        }

        public long GetAmount(Entity item)
        {
            var stack = GetItem(item);
            return stack == null ? 0 : stack.Amount;
        }

        public IEnumerable<IReadOnlyEntityStack> GetItems()
        {
            RefreshInventory();
            return entities;
        }

        public void Foreach(Action<IEntityStack> action)
        {
            for (var i = entities.Count - 1; i >= 0; i--)
            {
                action(entities[i]);
            }
        }

        public void Clear()
        {
            RefreshInventory();
            for (var i = entities.Count - 1; i >= 0; i--)
            {
                var stack = entities[i];
                stack.Set(0);
            }
        }

        public bool Contains(Entity item)
        {
            return entities.Exists(i => i.Entity == item);
        }

        public bool Contains(IReadOnlyEntityStack stack)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Entity == stack.Entity && entities[i].Amount >= stack.Amount)
                {
                    return true;
                }
            }
            return false;
        }

        public bool Contains(IEnumerable<IReadOnlyEntityStack> inventory)
        {
            foreach (var stack in inventory)
            {
                if (!Contains(stack))
                {
                    return false;
                }
            }
            return true;
        }

        public bool Contains(Inventory inventory)
        {
            return Contains(inventory.GetItems());
        }

        public long MoveTo(Inventory other, Entity item, long amount)
        {
            var removed = Remove(item, amount);
            other.Add(item, removed);
            return removed;
        }

        public long MoveTo(Inventory other, IReadOnlyEntityStack stack)
        {
            return MoveTo(other, stack.Entity, stack.Amount);
        }

        public long MoveTo(Inventory other, Inventory items)
        {
            long removed = 0;
            foreach (var item in items)
            {
                removed += MoveTo(other, item);
            }
            return removed;
        }

        public long MoveTo(Inventory other)
        {
            long removed = 0;
            for (var i = entities.Count - 1; i >= 0; i--)
            {
                removed += MoveTo(other, entities[i]);
            }
            return removed;
        }

        public IEnumerator<IReadOnlyEntityStack> GetEnumerator()
        {
            RefreshInventory();
            return Entities.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Inventory Intersect(Inventory other, Inventory result = null)
        {
            result ??= new Inventory();
            if (StackCount == 0 || other.StackCount == 0)
            {
                return result;
            }
            foreach (var item in entities)
            {
                long amount = Math.Min(item.Amount, other.GetAmount(item.Entity));
                if (amount > 0)
                {
                    result.Add(item.Entity, amount);
                }
            }
            return result;
        }

        public EntityAmount IntersectFirst(Inventory other)
        {
            if (StackCount == 0 || other == null || other.StackCount == 0)
            {
                return default;
            }
            foreach (var item in entities)
            {
                long amount = Math.Min(item.Amount, other.GetAmount(item.Entity));
                if (amount > 0)
                {
                    return new EntityAmount()
                    {
                        entity = item.Entity,
                        amount = amount
                    };
                }
            }
            return default;
        }

        public EntityAmount IntersectFirst(IEnumerable<Entity> entities)
        {
            if (StackCount == 0 || entities == null)
            {
                return default;
            }
            foreach (var item in entities)
            {
                var amount = GetAmount(item);
                if (amount > 0)
                {
                    return new EntityAmount()
                    {
                        entity = item,
                        amount = amount
                    };
                }
            }
            return default;
        }

        public Inventory Subtract(Inventory other, Inventory result = null)
        {
            result ??= new Inventory();
            if (StackCount == 0)
            {
                return result;
            }
            foreach (var item in entities)
            {
                long amount = item.Amount - other.GetAmount(item.Entity);
                if (amount > 0)
                {
                    result.Add(item.Entity, amount);
                }
            }
            return result;
        }

        public override string ToString()
        {
            if (entities.Count == 0)
            {
                return "[Inventory Empty]";
            }
            var text = "";
            foreach (var item in entities)
            {
                text += $"{item.Entity.name}({item.Amount}) ";
            }
            return text;
        }

        public string Serialize()
        {
            var strs = entities.Select(x => $"{x.Entity.id}:{x.Amount}");
            return string.Join(',', strs);
        }

        public void Deserialize(string text, Dictionary<int, Entity> items)
        {
            Clear();
            if (string.IsNullOrEmpty(text))
            {
                return;
            }
            var strs = text.Split(',');
            foreach (var str in strs)
            {
                var parts = str.Split(':');
                if (parts.Length != 2)
                {
                    continue;
                }
                var id = int.Parse(parts[0]);
                var amount = long.Parse(parts[1]);
                if (items.TryGetValue(id, out var item))
                {
                    Add(item, amount);
                }
                else
                {
                    throw new KeyNotFoundException($"Item with ID {id} not found in items dictionary.");
                }
            }
            UpdateValues();
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Inventory))]
    public class InventoryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty itemsProperty = property.FindPropertyRelative("entities");

            // Draw the property field with the label, so Unity handles the foldout and label correctly.
            EditorGUI.PropertyField(position, itemsProperty, label, true);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("entities"));
        }
    }
#endif
}
