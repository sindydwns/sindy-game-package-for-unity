using Sindy.Inven;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory 집합 연산 — Intersect, Subtract, IntersectFirst
    /// </summary>
    class TestInventorySetOps : TestCase
    {
        public override void Run()
        {
            IntersectBasic();
            IntersectNoOverlap();
            IntersectEmpty();
            IntersectWithResult();
            IntersectFirstBasic();
            IntersectFirstNoOverlap();
            IntersectFirstWithEntityList();
            SubtractBasic();
            SubtractNoOverlap();
            SubtractEmpty();
        }

        private Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        private void IntersectBasic()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var iron = CreateEntity(3, "iron");

            var a = new Inventory();
            a.Add(gold, 100);
            a.Add(wood, 50);

            var b = new Inventory();
            b.Add(gold, 70);
            b.Add(iron, 30);

            var result = a.Intersect(b);

            Assert.AreEqual(70, result.GetAmount(gold));
            Assert.AreEqual(0, result.GetAmount(wood));
            Assert.AreEqual(0, result.GetAmount(iron));
            Assert.AreEqual(1, result.StackCount);
        }

        private void IntersectNoOverlap()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var a = new Inventory();
            a.Add(gold, 100);

            var b = new Inventory();
            b.Add(wood, 50);

            var result = a.Intersect(b);

            Assert.AreEqual(0, result.TotalAmount);
            Assert.AreEqual(0, result.StackCount);
        }

        private void IntersectEmpty()
        {
            var gold = CreateEntity(1, "gold");

            var a = new Inventory();
            a.Add(gold, 100);

            var b = new Inventory();

            var result = a.Intersect(b);
            Assert.AreEqual(0, result.TotalAmount);

            result = b.Intersect(a);
            Assert.AreEqual(0, result.TotalAmount);
        }

        private void IntersectWithResult()
        {
            var gold = CreateEntity(1, "gold");

            var a = new Inventory();
            a.Add(gold, 100);

            var b = new Inventory();
            b.Add(gold, 70);

            var result = new Inventory();
            a.Intersect(b, result);

            Assert.AreEqual(70, result.GetAmount(gold));
        }

        private void IntersectFirstBasic()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var a = new Inventory();
            a.Add(gold, 100);
            a.Add(wood, 50);

            var b = new Inventory();
            b.Add(gold, 30);
            b.Add(wood, 80);

            var first = a.IntersectFirst(b);

            Assert.AreEqual(gold, first.Entity);
            Assert.AreEqual(30, first.Amount);
        }

        private void IntersectFirstNoOverlap()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var a = new Inventory();
            a.Add(gold, 100);

            var b = new Inventory();
            b.Add(wood, 50);

            var first = a.IntersectFirst(b);

            Assert.IsNull(first.Entity);
            Assert.AreEqual(0, first.Amount);
        }

        private void IntersectFirstWithEntityList()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var iron = CreateEntity(3, "iron");

            var inv = new Inventory();
            inv.Add(wood, 50);

            var entities = new Entity[] { gold, wood, iron };
            var first = inv.IntersectFirst(entities);

            Assert.AreEqual(wood, first.Entity);
            Assert.AreEqual(50, first.Amount);
        }

        private void SubtractBasic()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var a = new Inventory();
            a.Add(gold, 100);
            a.Add(wood, 50);

            var b = new Inventory();
            b.Add(gold, 30);
            b.Add(wood, 80);

            var result = a.Subtract(b);

            Assert.AreEqual(70, result.GetAmount(gold));
            Assert.AreEqual(0, result.GetAmount(wood));
            Assert.AreEqual(1, result.StackCount);
        }

        private void SubtractNoOverlap()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var a = new Inventory();
            a.Add(gold, 100);

            var b = new Inventory();
            b.Add(wood, 50);

            var result = a.Subtract(b);

            Assert.AreEqual(100, result.GetAmount(gold));
        }

        private void SubtractEmpty()
        {
            var gold = CreateEntity(1, "gold");

            var a = new Inventory();
            a.Add(gold, 100);

            var empty = new Inventory();

            var result = a.Subtract(empty);
            Assert.AreEqual(100, result.GetAmount(gold));

            result = empty.Subtract(a);
            Assert.AreEqual(0, result.TotalAmount);
        }
    }
}
