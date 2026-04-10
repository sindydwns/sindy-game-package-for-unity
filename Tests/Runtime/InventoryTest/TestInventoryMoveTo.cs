using Sindy.Inven;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory 이동 — MoveTo(Entity), MoveTo(stack), MoveTo(inventory), MoveTo(all)
    /// </summary>
    class TestInventoryMoveTo : TestCase
    {
        public override void Run()
        {
            MoveToByEntityAndAmount();
            MoveToPartialWhenInsufficient();
            MoveToByStack();
            MoveToByInventory();
            MoveToAll();
        }

        private Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        // Entity와 수량을 지정하여 src→dst로 이동하고 양쪽 수량이 올바른지 확인
        private void MoveToByEntityAndAmount()
        {
            var src = new Inventory();
            var dst = new Inventory();
            var gold = CreateEntity(1, "gold");
            src.Add(gold, 100);

            var moved = src.MoveTo(dst, gold, 40);

            Assert.AreEqual(40, moved);
            Assert.AreEqual(60, src.GetAmount(gold));
            Assert.AreEqual(40, dst.GetAmount(gold));
        }

        // 이동 요청량이 보유량보다 많으면 보유량만큼만 이동되는지 확인
        private void MoveToPartialWhenInsufficient()
        {
            var src = new Inventory();
            var dst = new Inventory();
            var gold = CreateEntity(1, "gold");
            src.Add(gold, 30);

            var moved = src.MoveTo(dst, gold, 100);

            Assert.AreEqual(30, moved);
            Assert.AreEqual(0, src.GetAmount(gold));
            Assert.AreEqual(30, dst.GetAmount(gold));
        }

        // EntityAmount 스택을 전달하여 이동이 동작하는지 확인
        private void MoveToByStack()
        {
            var src = new Inventory();
            var dst = new Inventory();
            var gold = CreateEntity(1, "gold");
            src.Add(gold, 100);

            var stack = new EntityAmount { entity = gold, amount = 50 };
            var moved = src.MoveTo(dst, stack);

            Assert.AreEqual(50, moved);
            Assert.AreEqual(50, src.GetAmount(gold));
            Assert.AreEqual(50, dst.GetAmount(gold));
        }

        // Inventory를 비용으로 전달하여 여러 Entity를 한 번에 이동하는지 확인
        private void MoveToByInventory()
        {
            var src = new Inventory();
            var dst = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            src.Add(gold, 100);
            src.Add(wood, 50);

            var cost = new Inventory();
            cost.Add(gold, 30);
            cost.Add(wood, 20);

            var moved = src.MoveTo(dst, cost);

            Assert.AreEqual(50, moved);
            Assert.AreEqual(70, src.GetAmount(gold));
            Assert.AreEqual(30, src.GetAmount(wood));
            Assert.AreEqual(30, dst.GetAmount(gold));
            Assert.AreEqual(20, dst.GetAmount(wood));
        }

        // 매개변수 없이 MoveTo 호출 시 전체 인벤토리가 이동되는지 확인
        private void MoveToAll()
        {
            var src = new Inventory();
            var dst = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            src.Add(gold, 100);
            src.Add(wood, 50);

            var moved = src.MoveTo(dst);

            Assert.AreEqual(150, moved);
            Assert.AreEqual(0, src.TotalAmount);
            Assert.AreEqual(0, src.StackCount);
            Assert.AreEqual(100, dst.GetAmount(gold));
            Assert.AreEqual(50, dst.GetAmount(wood));
        }
    }
}
