namespace Sindy.Inven
{
    /// <summary>
    /// 인벤토리 변경 1건. Before/After가 있어 구독자가 경계(0 → 양수, 상한 도달)를 판정할 수 있다.
    /// </summary>
    public readonly struct ItemChange<TKey>
    {
        public readonly TKey Key;
        /// <summary>양수 = 추가, 음수 = 제거.</summary>
        public readonly long Delta;
        public readonly long Before;
        public readonly long After;
        /// <summary>호출자가 넘긴 사유(로그·원장용). 없으면 null.</summary>
        public readonly string Reason;

        public ItemChange(TKey key, long delta, long before, long after, string reason)
        {
            Key = key;
            Delta = delta;
            Before = before;
            After = after;
            Reason = reason;
        }

        public bool IsAdd => Delta > 0;
        public bool IsRemove => Delta < 0;
        /// <summary>0 → 양수. 새 항목이 생겼다.</summary>
        public bool BecameNonEmpty => Before == 0 && After > 0;
        /// <summary>양수 → 0. 항목이 사라졌다.</summary>
        public bool BecameEmpty => Before > 0 && After == 0;

        public override string ToString()
        {
            var sign = Delta >= 0 ? "+" : "";
            return $"{Key} {sign}{Delta} ({Before} → {After}){(Reason == null ? "" : $" [{Reason}]")}";
        }
    }
}
