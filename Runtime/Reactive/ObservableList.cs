using System;
using System.Collections;
using System.Collections.Generic;

namespace Sindy.Reactive
{
    public enum ListChangeAction
    {
        Add,
        Remove,
        Replace,
        Move,
        Reset,
    }

    public readonly struct ListChange<T>
    {
        public readonly ListChangeAction Action;
        public readonly T OldItem;
        public readonly T NewItem;
        public readonly int OldIndex;
        public readonly int NewIndex;

        private ListChange(ListChangeAction action, T oldItem, T newItem, int oldIndex, int newIndex)
        {
            Action = action;
            OldItem = oldItem;
            NewItem = newItem;
            OldIndex = oldIndex;
            NewIndex = newIndex;
        }

        public static ListChange<T> Add(T item, int index) => new(ListChangeAction.Add, default, item, -1, index);
        public static ListChange<T> Remove(T item, int index) => new(ListChangeAction.Remove, item, default, index, -1);
        public static ListChange<T> Replace(T oldItem, T newItem, int index) => new(ListChangeAction.Replace, oldItem, newItem, index, index);
        public static ListChange<T> Move(T item, int oldIndex, int newIndex) => new(ListChangeAction.Move, item, item, oldIndex, newIndex);
        public static ListChange<T> Reset() => new(ListChangeAction.Reset, default, default, -1, -1);
    }

    public interface IReadOnlyObservableList<T> : IReadOnlyList<T>
    {
        event Action<ListChange<T>> OnChanged;
    }

    /// <summary>
    /// FR-DATA-01, FR-DATA-02. Add/Remove/Replace/Move/Reset 5종의 변경 이벤트를 발행하는 컬렉션.
    /// 기존 ReactiveList는 Add/Remove만 지원하므로 별도 타입으로 도입한다.
    /// </summary>
    public class ObservableList<T> : IList<T>, IReadOnlyObservableList<T>
    {
        private readonly List<T> list;

        public event Action<ListChange<T>> OnChanged;

        public ObservableList() { list = new(); }
        public ObservableList(int capacity) { list = new(capacity); }
        public ObservableList(IEnumerable<T> items) { list = new(items); }

        public int Count => list.Count;
        public bool IsReadOnly => false;

        public T this[int index]
        {
            get => list[index];
            set
            {
                var old = list[index];
                list[index] = value;
                OnChanged?.Invoke(ListChange<T>.Replace(old, value, index));
            }
        }

        public void Add(T item)
        {
            var index = list.Count;
            list.Add(item);
            OnChanged?.Invoke(ListChange<T>.Add(item, index));
        }

        public void Insert(int index, T item)
        {
            list.Insert(index, item);
            OnChanged?.Invoke(ListChange<T>.Add(item, index));
        }

        public bool Remove(T item)
        {
            var index = list.IndexOf(item);
            if (index < 0) return false;
            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            var item = list[index];
            list.RemoveAt(index);
            OnChanged?.Invoke(ListChange<T>.Remove(item, index));
        }

        public void Move(int from, int to)
        {
            if (from == to) return;
            var item = list[from];
            list.RemoveAt(from);
            list.Insert(to, item);
            OnChanged?.Invoke(ListChange<T>.Move(item, from, to));
        }

        public void Clear()
        {
            list.Clear();
            OnChanged?.Invoke(ListChange<T>.Reset());
        }

        public void Reset(IEnumerable<T> items)
        {
            list.Clear();
            if (items != null) list.AddRange(items);
            OnChanged?.Invoke(ListChange<T>.Reset());
        }

        public bool Contains(T item) => list.Contains(item);
        public int IndexOf(T item) => list.IndexOf(item);
        public void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);
        public List<T>.Enumerator GetEnumerator() => list.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
