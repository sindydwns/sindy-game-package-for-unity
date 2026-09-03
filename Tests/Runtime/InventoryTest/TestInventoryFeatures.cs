using System;
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using Sindy.Inven;

namespace Sindy.Test
{
    /// <summary>
    /// 기본 Feature — CapacityFeature(증분·Rebind 재계산·IsFull·ignoreCap), FilterFeature, HookFeature
    /// </summary>
    class TestInventoryFeatures : TestCase
    {
        public override void Run()
        {
            CapacityRejectsWhenOverAndKeepsUsedInSync();
            CapacityIncrementalMatchesFullRecalcAfterRebind();
            CapacityIgnoreCapAndZeroCost();
            CapacityRefreshFollowsChangedCapacity();
            CapacityMoveBetweenInventories();
            FilterRejectsOnlyAdd();
            HookReceivesChangeAndRebind();
        }

        private static readonly Dictionary<string, long> Weights = new()
        {
            ["gold"] = 0,
            ["wood"] = 2,
            ["stone"] = 5,
        };

        private static long WeightOf(string key) => Weights[key];

        // 상한 초과 추가는 거부(이유 = Feature 것), Used·IsFull이 증분으로 따라온다
        private void CapacityRejectsWhenOverAndKeepsUsedInSync()
        {
            var cap = new CapacityFeature<string>(WeightOf, () => 10, reason: "bag.full");
            using var inv = new Inventory<string>(new DictionaryStore<string>()).With(cap);
            var usedHistory = new List<long>();
            var fullHistory = new List<bool>();
            cap.UsedProp.Subscribe(usedHistory.Add).AddTo(disposables);
            cap.IsFullProp.Subscribe(fullHistory.Add).AddTo(disposables);

            Assert.AreSame(cap, inv.Feature<CapacityFeature<string>>());
            Assert.AreEqual(10, cap.Capacity);
            Assert.AreEqual(10, cap.Free);

            Assert.IsTrue(inv.Add("wood", 4));                         // 8
            Assert.IsFalse(inv.CanAdd("stone", 1, out var reason));    // 13 > 10
            Assert.AreEqual("bag.full", reason);
            Assert.IsFalse(inv.Add("stone", 1));
            Assert.AreEqual(0, inv.Count("stone"));
            Assert.AreEqual(8, cap.Used);
            Assert.IsFalse(cap.IsFull);

            Assert.IsTrue(inv.Add("wood", 1));                         // 10 — 꽉 참
            Assert.IsTrue(cap.IsFull);
            Assert.AreEqual(0, cap.Free);
            Assert.IsFalse(inv.Add("wood", 1));

            Assert.IsTrue(inv.Remove("wood", 2));                      // 6
            Assert.IsFalse(cap.IsFull);
            Assert.AreEqual(new long[] { 0, 8, 10, 6 }, usedHistory.ToArray());
            Assert.AreEqual(new[] { false, true, false }, fullHistory.ToArray());
        }

        // N회 증감 후 UsedProp == 전체 재계산 값. Rebind 후에도 새 스토어 기준으로 재계산
        private void CapacityIncrementalMatchesFullRecalcAfterRebind()
        {
            var cap = new CapacityFeature<string>(WeightOf, () => 1000);
            var dict = new Dictionary<string, long>();
            using var inv = new Inventory<string>(new DictionaryStore<string>(dict)).With(cap);
            var rng = new Random(42);
            var keys = new[] { "gold", "wood", "stone" };
            for (var i = 0; i < 200; i++)
            {
                var key = keys[rng.Next(keys.Length)];
                var n = rng.Next(1, 5);
                if (rng.Next(2) == 0) inv.Add(key, n);
                else inv.Remove(key, n);
            }
            Assert.AreEqual(Recalc(dict), cap.Used);

            var other = new Dictionary<string, long> { ["wood"] = 7, ["stone"] = 3 };
            inv.Rebind(new DictionaryStore<string>(other));
            Assert.AreEqual(14 + 15, cap.Used);
            Assert.AreEqual(Recalc(other), cap.Used);

            inv.Add("stone", 1);
            Assert.AreEqual(Recalc(other), cap.Used);
        }

        private static long Recalc(Dictionary<string, long> dict)
        {
            long sum = 0;
            foreach (var kv in dict) sum += WeightOf(kv.Key) * kv.Value;
            return sum;
        }

