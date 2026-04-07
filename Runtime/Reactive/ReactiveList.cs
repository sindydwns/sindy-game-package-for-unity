using System;
using System.Collections;
using System.Collections.Generic;
using R3;

namespace Sindy.Reactive
{
    public interface IReadOnlyReactiveList<T> : IEnumerable<T>
    {
        event Action<T> OnAdded;
        event Action<T> OnRemoved;
        public bool Contains(T item);
        public int Count { get; }
    }

    public class ReactiveList<T> : IReadOnlyReactiveList<T>
    {
        private readonly List<T> list = new();
        public Subject<ChangeEvent> OnChange = new();
        public event Action<T> OnAdded;
        public event Action<T> OnRemoved;
        public void Add(T item)
        {
            list.Add(item);
            OnAdded?.Invoke(item);
            OnChange.OnNext(new(ChangeType.Add, item));
        }
        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            OnAdded?.Invoke(item);
            OnChange.OnNext(new(ChangeType.Add, item));
        }
        public void Remove(T item)
        {
            if (list.Remove(item))
            {
                OnRemoved?.Invoke(item);
                OnChange.OnNext(new(ChangeType.Remove, item));
            }
        }
        public bool Contains(T item) => list.Contains(item);
        public void Clear()
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                list.RemoveAt(i);
                OnRemoved?.Invoke(item);
                OnChange.OnNext(new(ChangeType.Remove, item));
            }
        }
        public int Count => list.Count;
        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int IndexOf(T item) => list.IndexOf(item);

        public T Find(Func<T, bool> value)
        {
            foreach (var item in list)
            {
                if (value(item))
                {
                    return item;
                }
            }
            return default;
        }

        internal void AddRange(IEnumerable<T> options)
        {
            foreach (var option in options)
            {
                Add(option);
            }
        }

        public T this[int index] => list[index];

        public enum ChangeType
        {
            Add,
            Remove,
        }

        public readonly struct ChangeEvent
        {
            public ChangeType Type { get; }
            public T Item { get; }

            public ChangeEvent(ChangeType type, T item)
            {
                Type = type;
                Item = item;
            }
        }
    }
}
