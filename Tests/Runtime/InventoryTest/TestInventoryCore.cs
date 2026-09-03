using System;
using System.Collections.Generic;
using NUnit.Framework;
using R3;
using Sindy.Inven;

namespace Sindy.Test
{
    /// <summary>
    /// Inventory&lt;TKey&gt; 코어 — 게이트·원자적 이동·Feature 순서·재진입·Rebind·CountProp·Pay·Dispose
    /// </summary>
    class TestInventoryCore : TestCase
    {
        /// <summary>테스트용 Feature — 호출 기록, 게이트 거부, 재진입 시도</summary>
        private class ProbeFeature : InventoryFeature<string>
        {
            public readonly string Name;
            public readonly List<string> Log;
            public Func<string, long, bool> Reject;          // true면 거부
            public string RejectReason = "probe.reject";
            public bool ReenterOnChanged;
            public Exception Caught;
            public bool Detached;
            public int RebindCount;

            public ProbeFeature(string name, List<string> log)
            {
                Name = name;
                Log = log;
            }

            public override bool CanAccept(string key, long delta, out string reason)
            {
                if (Reject != null && Reject(key, delta))
                {
                    reason = RejectReason;
                    return false;
                }
                reason = null;
                return true;
            }

            public override void OnChanged(in ItemChange<string> change)
            {
                Log.Add(Name);
                if (ReenterOnChanged)
                {
                    try { Owner.Add("reenter", 1); }
                    catch (Exception e) { Caught = e; }
                }
            }

            public override void OnRebind() => RebindCount++;
            public override void Detach() { base.Detach(); Detached = true; }
        }

        /// <summary>같은 인벤에 Probe를 둘 얹기 위한 두 번째 타입(같은 타입 중복은 예외)</summary>
        private sealed class SecondProbeFeature : ProbeFeature
        {
            public SecondProbeFeature(string name, List<string> log) : base(name, log) { }
        }

        public override void Run()
        {
            AddRejectedByGateLeavesStateUntouchedAndReturnsReason();
            RemoveInsufficientReturnsReason();
            TryMoveIsAtomic();
            TryMoveToSelfThrows();
            FeatureOrderFollowsWith();
            DuplicateFeatureTypeThrows();
            ReentrantAddInsideOnChangedThrows();
            RebindKeepsSubscriptionsAndSkipsUnchanged();
            CountPropIsCreatedLazily();
            PayIsAllOrNothingAndAggregatesKeys();
            HasAllAggregatesKeys();
            ChangesCarryBeforeAfterAndReason();
            DisposeCompletesChangesAndDetachesFeatures();
            InvalidAmountThrows();
            ComparerIsUsedForCountPropAndAggregate();
        }

        private static Inventory<string> NewInventory(Dictionary<string, long> dict = null)
        {
            return new Inventory<string>(new DictionaryStore<string>(dict));
        }

        // 한 Feature라도 거부하면 store·CountProp·Changes 전부 무변경, reason은 그 Feature 것
        private void AddRejectedByGateLeavesStateUntouchedAndReturnsReason()
        {
            var log = new List<string>();
            var dict = new Dictionary<string, long>();
            using var inv = NewInventory(dict)
                .With(new ProbeFeature("a", log))
                .With(new SecondProbeFeature("b", log) { Reject = (k, d) => k == "bomb", RejectReason = "b.no_bomb" });
            var changes = 0;
            inv.Changes.Subscribe(_ => changes++).AddTo(disposables);
            var prop = inv.CountProp("bomb");
            var propEmits = 0;
            prop.Skip(1).Subscribe(_ => propEmits++).AddTo(disposables);

            Assert.IsFalse(inv.CanAdd("bomb", 1, out var reason));
            Assert.AreEqual("b.no_bomb", reason);
            Assert.IsFalse(inv.Add("bomb", 1));

            Assert.AreEqual(0, inv.Count("bomb"));
            Assert.IsFalse(dict.ContainsKey("bomb"));
            Assert.AreEqual(0, prop.CurrentValue);
            Assert.AreEqual(0, propEmits);
            Assert.AreEqual(0, changes);
            Assert.AreEqual(0, log.Count);

            Assert.IsTrue(inv.CanAdd("gold", 1, out reason));
            Assert.IsNull(reason);
            Assert.IsTrue(inv.Add("gold", 5));
            Assert.AreEqual(5, inv.Count("gold"));
            Assert.AreEqual(5, dict["gold"]);
            Assert.AreEqual(1, changes);
        }

