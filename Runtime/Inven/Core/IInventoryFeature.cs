namespace Sindy.Inven
{
    /// <summary>
    /// 인벤토리 관심사 하나(용량·필터·훅·프로젝트 고유 규칙).
    /// 호출 순서는 <see cref="Inventory{TKey}.With"/> 등록 순서.
    /// <para><see cref="OnChanged"/> 안에서 인벤토리를 다시 바꾸면 안 된다(재진입 가드가 예외를 던진다).
    /// <see cref="OnChanged"/>는 예외를 던지지 않는 것이 계약이다 — 던지면 로그만 남기고 다음 Feature로 진행한다.</para>
    /// </summary>
    public interface IInventoryFeature<TKey>
    {
        /// <summary>등록 시 1회. 초기 캐시 계산은 여기서.</summary>
        void Attach(IInventory<TKey> owner);

        /// <summary>
        /// 사전 게이트. delta &gt; 0 = 추가, delta &lt; 0 = 제거.
        /// 통과면 reason = null. 거부면 이유 문자열을 채우고 false.
        /// </summary>
        bool CanAccept(TKey key, long delta, out string reason);

        /// <summary>사후 훅. 상태는 이미 바뀐 뒤다.</summary>
        void OnChanged(in ItemChange<TKey> change);

        /// <summary>스토어 교체(<see cref="IInventory{TKey}.Rebind"/>) 후. 캐시를 전체 재계산한다.</summary>
        void OnRebind();

        /// <summary>인벤토리 Dispose 시 1회.</summary>
        void Detach();
    }

    /// <summary>
    /// <see cref="IInventoryFeature{TKey}"/>의 기본 구현. 필요한 메서드만 override 한다.
    /// </summary>
    public abstract class InventoryFeature<TKey> : IInventoryFeature<TKey>
    {
        /// <summary>등록된 인벤토리. Detach 후 null.</summary>
        protected IInventory<TKey> Owner { get; private set; }

        public virtual void Attach(IInventory<TKey> owner)
        {
            Owner = owner;
        }

        public virtual bool CanAccept(TKey key, long delta, out string reason)
        {
            reason = null;
            return true;
        }

        public virtual void OnChanged(in ItemChange<TKey> change) { }

        public virtual void OnRebind() { }

        public virtual void Detach()
        {
            Owner = null;
        }
    }
}
