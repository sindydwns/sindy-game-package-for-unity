using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sindy.Reactive
{
    public class ReactiveSet<T> : IEnumerable<T>
    {
        private readonly HashSet<T> set = new();
        public event System.Action<T> OnAdded;
        public event System.Action<T> OnRemoved;
        public void Add(T item)
        {
            if (set.Add(item))
            {
                OnAdded?.Invoke(item);
            }
        }
        public void Remove(T item)
        {
            if (set.Remove(item))
            {
                OnRemoved?.Invoke(item);
            }
        }
        public bool Contains(T item) => set.Contains(item);
        public bool ContainsAll(IEnumerable<T> items) => items.All(x => set.Contains(x));
        public void Clear()
        {
            var old = set.Select(x => x).ToList();
            foreach (var item in old)
            {
                Remove(item);
            }
        }
        public int Count => set.Count;
        public IEnumerator<T> GetEnumerator() => set.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Link(ReactiveList<T> list)
        {
            foreach (var item in list)
            {
                Add(item);
            }
            list.OnAdded += Add;
            list.OnRemoved += Remove;
        }

        public void Unlink(ReactiveList<T> list)
        {
            list.OnAdded -= Add;
            list.OnRemoved -= Remove;
        }
    }
}