        // 보유량 부족이면 Insufficient 이유로 거부, 무변경
        private void RemoveInsufficientReturnsReason()
        {
            using var inv = NewInventory();
            inv.Add("gold", 3);

            Assert.IsFalse(inv.CanRemove("gold", 4, out var reason));
            Assert.AreEqual(InventoryReason.Insufficient, reason);
            Assert.IsFalse(inv.Remove("gold", 4));
            Assert.AreEqual(3, inv.Count("gold"));

            Assert.IsTrue(inv.Remove("gold", 3));
            Assert.AreEqual(0, inv.Count("gold"));
            Assert.IsFalse(inv.Remove("gold", 1));
        }

        // 받는 쪽 거부 시 주는 쪽도 무변경. 통과 시 총량 보존
        private void TryMoveIsAtomic()
        {
            using var src = NewInventory();
            using var dst = NewInventory().With(new FilterFeature<string>(k => k != "bomb", "dst.no_bomb"));
            src.Add("bomb", 2);
            src.Add("gold", 10);
            var srcChanges = 0;
            var dstChanges = 0;
            src.Changes.Subscribe(_ => srcChanges++).AddTo(disposables);
            dst.Changes.Subscribe(_ => dstChanges++).AddTo(disposables);

            Assert.IsFalse(src.CanMove(dst, "bomb", 1, out var reason));
            Assert.AreEqual("dst.no_bomb", reason);
            Assert.IsFalse(src.TryMove(dst, "bomb", 1));
            Assert.AreEqual(2, src.Count("bomb"));
            Assert.AreEqual(0, dst.Count("bomb"));
            Assert.AreEqual(0, srcChanges);
            Assert.AreEqual(0, dstChanges);

            Assert.IsFalse(src.TryMove(dst, "gold", 11));          // 부족
            Assert.AreEqual(10, src.Count("gold"));

            Assert.IsTrue(src.TryMove(dst, "gold", 4, "trade"));
            Assert.AreEqual(6, src.Count("gold"));
            Assert.AreEqual(4, dst.Count("gold"));
            Assert.AreEqual(1, srcChanges);
            Assert.AreEqual(1, dstChanges);
        }

        private void TryMoveToSelfThrows()
        {
            using var inv = NewInventory();
            inv.Add("gold", 1);
            Assert.Throws<ArgumentException>(() => inv.TryMove(inv, "gold", 1));
            Assert.Throws<ArgumentNullException>(() => inv.TryMove(null, "gold", 1));
        }

        // OnChanged 호출 순서 = 등록 순서, Changes는 Feature 뒤에
        private void FeatureOrderFollowsWith()
        {
            var log = new List<string>();
            using var inv = NewInventory()
                .With(new ProbeFeature("first", log))
                .With(new HookFeature<string>(_ => log.Add("hook")));
            inv.Changes.Subscribe(_ => log.Add("changes")).AddTo(disposables);

            inv.Add("gold", 1);

            Assert.AreEqual(new[] { "first", "hook", "changes" }, log.ToArray());
            Assert.IsNotNull(inv.Feature<ProbeFeature>());
            Assert.IsNotNull(inv.Feature<HookFeature<string>>());
            Assert.IsNull(inv.Feature<FilterFeature<string>>());
            Assert.AreEqual(2, inv.Features.Count);
        }

        private void DuplicateFeatureTypeThrows()
        {
            var log = new List<string>();
            using var inv = NewInventory().With(new ProbeFeature("a", log));
            Assert.Throws<InvalidOperationException>(() => inv.With(new ProbeFeature("b", log)));
            Assert.Throws<ArgumentNullException>(() => inv.With(null));
        }

