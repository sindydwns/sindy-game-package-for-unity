using System.Collections.Generic;
using R3;

namespace Sindy.Inven
{
    /// <summary>패키지가 쓰는 기본 거부 이유 문자열.</summary>
    public static class InventoryReason
    {
        /// <summary>보유량 부족(제거·이동·지불).</summary>
        public const string Insufficient = "inventory.insufficient";
        /// <summary><see cref="CapacityFeature{TKey}"/> 기본값.</summary>
        public const string Full = "inventory.full";
        /// <summary><see cref="FilterFeature{TKey}"/> 기본값.</summary>
        public const string Rejected = "inventory.rejected";
    }

    public interface IReadOnlyInventory<TKey>
    {
        /// <summary>현재 수량. 없으면 0.</summary>
        long Count(TKey key);

        /// <summary>
        /// 키별 반응형 수량. 첫 요청 때 생성되고 Dispose 전까지 유지된다 — 구독한 키만 비용이 든다.
        /// 키 종류가 수천 개라면 <see cref="Entries"/> + <see cref="Changes"/>로 목록을 갱신하는 편이 맞다.
        /// </summary>
        ReadOnlyReactiveProperty<long> CountProp(TKey key);

        /// <summary>변경 스트림. Dispose 시 OnCompleted.</summary>
        Observable<ItemChange<TKey>> Changes { get; }

        /// <summary>보유 항목 전체(스토어 위임).</summary>
        IEnumerable<KeyValuePair<TKey, long>> Entries { get; }

        /// <summary>등록된 Feature를 타입으로 조회한다. 없으면 null. (ViewModel.Feature&lt;T&gt;()와 같은 관용구)</summary>
        T Feature<T>() where T : class, IInventoryFeature<TKey>;

        /// <summary>비용 전부를 보유하고 있는가(수량만 검사, 게이트는 보지 않는다). 같은 키는 합산한다.</summary>
        bool HasAll(IEnumerable<KeyValuePair<TKey, long>> costs);
    }

    public interface IInventory<TKey> : IReadOnlyInventory<TKey>
    {
        /// <summary>전 Feature 게이트 통과 여부. 거부면 reason에 거부한 Feature의 이유.</summary>
        bool CanAdd(TKey key, long n, out string reason);

        /// <summary>보유량 ≥ n 이고 전 Feature 게이트 통과.</summary>
        bool CanRemove(TKey key, long n, out string reason);

        /// <summary>게이트 통과 시에만 추가. 거부면 false, 상태 무변경.</summary>
        bool Add(TKey key, long n, string reason = null);

        /// <summary>게이트 통과 시에만 제거. 부족하거나 거부면 false, 상태 무변경.</summary>
        bool Remove(TKey key, long n, string reason = null);

        /// <summary>양쪽 게이트를 모두 통과했을 때만 한 호출 안에서 제거 → 추가. 실패 시 양쪽 무변경.</summary>
        bool TryMove(IInventory<TKey> to, TKey key, long n, string reason = null);

        /// <summary>전부 CanRemove 통과 시에만 전부 제거. 하나라도 실패하면 false, 상태 무변경. 같은 키는 합산한다.</summary>
        bool Pay(IEnumerable<KeyValuePair<TKey, long>> costs, string reason = null);

        /// <summary>스토어를 교체하고 기존 CountProp에 새 값을 재방출한다(구독은 유지). Feature 캐시는 OnRebind로 재계산.</summary>
        void Rebind(IInventoryStore<TKey> store);
    }
}
