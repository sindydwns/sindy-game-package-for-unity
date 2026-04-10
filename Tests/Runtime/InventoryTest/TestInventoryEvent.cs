using System.Collections.Generic;
using Sindy.Inven;
using UnityEngine;
using UnityEngine.Assertions;
using R3;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory 이벤트 — OnChange(수량 변화), OnChangeStack(스택 생성/삭제)
    /// </summary>
    class TestInventoryEvent : TestCase
    {
        public override void Run()
        {
            OnChangeFiresOnAdd();
            OnChangeFiresOnRemove();
            OnChangeFiresOnSet();
            OnChangeNotFiresWhenNoChange();
            OnChangeStackFiresOnCreate();
            OnChangeStackFiresOnDelete();
            OnChangeEventHasCorrectValues();
            EntityStackOnChangeWorks();
        }

        private Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        private void OnChangeFiresOnAdd()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var events = new List<ChangeEvent>();

            inv.OnChange.Subscribe(e => events.Add(e)).AddTo(disposables);

            inv.Add(gold, 100);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(0, events[0].OldAmount);
            Assert.AreEqual(100, events[0].NewAmount);
            Assert.AreEqual(100, events[0].Gap);
        }

        private void OnChangeFiresOnRemove()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            var events = new List<ChangeEvent>();
            inv.OnChange.Subscribe(e => events.Add(e)).AddTo(disposables);

            inv.Remove(gold, 30);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(100, events[0].OldAmount);
            Assert.AreEqual(70, events[0].NewAmount);
            Assert.AreEqual(-30, events[0].Gap);
        }

        private void OnChangeFiresOnSet()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            var events = new List<ChangeEvent>();
            inv.OnChange.Subscribe(e => events.Add(e)).AddTo(disposables);

            inv.Set(gold, 200);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(100, events[0].OldAmount);
            Assert.AreEqual(200, events[0].NewAmount);
        }

        private void OnChangeNotFiresWhenNoChange()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            var events = new List<ChangeEvent>();
            inv.OnChange.Subscribe(e => events.Add(e)).AddTo(disposables);

            inv.Set(gold, 100);

            Assert.AreEqual(0, events.Count);
        }

        private void OnChangeStackFiresOnCreate()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var stackEvents = new List<IEntityStack>();

            inv.OnChangeStack.Subscribe(s => stackEvents.Add(s)).AddTo(disposables);

            inv.Add(gold, 10);

            Assert.AreEqual(1, stackEvents.Count);
            Assert.AreEqual(gold, stackEvents[0].Entity);
        }

        private void OnChangeStackFiresOnDelete()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 10);

            var stackEvents = new List<IEntityStack>();
            inv.OnChangeStack.Subscribe(s => stackEvents.Add(s)).AddTo(disposables);

            inv.Remove(gold, 10);

            Assert.AreEqual(1, stackEvents.Count);
            Assert.AreEqual(gold, stackEvents[0].Entity);
        }

        private void OnChangeEventHasCorrectValues()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 50);

            ChangeEvent captured = default;
            inv.OnChange.Subscribe(e => captured = e).AddTo(disposables);

            inv.Add(gold, 30);

            Assert.AreEqual(gold, captured.Entity);
            Assert.AreEqual(inv, captured.Inventory);
            Assert.AreEqual(50, captured.OldAmount);
            Assert.AreEqual(80, captured.NewAmount);
            Assert.AreEqual(30, captured.Gap);
        }

        private void EntityStackOnChangeWorks()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var stack = inv.GetEntityStack(gold);

            // ChangeEvent.NewAmount는 stack.Amount 라이브 참조이므로,
            // 이벤트 발생 시점의 값을 스냅샷으로 캡처
            var snapshots = new List<(long oldAmount, long newAmount)>();
            stack.OnChange.Subscribe(e =>
                snapshots.Add((e.OldAmount, e.NewAmount))
            ).AddTo(disposables);

            stack.Add(50);
            stack.Add(30);
            stack.Remove(20);

            Assert.AreEqual(3, snapshots.Count);
            Assert.AreEqual(0, snapshots[0].oldAmount);
            Assert.AreEqual(50, snapshots[0].newAmount);
            Assert.AreEqual(50, snapshots[1].oldAmount);
            Assert.AreEqual(80, snapshots[1].newAmount);
            Assert.AreEqual(80, snapshots[2].oldAmount);
            Assert.AreEqual(60, snapshots[2].newAmount);
        }
    }
}
