using System;

namespace Sindy.Inven
{
    /// <summary>
    /// 받을 수 있는 키 제한(재화 거부, 연료만 허용 등). 제거는 항상 허용한다.
    /// </summary>
    public sealed class FilterFeature<TKey> : InventoryFeature<TKey>
    {
        private readonly Func<TKey, bool> accept;
        private readonly string reason;

        /// <param name="accept">true면 받는다.</param>
        /// <param name="reason">거부 이유 문자열.</param>
        public FilterFeature(Func<TKey, bool> accept, string reason = InventoryReason.Rejected)
        {
            this.accept = accept ?? throw new ArgumentNullException(nameof(accept));
            this.reason = reason;
        }

        public string Reason => reason;

        public bool Accepts(TKey key) => accept(key);

        public override bool CanAccept(TKey key, long delta, out string reason)
        {
            if (delta > 0 && !accept(key))
            {
                reason = this.reason;
                return false;
            }
            reason = null;
            return true;
        }
    }
}