        // ignoreCap이 true면 게이트 통과(사용량은 계속 집계). 비용 0인 키는 용량과 무관
        private void CapacityIgnoreCapAndZeroCost()
        {
            var ignore = false;
            var cap = new CapacityFeature<string>(WeightOf, () => 4, () => ignore);
            using var inv = new Inventory<string>(new DictionaryStore<string>()).With(cap);

            Assert.IsTrue(inv.Add("gold", 1_000_000));                 // 비용 0
            Assert.AreEqual(0, cap.Used);
            Assert.IsFalse(inv.Add("stone", 1));                       // 5 > 4

            ignore = true;
            Assert.IsTrue(inv.Add("stone", 1));
            Assert.AreEqual(5, cap.Used);
            Assert.IsTrue(cap.IsFull);

            ignore = false;
            Assert.IsFalse(inv.Add("wood", 1));
            Assert.IsTrue(inv.Remove("stone", 1));                     // 제거는 항상 허용
            Assert.AreEqual(0, cap.Used);
        }

        // 상한이 바깥에서 바뀌면 Refresh로 IsFull을 갱신한다
        private void CapacityRefreshFollowsChangedCapacity()
        {
            long capacity = 4;
            var cap = new CapacityFeature<string>(WeightOf, () => capacity);
            using var inv = new Inventory<string>(new DictionaryStore<string>()).With(cap);
            inv.Add("wood", 2);                                        // 4 — 꽉 참
            Assert.IsTrue(cap.IsFull);

            capacity = 10;
            Assert.IsTrue(cap.IsFull);                                 // 아직 모른다
            Assert.IsTrue(inv.CanAdd("wood", 3, out _));               // 게이트는 최신 상한을 본다
            cap.Refresh();
            Assert.IsFalse(cap.IsFull);
            Assert.AreEqual(6, cap.Free);
        }

        // 받는 쪽 용량 부족이면 TryMove 거부·양쪽 무변경, 통과 시 양쪽 Used 갱신
        private void CapacityMoveBetweenInventories()
        {
            var srcCap = new CapacityFeature<string>(WeightOf, () => 100);
            var dstCap = new CapacityFeature<string>(WeightOf, () => 6);
            using var src = new Inventory<string>(new DictionaryStore<string>()).With(srcCap);
            using var dst = new Inventory<string>(new DictionaryStore<string>()).With(dstCap);
            src.Add("stone", 3);                                       // 15

            Assert.IsFalse(src.TryMove(dst, "stone", 2));              // 10 > 6
            Assert.AreEqual(15, srcCap.Used);
            Assert.AreEqual(0, dstCap.Used);

            Assert.IsTrue(src.TryMove(dst, "stone", 1));
            Assert.AreEqual(10, srcCap.Used);
            Assert.AreEqual(5, dstCap.Used);
            Assert.AreEqual(2, src.Count("stone"));
            Assert.AreEqual(1, dst.Count("stone"));
        }

        // 필터는 추가만 거부하고 제거는 허용
        private void FilterRejectsOnlyAdd()
        {
            var filter = new FilterFeature<string>(k => k != "gold", "fuel.only");
            using var inv = new Inventory<string>(new DictionaryStore<string>(new Dictionary<string, long> { ["gold"] = 3 })).With(filter);

            Assert.IsTrue(filter.Accepts("wood"));
            Assert.IsFalse(filter.Accepts("gold"));
            Assert.IsFalse(inv.CanAdd("gold", 1, out var reason));
            Assert.AreEqual("fuel.only", reason);
            Assert.IsTrue(inv.CanRemove("gold", 1, out reason));
            Assert.IsNull(reason);
            Assert.IsTrue(inv.Remove("gold", 3));
            Assert.IsTrue(inv.Add("wood", 1));

            var defaultReason = new FilterFeature<string>(_ => false);
            Assert.AreEqual(InventoryReason.Rejected, defaultReason.Reason);
        }

        // 훅은 변경마다 호출, Rebind 시 onRebind 호출
        private void HookReceivesChangeAndRebind()
        {
            var received = new List<ItemChange<string>>();
            var rebinds = 0;
            var hook = new HookFeature<string>(received.Add, () => rebinds++);
            using var inv = new Inventory<string>(new DictionaryStore<string>()).With(hook);

            inv.Add("gold", 2, "loot");
            inv.Remove("gold", 1);
            inv.Rebind(new DictionaryStore<string>());

            Assert.AreEqual(2, received.Count);
            Assert.AreEqual("loot", received[0].Reason);
            Assert.AreEqual(1, received[1].After);
            Assert.AreEqual(1, rebinds);
            Assert.Throws<ArgumentNullException>(() => new HookFeature<string>(null));
        }
    }
}
