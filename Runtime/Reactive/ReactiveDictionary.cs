using System;
using System.Collections.Generic;

namespace Sindy.Reactive
{
    public class ReactiveDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> dictionary = new();

        public event Action<TKey, TValue> OnAdded;
        public event Action<TKey, TValue> OnRemoved;
        public event Action<TKey, TValue, TValue> OnUpdated;

        public void Add(TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                throw new ArgumentException($"Key {key} already exists in the dictionary.");
            }

            dictionary[key] = value;
            OnAdded?.Invoke(key, value);
        }

        public void Remove(TKey key)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException($"Key {key} not found in the dictionary.");
            }

            dictionary.Remove(key);
            OnRemoved?.Invoke(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public bool TryGetValue<T>(TKey key, out T value) where T : TValue
        {
            if (dictionary.TryGetValue(key, out var val) && val is T typedValue)
            {
                value = typedValue;
                return true;
            }
            value = default;
            return false;
        }

        public int Count => dictionary.Count;

        public IEnumerable<TKey> Keys => dictionary.Keys;

        public IEnumerable<TValue> Values => dictionary.Values;

        public TValue this[TKey key]
        {
            get
            {
                if (!dictionary.TryGetValue(key, out var value))
                {
                    throw new KeyNotFoundException($"Key {key} not found in the dictionary.");
                }
                return value;
            }
            set
            {
                if (dictionary.TryGetValue(key, out var oldValue))
                {
                    dictionary[key] = value;
                    OnUpdated?.Invoke(key, oldValue, value);
                }
                else
                {
                    dictionary[key] = value;
                    OnAdded?.Invoke(key, value);
                }
            }
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(TKey dataType)
        {
            return dictionary.ContainsKey(dataType);
        }
    }
}