        // Feature가 OnChanged 안에서 Add를 호출하면 예외. 바깥 호출은 정상 완료되고 다음 Feature도 호출된다
        private void ReentrantAddInsideOnChangedThrows()
        {
            var log = new List<string>();
            var probe = new ProbeFeature("reenter", log) { ReenterOnChanged = true };
            using var inv = NewInventory().With(probe).With(new SecondProbeFeature("after", log));

            Assert.IsTrue(inv.Add("gold", 1));

            Assert.IsInstanceOf<InvalidOperationException>(probe.Caught);
            Assert.AreEqual(0, inv.Count("reenter"));
            Assert.AreEqual(1, inv.Count("gold"));
            Assert.AreEqual(new[] { "reenter", "after" }, log.ToArray());

            // 재진입이 아닌 다음 호출은 정상
            probe.ReenterOnChanged = false;
            Assert.IsTrue(inv.Add("gold", 1));
            Assert.AreEqual(2, inv.Count("gold"));
        }

        // 스토어 교체 후 기존 CountProp 구독이 새 값을 받는다. 같은 값이면 방출 없음. Feature.OnRebind 호출
        private void RebindKeepsSubscriptionsAndSkipsUnchanged()
        {
            var log = new List<string>();
            var probe = new ProbeFeature("p", log);
            using var inv = NewInventory(new Dictionary<string, long> { ["gold"] = 5, ["wood"] = 3 }).With(probe);
            var goldValues = new List<long>();
            var woodValues = new List<long>();
            inv.CountProp("gold").Subscribe(goldValues.Add).AddTo(disposables);
            inv.CountProp("wood").Subscribe(woodValues.Add).AddTo(disposables);

            var newStore = new DictionaryStore<string>(new Dictionary<string, long> { ["gold"] = 9, ["wood"] = 3 });
            inv.Rebind(newStore);

            Assert.AreSame(newStore, inv.Store);
            Assert.AreEqual(new long[] { 5, 9 }, goldValues.ToArray());
            Assert.AreEqual(new long[] { 3 }, woodValues.ToArray());      // 같은 값 → 방출 없음
            Assert.AreEqual(1, probe.RebindCount);
            Assert.AreEqual(9, inv.Count("gold"));

            inv.Add("gold", 1);
            Assert.AreEqual(10, newStore.Dictionary["gold"]);
            Assert.AreEqual(new long[] { 5, 9, 10 }, goldValues.ToArray());
        }

        // 요청한 키만 생성되고 같은 키는 같은 인스턴스
        private void CountPropIsCreatedLazily()
        {
            using var inv = NewInventory(new Dictionary<string, long> { ["gold"] = 1, ["wood"] = 2 });
            var gold = inv.CountProp("gold");
            Assert.AreSame(gold, inv.CountProp("gold"));
            Assert.AreEqual(1, gold.CurrentValue);

            var emitted = new List<long>();
            gold.Subscribe(emitted.Add).AddTo(disposables);
            inv.Add("gold", 2);
            inv.Add("wood", 5);                                        // 미구독 키 — 영향 없음
            Assert.AreEqual(new long[] { 1, 3 }, emitted.ToArray());

            var none = inv.CountProp("none");                          // 없는 키도 0으로 생성
            Assert.AreEqual(0, none.CurrentValue);
        }

        // 전부 CanRemove 통과 시에만 전부 제거. 같은 키는 합산
        private void PayIsAllOrNothingAndAggregatesKeys()
        {
            using var inv = NewInventory(new Dictionary<string, long> { ["gold"] = 10, ["wood"] = 4 });
            var changes = 0;
            inv.Changes.Subscribe(_ => changes++).AddTo(disposables);

            var tooMuch = new[]
            {
                new KeyValuePair<string, long>("gold", 3),
                new KeyValuePair<string, long>("wood", 5),             // 부족
            };
            Assert.IsFalse(inv.Pay(tooMuch));
            Assert.AreEqual(10, inv.Count("gold"));
            Assert.AreEqual(4, inv.Count("wood"));
            Assert.AreEqual(0, changes);

            var duplicated = new[]
            {
                new KeyValuePair<string, long>("wood", 3),
                new KeyValuePair<string, long>("wood", 2),             // 합산 5 > 4
            };
            Assert.IsFalse(inv.Pay(duplicated));
            Assert.AreEqual(4, inv.Count("wood"));

            var ok = new[]
            {
                new KeyValuePair<string, long>("gold", 3),
                new KeyValuePair<string, long>("gold", 2),
                new KeyValuePair<string, long>("wood", 4),
                new KeyValuePair<string, long>("stone", 0),           // 0은 무시
            };
            Assert.IsTrue(inv.Pay(ok, "craft"));
            Assert.AreEqual(5, inv.Count("gold"));
            Assert.AreEqual(0, inv.Count("wood"));
            Assert.AreEqual(2, changes);                               // 키마다 1건

            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Pay(new[] { new KeyValuePair<string, long>("gold", -1) }));
        }

