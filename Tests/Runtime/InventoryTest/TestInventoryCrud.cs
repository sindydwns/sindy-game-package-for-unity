using Sindy.Inven;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory CRUD — Add, Remove, Set, GetAmount, Contains, Clear
    /// </summary>
    class TestInventoryCrud : TestCase
    {
        public override void Run()
        {
            AddBasic();
            AddMultipleEntities();
            AddDuplicateAccumulates();
            AddZeroOrNegativeIgnored();
            AddNullIgnored();
            RemoveBasic();
            RemoveReturnsActualRemoved();
            RemoveMoreThanExists();
            RemoveZeroOrNegativeIgnored();
            SetBasic();
            SetToZeroRemovesStack();
            GetAmountReturnsZeroForMissing();
            ContainsEntity();
            ContainsStack();
            ContainsInventory();
            ClearResetsAll();
            TotalAmountTracked();
            StackCountTracked();
            ForeachIteratesAll();
        }

        private Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        private void AddBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 100);

            Assert.AreEqual(100, inv.GetAmount(gold));
            Assert.AreEqual(1, inv.StackCount);
        }

        private void AddMultipleEntities()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            inv.Add(gold, 50);
            inv.Add(wood, 30);

            Assert.AreEqual(50, inv.GetAmount(gold));
            Assert.AreEqual(30, inv.GetAmount(wood));
            Assert.AreEqual(2, inv.StackCount);
        }

        private void AddDuplicateAccumulates()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 50);
            inv.Add(gold, 30);

            Assert.AreEqual(80, inv.GetAmount(gold));
            Assert.AreEqual(1, inv.StackCount);
        }

        private void AddZeroOrNegativeIgnored()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 0);
            inv.Add(gold, -10);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        private void AddNullIgnored()
        {
            var inv = new Inventory();

            inv.Add((Entity)null, 100);

            Assert.AreEqual(0, inv.TotalAmount);
            Assert.AreEqual(0, inv.StackCount);
        }

        private void RemoveBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            var removed = inv.Remove(gold, 30);

            Assert.AreEqual(30, removed);
            Assert.AreEqual(70, inv.GetAmount(gold));
        }

        private void RemoveReturnsActualRemoved()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 10);

            var removed = inv.Remove(gold, 30);

            Assert.AreEqual(10, removed);
            Assert.AreEqual(0, inv.GetAmount(gold));
        }

        private void RemoveMoreThanExists()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 5);

            inv.Remove(gold, 100);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        private void RemoveZeroOrNegativeIgnored()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 50);

            var removed = inv.Remove(gold, 0);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(50, inv.GetAmount(gold));
        }

        private void SetBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Set(gold, 200);

            Assert.AreEqual(200, inv.GetAmount(gold));
        }

        private void SetToZeroRemovesStack()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            inv.Set(gold, 0);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        private void GetAmountReturnsZeroForMissing()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            Assert.AreEqual(0, inv.GetAmount(gold));
        }

        private void ContainsEntity()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            inv.Add(gold, 10);

            Assert.IsTrue(inv.Contains(gold));
            Assert.IsFalse(inv.Contains(wood));
        }

        private void ContainsStack()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 50);

            var enough = new EntityAmount { entity = gold, amount = 30 };
            var tooMuch = new EntityAmount { entity = gold, amount = 100 };

            Assert.IsTrue(inv.Contains(enough));
            Assert.IsFalse(inv.Contains(tooMuch));
        }

        private void ContainsInventory()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            inv.Add(gold, 100);
            inv.Add(wood, 50);

            var required = new Inventory();
            required.Add(gold, 50);
            required.Add(wood, 30);

            var tooMuch = new Inventory();
            tooMuch.Add(gold, 200);

            Assert.IsTrue(inv.Contains(required));
            Assert.IsFalse(inv.Contains(tooMuch));
        }

        private void ClearResetsAll()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            inv.Add(gold, 100);
            inv.Add(wood, 50);

            inv.Clear();

            Assert.AreEqual(0, inv.TotalAmount);
            Assert.AreEqual(0, inv.StackCount);
            Assert.AreEqual(0, inv.GetAmount(gold));
        }

        private void TotalAmountTracked()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            inv.Add(gold, 100);
            inv.Add(wood, 50);
            Assert.AreEqual(150, inv.TotalAmount);

            inv.Remove(gold, 30);
            Assert.AreEqual(120, inv.TotalAmount);

            inv.Set(wood, 0);
            Assert.AreEqual(70, inv.TotalAmount);
        }

        private void StackCountTracked()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            inv.Add(gold, 10);
            Assert.AreEqual(1, inv.StackCount);

            inv.Add(wood, 20);
            Assert.AreEqual(2, inv.StackCount);

            inv.Remove(gold, 10);
            Assert.AreEqual(1, inv.StackCount);
        }

        private void ForeachIteratesAll()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            inv.Add(gold, 10);
            inv.Add(wood, 20);

            long sum = 0;
            inv.Foreach(stack => sum += stack.Amount);

            Assert.AreEqual(30, sum);
        }
    }
}
