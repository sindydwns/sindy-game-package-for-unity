using System.Collections.Generic;
using Sindy.Reactive;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ReactiveSet — Add(중복무시), Remove, Contains, ContainsAll, Clear, Link/Unlink
    /// </summary>
    class TestReactiveSet : TestCase
    {
        public override void Run()
        {
            AddBasic();
            AddDuplicateIgnored();
            RemoveExisting();
            RemoveNonExisting();
            ContainsCheck();
            ContainsAllCheck();
            ClearFiresPerItem();
            CountTracked();
            LinkSyncsWithList();
            LinkReceivesFutureChanges();
            UnlinkStopsSyncing();
            EnumerationWorks();
        }

        private void AddBasic()
        {
            var set = new ReactiveSet<int>();

            set.Add(1);
            set.Add(2);

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        private void AddDuplicateIgnored()
        {
            var set = new ReactiveSet<int>();
            int addedCount = 0;
            set.OnAdded += _ => addedCount++;

            set.Add(1);
            set.Add(1);
            set.Add(1);

            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(1, addedCount);
        }

        private void RemoveExisting()
        {
            var set = new ReactiveSet<string>();
            set.Add("A");
            set.Add("B");

            var removed = new List<string>();
            set.OnRemoved += item => removed.Add(item);

            set.Remove("A");

            Assert.AreEqual(1, set.Count);
            Assert.IsFalse(set.Contains("A"));
            Assert.AreEqual(1, removed.Count);
            Assert.AreEqual("A", removed[0]);
        }

        private void RemoveNonExisting()
        {
            var set = new ReactiveSet<int>();
            set.Add(1);

            int removedCount = 0;
            set.OnRemoved += _ => removedCount++;

            set.Remove(99);

            Assert.AreEqual(1, set.Count);
            Assert.AreEqual(0, removedCount);
        }

        private void ContainsCheck()
        {
            var set = new ReactiveSet<int>();
            set.Add(10);

            Assert.IsTrue(set.Contains(10));
            Assert.IsFalse(set.Contains(20));
        }

        private void ContainsAllCheck()
        {
            var set = new ReactiveSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.IsTrue(set.ContainsAll(new[] { 1, 2 }));
            Assert.IsTrue(set.ContainsAll(new[] { 1, 2, 3 }));
            Assert.IsFalse(set.ContainsAll(new[] { 1, 4 }));
        }

        private void ClearFiresPerItem()
        {
            var set = new ReactiveSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            var removed = new List<int>();
            set.OnRemoved += item => removed.Add(item);

            set.Clear();

            Assert.AreEqual(0, set.Count);
            Assert.AreEqual(3, removed.Count);
        }

        private void CountTracked()
        {
            var set = new ReactiveSet<int>();
            Assert.AreEqual(0, set.Count);

            set.Add(1);
            set.Add(2);
            Assert.AreEqual(2, set.Count);

            set.Remove(1);
            Assert.AreEqual(1, set.Count);

            set.Clear();
            Assert.AreEqual(0, set.Count);
        }

        private void LinkSyncsWithList()
        {
            var list = new ReactiveList<string>();
            list.Add("A");
            list.Add("B");

            var set = new ReactiveSet<string>();
            set.Link(list);

            // Link 시 기존 리스트 요소가 Set에 추가됨
            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains("A"));
            Assert.IsTrue(set.Contains("B"));
        }

        private void LinkReceivesFutureChanges()
        {
            var list = new ReactiveList<string>();
            var set = new ReactiveSet<string>();
            set.Link(list);

            // Link 후 리스트 변경이 Set에 반영됨
            list.Add("X");
            Assert.IsTrue(set.Contains("X"));

            list.Remove("X");
            Assert.IsFalse(set.Contains("X"));
        }

        private void UnlinkStopsSyncing()
        {
            var list = new ReactiveList<string>();
            var set = new ReactiveSet<string>();
            set.Link(list);

            list.Add("A");
            Assert.IsTrue(set.Contains("A"));

            set.Unlink(list);

            // Unlink 후 리스트 변경이 Set에 반영되지 않음
            list.Add("B");
            Assert.IsFalse(set.Contains("B"));
        }

        private void EnumerationWorks()
        {
            var set = new ReactiveSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            int sum = 0;
            foreach (var item in set)
            {
                sum += item;
            }

            Assert.AreEqual(6, sum);
        }
    }
}
