using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using R3;
using Sindy.Inven;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// 기본 Store — DictionaryStore(0 제거/유지), SerializedListStore(List&lt;EntityStack&gt; 왕복, 기존 Inventory 위임)
    /// </summary>
    class TestInventoryStores : TestCase
    {
        public override void Run()
        {
            DictionaryStoreRemovesZeroByDefault();
            DictionaryStoreKeepZero();
            SerializedListStoreRoundTripsEntityList();
            SerializedListStoreDelegatesToLegacyInventory();
        }

        private static Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        private void DictionaryStoreRemovesZeroByDefault()
        {
            var dict = new Dictionary<int, long>();
            using var inv = new Inventory<int>(new DictionaryStore<int>(dict));
            inv.Add(7, 3);
            Assert.AreEqual(3, dict[7]);
            inv.Remove(7, 3);
            Assert.IsFalse(dict.ContainsKey(7));
            Assert.AreEqual(0, inv.Entries.Count());
            Assert.IsFalse(inv.Store.TryGet(7, out var count));
            Assert.AreEqual(0, count);
        }

        private void DictionaryStoreKeepZero()
        {
            var store = new DictionaryStore<int>(keepZero: true);
            using var inv = new Inventory<int>(store);
            inv.Add(7, 1);
            inv.Remove(7, 1);
            Assert.IsTrue(store.Dictionary.ContainsKey(7));
            Assert.AreEqual(0, store.Dictionary[7]);
            Assert.AreEqual(1, inv.Entries.Count());
        }

        // Autoline형 스토어로 Add/Remove 후 List<EntityStack> 내용 일치. 0이 되면 목록에서 제거
        private void SerializedListStoreRoundTripsEntityList()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var list = new List<EntityStack> { new(null, gold, 5) };
            using var inv = new Inventory<Entity>(new SerializedListStore(list))
                .With(new CapacityFeature<Entity>(e => e == wood ? 2 : 0, () => 10));

            Assert.AreEqual(5, inv.Count(gold));
            Assert.IsTrue(inv.Add(gold, 2));
            Assert.IsTrue(inv.Add(wood, 3));                           // 6
            Assert.IsFalse(inv.Add(wood, 3));                          // 12 > 10

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(7, list.First(s => s.Entity == gold).Amount);
            Assert.AreEqual(3, list.First(s => s.Entity == wood).Amount);

            Assert.IsTrue(inv.Remove(gold, 7));
            Assert.AreEqual(1, list.Count);
            Assert.IsFalse(list.Any(s => s.Entity == gold));
            Assert.AreEqual(0, inv.Count(gold));

            var entries = inv.Entries.ToList();
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(wood, entries[0].Key);
            Assert.AreEqual(3, entries[0].Value);

            // Rebind로 다른 목록을 물리면 CountProp이 그 값을 받는다
            var prop = inv.CountProp(gold);
            inv.Rebind(new SerializedListStore(new List<EntityStack> { new(null, gold, 42) }));
            Assert.AreEqual(42, prop.CurrentValue);
            Assert.AreEqual(0, inv.Feature<CapacityFeature<Entity>>().Used);
        }

        // 기존 Inventory를 감싸면 읽기·쓰기가 그쪽으로 위임되고 기존 OnChange도 발생한다
        private void SerializedListStoreDelegatesToLegacyInventory()
        {
            var gold = CreateEntity(1, "gold");
            var legacy = new Inventory();
            legacy.Add(gold, 4);
            var legacyEvents = 0;
            legacy.OnChange.Subscribe(_ => legacyEvents++).AddTo(disposables);

            using var inv = new Inventory<Entity>(new SerializedListStore(legacy));
            Assert.AreEqual(4, inv.Count(gold));

            Assert.IsTrue(inv.Add(gold, 6));
            Assert.AreEqual(10, legacy.GetAmount(gold));
            Assert.AreEqual(1, legacyEvents);

            legacy.Remove(gold, 10);                                   // 기존 API로 바꿔도 새 코어가 읽는다
            Assert.AreEqual(0, inv.Count(gold));
            Assert.IsFalse(inv.Store.TryGet(gold, out _));
            Assert.AreEqual(0, inv.Entries.Count());
        }
    }
}
