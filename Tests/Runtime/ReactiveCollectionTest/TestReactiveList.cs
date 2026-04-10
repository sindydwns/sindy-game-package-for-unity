using System.Collections.Generic;
using Sindy.Reactive;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ReactiveList — Add, Insert, Remove, Clear, Contains, Find, indexer, events, OnChange
    /// </summary>
    class TestReactiveList : TestCase
    {
        public override void Run()
        {
            AddBasic();
            AddMultiple();
            InsertAtIndex();
            RemoveExisting();
            RemoveNonExisting();
            ClearFiresPerItem();
            ContainsCheck();
            FindPredicate();
            IndexerAccess();
            IndexOfCheck();
            CountTracked();
            AddRangeWorks();
            OnAddedEventFires();
            OnRemovedEventFires();
            OnChangeSubjectFires();
            EnumerationWorks();
        }

        private void AddBasic()
        {
            var list = new ReactiveList<string>();

            list.Add("A");

            Assert.AreEqual(1, list.Count);
            Assert.IsTrue(list.Contains("A"));
        }

        private void AddMultiple()
        {
            var list = new ReactiveList<string>();

            list.Add("A");
            list.Add("B");
            list.Add("C");

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("A", list[0]);
            Assert.AreEqual("B", list[1]);
            Assert.AreEqual("C", list[2]);
        }

        private void InsertAtIndex()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("C");

            list.Insert(1, "B");

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("A", list[0]);
            Assert.AreEqual("B", list[1]);
            Assert.AreEqual("C", list[2]);
        }

        private void RemoveExisting()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");

            list.Remove("A");

            Assert.AreEqual(1, list.Count);
            Assert.IsFalse(list.Contains("A"));
            Assert.IsTrue(list.Contains("B"));
        }

        private void RemoveNonExisting()
        {
            var list = new ReactiveList<string>();
            list.Add("A");

            int removedCount = 0;
            list.OnRemoved += _ => removedCount++;

            list.Remove("Z");

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(0, removedCount);
        }

        private void ClearFiresPerItem()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");
            list.Add("C");

            var removed = new List<string>();
            list.OnRemoved += item => removed.Add(item);

            list.Clear();

            Assert.AreEqual(0, list.Count);
            Assert.AreEqual(3, removed.Count);
        }

        private void ContainsCheck()
        {
            var list = new ReactiveList<int>();
            list.Add(1);
            list.Add(2);

            Assert.IsTrue(list.Contains(1));
            Assert.IsFalse(list.Contains(99));
        }

        private void FindPredicate()
        {
            var list = new ReactiveList<string>();
            list.Add("apple");
            list.Add("banana");
            list.Add("cherry");

            var found = list.Find(x => x.StartsWith("b"));

            Assert.AreEqual("banana", found);

            var notFound = list.Find(x => x.StartsWith("z"));

            Assert.IsNull(notFound);
        }

        private void IndexerAccess()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");

            Assert.AreEqual("A", list[0]);
            Assert.AreEqual("B", list[1]);
        }

        private void IndexOfCheck()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");
            list.Add("C");

            Assert.AreEqual(1, list.IndexOf("B"));
            Assert.AreEqual(-1, list.IndexOf("Z"));
        }

        private void CountTracked()
        {
            var list = new ReactiveList<int>();
            Assert.AreEqual(0, list.Count);

            list.Add(1);
            list.Add(2);
            Assert.AreEqual(2, list.Count);

            list.Remove(1);
            Assert.AreEqual(1, list.Count);

            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        private void AddRangeWorks()
        {
            var list = new ReactiveList<int>();

            var added = new List<int>();
            list.OnAdded += item => added.Add(item);

            list.AddRange(new[] { 1, 2, 3 });

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, added.Count);
        }

        private void OnAddedEventFires()
        {
            var list = new ReactiveList<string>();
            var added = new List<string>();
            list.OnAdded += item => added.Add(item);

            list.Add("A");
            list.Insert(0, "B");

            Assert.AreEqual(2, added.Count);
            Assert.AreEqual("A", added[0]);
            Assert.AreEqual("B", added[1]);
        }

        private void OnRemovedEventFires()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");

            var removed = new List<string>();
            list.OnRemoved += item => removed.Add(item);

            list.Remove("A");

            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual("A", removed[0]);
        }

        private void OnChangeSubjectFires()
        {
            var list = new ReactiveList<string>();
            var changes = new List<ReactiveList<string>.ChangeEvent>();
            list.OnChange.Subscribe(e => changes.Add(e)).AddTo(disposables);

            list.Add("A");
            list.Remove("A");

            Assert.AreEqual(2, changes.Count);
            Assert.AreEqual(ReactiveList<string>.ChangeType.Add, changes[0].Type);
            Assert.AreEqual("A", changes[0].Item);
            Assert.AreEqual(ReactiveList<string>.ChangeType.Remove, changes[1].Type);
            Assert.AreEqual("A", changes[1].Item);
        }

        private void EnumerationWorks()
        {
            var list = new ReactiveList<int>();
            list.Add(10);
            list.Add(20);
            list.Add(30);

            int sum = 0;
            foreach (var item in list)
            {
                sum += item;
            }

            Assert.AreEqual(60, sum);
        }
    }
}
