using System.Collections.Generic;

namespace Sindy.Inven
{
    /// <summary>
    /// 저장소 — 프로젝트가 구현한다. 패키지는 저장 형식(JSON·SO·서버 응답)을 모른다.
    /// 스토어에는 이벤트가 없다. 반응형은 <see cref="Inventory{TKey}"/>가 담당한다.
    /// </summary>
    public interface IInventoryStore<TKey>
    {
        /// <summary>항목이 없으면 false(count = 0).</summary>
        bool TryGet(TKey key, out long count);

        /// <summary>수량을 기록한다. 0으로 내려갔을 때 항목을 지울지는 스토어가 정한다.</summary>
        void Set(TKey key, long count);

        /// <summary>보유 항목 전체. 수량 0인 항목을 돌려줄지도 스토어가 정한다.</summary>
        IEnumerable<KeyValuePair<TKey, long>> All();
    }
}
