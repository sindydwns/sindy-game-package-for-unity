using System;

namespace Sindy.Inven
{
    /// <summary>
    /// 사후 훅을 델리게이트로 — 프로젝트 이벤트 버스 발행용. 게이트는 없다.
    /// 훅 안에서 인벤토리를 다시 바꾸면 안 된다(재진입 예외).
    /// </summary>
    public sealed class HookFeature<TKey> : InventoryFeature<TKey>
    {
        private readonly Action<ItemChange<TKey>> onChanged;
        private readonly Action onRebind;

        /// <param name="onChanged">변경 1건마다 호출.</param>
        /// <param name="onRebind">스토어 교체 후 호출(선택).</param>
        public HookFeature(Action<ItemChange<TKey>> onChanged, Action onRebind = null)
        {
            this.onChanged = onChanged ?? throw new ArgumentNullException(nameof(onChanged));
            this.onRebind = onRebind;
        }

        public override void OnChanged(in ItemChange<TKey> change)
        {
            onChanged(change);
        }

        public override void OnRebind()
        {
            onRebind?.Invoke();
        }
    }
}
