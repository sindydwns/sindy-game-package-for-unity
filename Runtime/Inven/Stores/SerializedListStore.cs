using System;
using System.Collections.Generic;

namespace Sindy.Inven
{
    /// <summary>
    /// Entity 키 스토어. <c>[SerializeField] List&lt;EntityStack&gt;</c> 또는 기존 <see cref="Inventory"/>(Entity 기반)를 감싼다 —
    /// SO 인벤·<c>InventoryDrawer</c>와 호환된다. 수량이 0이 되면 목록에서 지운다.
    /// <para>기존 <see cref="Inventory"/>를 감싸면 그쪽 <c>OnChange</c>도 그대로 발생하므로 옮기는 동안 두 API를 함께 쓸 수 있다.</para>
    /// </summary>
    public sealed class SerializedListStore : IInventoryStore<Entity>
    {
        private readonly List<EntityStack> list;
        private readonly Inventory legacy;

        /// <param name="list">감쌀 목록. 인스펙터에 노출된 <c>List&lt;EntityStack&gt;</c>를 그대로 넘긴다.</param>
        public SerializedListStore(List<EntityStack> list)
        {
            this.list = list ?? throw new ArgumentNullException(nameof(list));
        }

        /// <param name="inventory">감쌀 기존 Inventory. 읽기·쓰기를 그쪽 <c>GetAmount</c>/<c>Set</c>에 위임한다.</param>
        public SerializedListStore(Inventory inventory)
        {
            legacy = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        public bool TryGet(Entity key, out long count)
        {
            if (key == null)
            {
                count = 0;
                return false;
            }
            if (legacy != null)
            {
                count = legacy.GetAmount(key);
                return count != 0;
            }
            var index = IndexOf(key);
            if (index < 0)
            {
                count = 0;
                return false;
            }
            count = list[index].Amount;
            return true;
        }

        public void Set(Entity key, long count)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (legacy != null)
            {
                legacy.Set(key, count);
                return;
            }
            var index = IndexOf(key);
            if (index < 0)
            {
                if (count != 0)
                {
                    list.Add(new EntityStack(null, key, count));
                }
            }
            else if (count == 0)
            {
                list.RemoveAt(index);
            }
            else
            {
                list[index].Amount = count;
            }
        }

        public IEnumerable<KeyValuePair<Entity, long>> All()
        {
            if (legacy != null)
            {
                foreach (var stack in legacy.GetItems())
                {
                    yield return new KeyValuePair<Entity, long>(stack.Entity, stack.Amount);
                }
                yield break;
            }
            for (var i = 0; i < list.Count; i++)
            {
                yield return new KeyValuePair<Entity, long>(list[i].Entity, list[i].Amount);
            }
        }

        private int IndexOf(Entity key)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Entity == key) return i;
            }
            return -1;
        }
    }
}
