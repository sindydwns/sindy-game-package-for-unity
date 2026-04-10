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