        private void HasAllAggregatesKeys()
        {
            using var inv = NewInventory(new Dictionary<string, long> { ["gold"] = 5 });
            Assert.IsTrue(inv.HasAll(new[] { new KeyValuePair<string, long>("gold", 5) }));
            Assert.IsFalse(inv.HasAll(new[] { new KeyValuePair<string, long>("gold", 3), new KeyValuePair<string, long>("gold", 3) }));
            Assert.IsFalse(inv.HasAll(new[] { new KeyValuePair<string, long>("none", 1) }));
            Assert.IsTrue(inv.HasAll(Array.Empty<KeyValuePair<string, long>>()));
        }

        private void ChangesCarryBeforeAfterAndReason()
        {
            using var inv = NewInventory();
            var list = new List<ItemChange<string>>();
            inv.Changes.Subscribe(list.Add).AddTo(disposables);

            inv.Add("gold", 3, "loot");
            inv.Remove("gold", 3, "sell");

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("gold", list[0].Key);
            Assert.AreEqual(3, list[0].Delta);
            Assert.AreEqual(0, list[0].Before);
            Assert.AreEqual(3, list[0].After);
            Assert.AreEqual("loot", list[0].Reason);
            Assert.IsTrue(list[0].BecameNonEmpty);
            Assert.IsTrue(list[1].BecameEmpty);
            Assert.AreEqual(-3, list[1].Delta);
            Assert.AreEqual("sell", list[1].Reason);
        }

        // Changes 구독자가 OnCompleted 수신, Feature Detach, 이후 호출은 ObjectDisposedException
        private void DisposeCompletesChangesAndDetachesFeatures()
        {
            var log = new List<string>();
            var probe = new ProbeFeature("p", log);
            var inv = NewInventory().With(probe);
            var completed = false;
            inv.Changes.Subscribe(_ => { }, _ => completed = true).AddTo(disposables);
            inv.CountProp("gold");

            inv.Dispose();
            inv.Dispose();                                             // 멱등

            Assert.IsTrue(completed);
            Assert.IsTrue(probe.Detached);
            Assert.IsTrue(inv.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => inv.Add("gold", 1));
            Assert.Throws<ObjectDisposedException>(() => inv.CountProp("gold"));
        }

        private void InvalidAmountThrows()
        {
            using var inv = NewInventory();
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Add("gold", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => inv.Remove("gold", -1));
            Assert.Throws<ArgumentNullException>(() => new Inventory<string>(null));
            Assert.Throws<ArgumentNullException>(() => inv.Rebind(null));
        }

        // 비교자를 넘기면 CountProp 캐시·합산·스토어가 같은 키로 본다
        private void ComparerIsUsedForCountPropAndAggregate()
        {
            var store = new DictionaryStore<string>(comparer: StringComparer.OrdinalIgnoreCase);
            using var inv = new Inventory<string>(store, StringComparer.OrdinalIgnoreCase);
            var prop = inv.CountProp("Gold");
            inv.Add("gold", 2);
            inv.Add("GOLD", 1);

            Assert.AreEqual(3, prop.CurrentValue);
            Assert.AreSame(prop, inv.CountProp("GOLD"));
            Assert.IsTrue(inv.HasAll(new[] { new KeyValuePair<string, long>("gOLD", 2), new KeyValuePair<string, long>("Gold", 1) }));
            Assert.IsFalse(inv.HasAll(new[] { new KeyValuePair<string, long>("gOLD", 2), new KeyValuePair<string, long>("Gold", 2) }));
        }
    }
}
