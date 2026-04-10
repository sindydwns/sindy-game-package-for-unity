using System.Collections.Generic;
using Sindy.Inven;
using R3;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory 직렬화 — Serialize, Deserialize, 빈 인벤토리, 잘못된 포맷,
    /// Unity Inspector 역직렬화 시뮬레이션
    /// </summary>
    class TestInventorySerialize : TestCase
    {
        public override void Run()
        {
            SerializeBasic();
            DeserializeBasic();
            SerializeRoundTrip();
            DeserializeEmpty();
            DeserializeNull();
            DeserializeInvalidFormat();
            DeserializeMissingEntity();
            ConstructorWithSerial();
            ConstructorWithItems();
            InspectorDeserialize_TotalAmountRecalculated();
            InspectorDeserialize_InventoryRefInjected();
            InspectorDeserialize_AddAfterRefresh();
            InspectorDeserialize_RemoveAfterRefresh();
            InspectorDeserialize_GetEntityStackReplacesOrphan();
            InspectorDeserialize_EventsWorkAfterRefresh();
            InspectorDeserialize_LazyRefreshOnFirstAccess();
            InspectorDeserialize_AmountPropSyncsWithField();
        }

        private Entity CreateEntity(int id, string name)
        {
            var entity = ScriptableObject.CreateInstance<Entity>();
            entity.id = id;
            entity.nameId = name;
            return entity;
        }

        // Serialize()로 인벤토리를 문자열로 변환하면 "id:amount" 형식이 포함되는지 확인
        private void SerializeBasic()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var inv = new Inventory();
            inv.Add(gold, 100);
            inv.Add(wood, 50);

            var serial = inv.Serialize();

            Assert.IsTrue(serial.Contains("1:100"));
            Assert.IsTrue(serial.Contains("2:50"));
        }

        // Deserialize()로 문자열에서 인벤토리를 복원하면 수량과 총량이 올바른지 확인
        private void DeserializeBasic()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var dict = new Dictionary<int, Entity>
            {
                { 1, gold },
                { 2, wood }
            };

            var inv = new Inventory();
            inv.Deserialize("1:100,2:50", dict);

            Assert.AreEqual(100, inv.GetAmount(gold));
            Assert.AreEqual(50, inv.GetAmount(wood));
            Assert.AreEqual(150, inv.TotalAmount);
        }

        // Serialize → Deserialize 왕복 변환 후 원본과 동일한 데이터인지 확인
        private void SerializeRoundTrip()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var dict = new Dictionary<int, Entity>
            {
                { 1, gold },
                { 2, wood }
            };

            var original = new Inventory();
            original.Add(gold, 100);
            original.Add(wood, 50);

            var serial = original.Serialize();

            var restored = new Inventory();
            restored.Deserialize(serial, dict);

            Assert.AreEqual(original.GetAmount(gold), restored.GetAmount(gold));
            Assert.AreEqual(original.GetAmount(wood), restored.GetAmount(wood));
            Assert.AreEqual(original.TotalAmount, restored.TotalAmount);
        }

        // 빈 문자열을 Deserialize하면 빈 인벤토리가 되는지 확인
        private void DeserializeEmpty()
        {
            var dict = new Dictionary<int, Entity>();
            var inv = new Inventory();

            inv.Deserialize("", dict);

            Assert.AreEqual(0, inv.TotalAmount);
            Assert.AreEqual(0, inv.StackCount);
        }

        // null을 Deserialize하면 빈 인벤토리가 되는지 확인
        private void DeserializeNull()
        {
            var dict = new Dictionary<int, Entity>();
            var inv = new Inventory();

            inv.Deserialize(null, dict);

            Assert.AreEqual(0, inv.TotalAmount);
        }

        // 잘못된 형식의 문자열을 Deserialize하면 빈 인벤토리가 되는지 확인
        private void DeserializeInvalidFormat()
        {
            var dict = new Dictionary<int, Entity>();
            var inv = new Inventory();

            inv.Deserialize("invalid,data,here", dict);

            Assert.AreEqual(0, inv.TotalAmount);
        }

        // 존재하지 않는 Entity ID가 포함된 문자열을 Deserialize하면 예외가 발생하는지 확인
        private void DeserializeMissingEntity()
        {
            var gold = CreateEntity(1, "gold");
            var dict = new Dictionary<int, Entity>
            {
                { 1, gold }
            };

            var inv = new Inventory();
            try
            {
                inv.Deserialize("1:100,999:50", dict);
                Assert.IsTrue(false, "Expected KeyNotFoundException was not thrown.");
            }
            catch (KeyNotFoundException e)
            {
                Assert.IsTrue(e.Message.Contains("999"));
            }

            // 예외 발생 전까지 파싱된 gold는 정상 복원되었는지 확인
            Assert.AreEqual(100, inv.GetAmount(gold));
        }

        // 직렬화 문자열을 생성자에 전달하여 인벤토리를 초기화할 수 있는지 확인
        private void ConstructorWithSerial()
        {
            var gold = CreateEntity(1, "gold");
            var dict = new Dictionary<int, Entity>
            {
                { 1, gold }
            };

            var inv = new Inventory("1:200", dict);

            Assert.AreEqual(200, inv.GetAmount(gold));
        }

        // IReadOnlyEntityStack 배열로 생성자에서 인벤토리를 초기화할 수 있는지 확인
        private void ConstructorWithItems()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");
            var items = new IReadOnlyEntityStack[]
            {
                new EntityAmount { entity = gold, amount = 100 },
                new EntityAmount { entity = wood, amount = 50 },
            };

            var inv = new Inventory(items);

            Assert.AreEqual(100, inv.GetAmount(gold));
            Assert.AreEqual(50, inv.GetAmount(wood));
            Assert.AreEqual(150, inv.TotalAmount);
        }

        // ──────────────────────────────────────────────
        // Unity Inspector 역직렬화 시뮬레이션 테스트
        // ──────────────────────────────────────────────
        // Unity 역직렬화 시:
        //   [SerializeField] entities, entity, amount → 복원됨
        //   [NonSerialized] inventory, _amountProp, initialized, pool → 기본값
        // JsonUtility.ToJson/FromJsonOverwrite로 이 과정을 재현

        /// <summary>
        /// Inspector로 세팅된 Inventory를 역직렬화하면
        /// [NonSerialized] 필드(totalAmount 등)가 초기화되므로
        /// RefreshInventory 호출 후 TotalAmount가 올바르게 재계산되는지 확인
        /// </summary>
        // Unity 역직렬화 후 TotalAmount가 자동 재계산되는지 확인
        private void InspectorDeserialize_TotalAmountRecalculated()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var original = new Inventory();
            original.Add(gold, 100);
            original.Add(wood, 50);
            Assert.AreEqual(150, original.TotalAmount);

            // Unity 직렬화 → 역직렬화 시뮬레이션
            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // 역직렬화 직후 TotalAmount 접근 시 RefreshInventory가 자동 호출되어 재계산
            Assert.AreEqual(150, restored.TotalAmount);
            Assert.AreEqual(2, restored.StackCount);
        }

        /// <summary>
        /// 역직렬화 후 EntityStack.Inventory가 null이었다가
        /// RefreshInventory 호출 시 올바르게 주입되는지 확인
        /// </summary>
        // Unity 역직렬화 후 EntityStack에 Inventory 참조가 올바르게 주입되는지 확인
        private void InspectorDeserialize_InventoryRefInjected()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // RefreshInventory 전에는 EntityStack.Inventory가 null
            // Entities 접근 시 RefreshInventory 자동 호출
            var entities = restored.Entities;
            Assert.AreEqual(1, entities.Count);

            // GetEntityStack을 통해 가져온 스택의 Inventory 참조가 올바른지 확인
            var stack = restored.GetEntityStack(gold);
            Assert.AreEqual(restored, stack.Inventory);
        }

        /// <summary>
        /// 역직렬화된 Inventory에 Add 가능한지 확인
        /// </summary>
        // Unity 역직렬화된 인벤토리에 Add가 정상 동작하는지 확인
        private void InspectorDeserialize_AddAfterRefresh()
        {
            var gold = CreateEntity(1, "gold");
            var wood = CreateEntity(2, "wood");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // 역직렬화 후 기존 Entity에 추가
            restored.Add(gold, 50);
            Assert.AreEqual(150, restored.GetAmount(gold));

            // 새 Entity 추가
            restored.Add(wood, 30);
            Assert.AreEqual(30, restored.GetAmount(wood));
            Assert.AreEqual(180, restored.TotalAmount);
        }

        /// <summary>
        /// 역직렬화된 Inventory에서 Remove 가능한지 확인
        /// </summary>
        // Unity 역직렬화된 인벤토리에서 Remove가 정상 동작하는지 확인
        private void InspectorDeserialize_RemoveAfterRefresh()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            var removed = restored.Remove(gold, 30);

            Assert.AreEqual(30, removed);
            Assert.AreEqual(70, restored.GetAmount(gold));
            Assert.AreEqual(70, restored.TotalAmount);
        }

        /// <summary>
        /// 역직렬화 후 GetEntityStack이 Inventory 참조가 없는 EntityStack을 감지하면
        /// 새 EntityStack으로 교체하는지 확인
        /// </summary>
        // 역직렬화 후 Inventory 참조 없는 EntityStack이 새 스택으로 교체되는지 확인
        private void InspectorDeserialize_GetEntityStackReplacesOrphan()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // GetEntityStack은 Inventory 참조가 null인 스택을 감지하여 교체
            var stack = restored.GetEntityStack(gold);

            Assert.AreEqual(gold, stack.Entity);
            Assert.AreEqual(100, stack.Amount);
            Assert.AreEqual(restored, stack.Inventory);

            // 교체된 스택을 통해 정상적으로 조작 가능
            stack.Add(50);
            Assert.AreEqual(150, restored.GetAmount(gold));
        }

        /// <summary>
        /// 역직렬화된 Inventory에서 OnChange/OnChangeStack 이벤트가 정상 동작하는지 확인
        /// </summary>
        // 역직렬화된 인벤토리에서 OnChange 이벤트가 정상 동작하는지 확인
        private void InspectorDeserialize_EventsWorkAfterRefresh()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            var changeEvents = new List<ChangeEvent>();
            restored.OnChange.Subscribe(e => changeEvents.Add(e)).AddTo(disposables);

            restored.Add(gold, 50);

            Assert.AreEqual(1, changeEvents.Count);
            Assert.AreEqual(100, changeEvents[0].OldAmount);
            Assert.AreEqual(150, changeEvents[0].NewAmount);
        }

        /// <summary>
        /// RefreshInventory를 명시 호출하지 않아도
        /// TotalAmount, Entities, GetEntityStack 등 접근 시 자동으로 초기화되는지 확인
        /// </summary>
        // RefreshInventory 명시 호출 없이 프로퍼티 접근 시 자동 초기화되는지 확인
        private void InspectorDeserialize_LazyRefreshOnFirstAccess()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // RefreshInventory 명시 호출 없이 바로 TotalAmount 접근
            Assert.AreEqual(100, restored.TotalAmount);

            // Entities 접근도 자동 초기화
            Assert.AreEqual(1, restored.Entities.Count);
        }

        /// <summary>
        /// 역직렬화 후 EntityStack의 ReactiveProperty가 null이었다가
        /// Amount 접근 시 SerializeField의 amount 값으로 올바르게 동기화되는지 확인
        /// </summary>
        // 역직렬화 후 EntityStack의 ReactiveProperty가 SerializeField 값과 동기화되는지 확인
        private void InspectorDeserialize_AmountPropSyncsWithField()
        {
            var gold = CreateEntity(1, "gold");

            var original = new Inventory();
            original.Add(gold, 100);

            var json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<Inventory>(json);

            // GetEntityStack을 통해 스택 획득 (내부에서 AmountProp 동기화 발생)
            var stack = restored.GetEntityStack(gold);

            // EntityStack.OnChange 구독 후 조작
            var snapshots = new List<(long oldAmount, long newAmount)>();
            stack.OnChange.Subscribe(e =>
                snapshots.Add((e.OldAmount, e.NewAmount))
            ).AddTo(disposables);

            stack.Add(50);

            Assert.AreEqual(1, snapshots.Count);
            Assert.AreEqual(100, snapshots[0].oldAmount);
            Assert.AreEqual(150, snapshots[0].newAmount);
        }
    }
}
