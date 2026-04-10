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

        // 기본 Add로 Entity가 정상 추가되는지 확인
        private void AddBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 100);

            Assert.AreEqual(100, inv.GetAmount(gold));
            Assert.AreEqual(1, inv.StackCount);
        }

        // 서로 다른 Entity 여러 개를 Add하면 각각 독립 스택으로 추가되는지 확인
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

        // 같은 Entity를 여러 번 Add하면 수량이 누적되는지 확인
        private void AddDuplicateAccumulates()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 50);
            inv.Add(gold, 30);

            Assert.AreEqual(80, inv.GetAmount(gold));
            Assert.AreEqual(1, inv.StackCount);
        }

        // 0 또는 음수 수량으로 Add 시 무시되는지 확인
        private void AddZeroOrNegativeIgnored()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Add(gold, 0);
            inv.Add(gold, -10);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        // null Entity로 Add 시 무시되는지 확인
        private void AddNullIgnored()
        {
            var inv = new Inventory();

            inv.Add((Entity)null, 100);

            Assert.AreEqual(0, inv.TotalAmount);
            Assert.AreEqual(0, inv.StackCount);
        }

        // 기본 Remove로 수량이 차감되고 실제 제거량을 반환하는지 확인
        private void RemoveBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            var removed = inv.Remove(gold, 30);

            Assert.AreEqual(30, removed);
            Assert.AreEqual(70, inv.GetAmount(gold));
        }

        // 보유량보다 많이 Remove 요청 시 보유량만큼만 제거하고 실제 제거량을 반환하는지 확인
        private void RemoveReturnsActualRemoved()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 10);

            var removed = inv.Remove(gold, 30);

            Assert.AreEqual(10, removed);
            Assert.AreEqual(0, inv.GetAmount(gold));
        }

        // 보유량 초과 Remove 시 수량이 0이 되고 스택이 삭제되는지 확인
        private void RemoveMoreThanExists()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 5);

            inv.Remove(gold, 100);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        // 0 수량으로 Remove 시 무시되는지 확인
        private void RemoveZeroOrNegativeIgnored()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 50);

            var removed = inv.Remove(gold, 0);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(50, inv.GetAmount(gold));
        }

        // Set으로 수량을 직접 설정할 수 있는지 확인
        private void SetBasic()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            inv.Set(gold, 200);

            Assert.AreEqual(200, inv.GetAmount(gold));
        }

        // Set(0)으로 설정하면 스택이 삭제되는지 확인
        private void SetToZeroRemovesStack()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            inv.Add(gold, 100);

            inv.Set(gold, 0);

            Assert.AreEqual(0, inv.GetAmount(gold));
            Assert.AreEqual(0, inv.StackCount);
        }

        // 존재하지 않는 Entity의 GetAmount가 0을 반환하는지 확인
        private void GetAmountReturnsZeroForMissing()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");

            Assert.AreEqual(0, inv.GetAmount(gold));
        }

        // Contains(Entity)로 Entity 보유 여부를 판별하는지 확인
        private void ContainsEntity()
        {
            var inv = new Inventory();
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            inv.Add(gold, 10);

            Assert.IsTrue(inv.Contains(gold));
            Assert.IsFalse(inv.Contains(wood));
        }

        // Contains(EntityAmount)로 필요 수량 충족 여부를 판별하는지 확인
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

        // Contains(Inventory)로 필요 인벤토리의 모든 항목 충족 여부를 판별하는지 확인
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

        // Clear 후 총량/스택수/개별 수량이 모두 0인지 확인
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

        // Add/Remove/Set에 따라 TotalAmount가 올바르게 추적되는지 확인
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

        // Add/Remove에 따라 StackCount가 올바르게 추적되는지 확인
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

        // Foreach로 모든 스택을 순회하며 수량을 합산할 수 있는지 확인
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
