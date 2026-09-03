using System.Collections.Generic;

namespace Sindy.Inven
{
    /// <summary>
    /// <see cref="Dictionary{TKey,TValue}"/>를 그대로 감싼다. 테스트·간단한 프로젝트용.
    /// 수량이 0이 되면 항목을 지운다(<c>keepZero</c>로 유지 가능).
    /// </summary>
    public sealed class DictionaryStore<TKey> : IInventoryStore<TKey>
    {
        private readonly Dictionary<TKey, long> dict;
        private readonly bool keepZero;

        /// <param name="dict">감쌀 사전. null이면 새로 만든다.</param>
        /// <param name="comparer">dict가 null일 때 새 사전의 비교자.</param>
        /// <param name="keepZero">true면 수량 0인 항목을 지우지 않는다.</param>
        public DictionaryStore(Dictionary<TKey, long> dict = null, IEqualityComparer<TKey> comparer = null, bool keepZero = false)
        {
            this.dict = dict ?? new Dictionary<TKey, long>(comparer);
            this.keepZero = keepZero;
        }

        /// <summary>감싼 사전(직렬화·검사용).</summary>
        public Dictionary<TKey, long> Dictionary => dict;

        public bool TryGet(TKey key, out long count)
        {
            return dict.TryGetValue(key, out count);
        }

        public void Set(TKey key, long count)
        {
            if (count == 0 && !keepZero)
            {
                dict.Remove(key);
            }
            else
            {
                dict[key] = count;
            }
        }

        public IEnumerable<KeyValuePair<TKey, long>> All()
        {
            return dict;
        }
    }
}
