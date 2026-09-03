using System;
using R3;

namespace Sindy.Inven
{
    /// <summary>
    /// 수치 용량. 무게·슬롯 수·부피 전부 이것 하나 — 키별 비용과 상한을 델리게이트로 받는다.
    /// <para>사용량은 증분 캐시(<see cref="UsedProp"/>), <see cref="OnRebind"/>·<see cref="Refresh"/>에서 전체 재계산.</para>
    /// <para>상한(<c>capacity</c>)이 바깥에서 바뀌면 <see cref="Refresh"/>를 호출해야 <see cref="IsFullProp"/>가 따라온다.</para>
    /// </summary>
    public sealed class CapacityFeature<TKey> : InventoryFeature<TKey>, IDisposable
    {
        private readonly Func<TKey, long> costOf;
        private readonly Func<long> capacity;
        private readonly Func<bool> ignoreCap;
        private readonly string reason;
        private readonly ReactiveProperty<long> used = new(0);
        private readonly ReactiveProperty<bool> isFull = new(false);

        /// <param name="costOf">키 하나당 비용(무게·슬롯 1·부피). 0이면 용량을 차지하지 않는다.</param>
        /// <param name="capacity">상한. 매 게이트마다 호출되므로 바뀌는 값을 넘겨도 된다.</param>
        /// <param name="ignoreCap">true를 돌려주면 게이트를 통과시킨다(치트·이벤트).</param>
        /// <param name="reason">거부 이유 문자열.</param>
        public CapacityFeature(Func<TKey, long> costOf, Func<long> capacity, Func<bool> ignoreCap = null, string reason = InventoryReason.Full)
        {
            this.costOf = costOf ?? throw new ArgumentNullException(nameof(costOf));
            this.capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
            this.ignoreCap = ignoreCap;
            this.reason = reason;
        }

        /// <summary>현재 사용량(비용 × 수량의 합).</summary>
        public ReadOnlyReactiveProperty<long> UsedProp => used;
        public long Used => used.Value;
        public long Capacity => capacity();
        public long Free => Capacity - Used;
        /// <summary>Used ≥ Capacity. 사용량 변경·Refresh 시 갱신.</summary>
        public ReadOnlyReactiveProperty<bool> IsFullProp => isFull;
        public bool IsFull => isFull.Value;
        public string Reason => reason;

        public long CostOf(TKey key) => costOf(key);

        public override void Attach(IInventory<TKey> owner)
        {
            base.Attach(owner);
            Refresh();
        }

        public override bool CanAccept(TKey key, long delta, out string reason)
        {
            reason = null;
            if (delta <= 0) return true;                       // 제거는 항상 허용
            if (ignoreCap != null && ignoreCap()) return true;
            var cost = costOf(key);
            if (cost <= 0) return true;
            if (used.Value + cost * delta > capacity())
            {
                reason = this.reason;
                return false;
            }
            return true;
        }

        public override void OnChanged(in ItemChange<TKey> change)
        {
            var cost = costOf(change.Key);
            if (cost == 0) return;
            used.Value += cost * change.Delta;
            isFull.Value = used.Value >= capacity();
        }

        public override void OnRebind()
        {
            Refresh();
        }

        /// <summary>사용량을 전체 재계산하고 IsFull을 갱신한다. 상한이 바깥에서 바뀌었을 때도 호출한다.</summary>
        public void Refresh()
        {
            long sum = 0;
            if (Owner != null)
            {
                foreach (var kv in Owner.Entries)
                {
                    sum += costOf(kv.Key) * kv.Value;
                }
            }
            used.Value = sum;
            isFull.Value = sum >= capacity();
        }

        public override void Detach()
        {
            base.Detach();
            Dispose();
        }

        public void Dispose()
        {
            used.Dispose();
            isFull.Dispose();
        }
    }
}
