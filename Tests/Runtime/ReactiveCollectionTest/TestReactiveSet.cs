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

        // 기본 Add로 요소가 정상 추가되는지 확인
        private void AddBasic()
        {
            var set = new ReactiveSet<int>();

            set.Add(1);
            set.Add(2);

            Assert.AreEqual(2, set.Count);
            Assert.IsTrue(set.Contains(1));
            Assert.IsTrue(set.Contains(2));
        }

        // 중복 요소 Add 시 무시되고 OnAdded 이벤트가 1회만 발생하는지 확인
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

        // 존재하는 요소를 Remove하면 삭제되고 OnRemoved 이벤트가 발생하는지 확인
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

        // 존재하지 않는 요소를 Remove 시 무시되고 OnRemoved 이벤트가 발생하지 않는지 확인
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

        // Contains로 요소 포함 여부를 올바르게 판별하는지 확인
        private void ContainsCheck()
        {
            var set = new ReactiveSet<int>();
            set.Add(10);

            Assert.IsTrue(set.Contains(10));
            Assert.IsFalse(set.Contains(20));
        }

        // ContainsAll로 주어진 모든 요소가 포함되어 있는지 판별하는지 확인
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

        // Clear 시 각 요소마다 OnRemoved 이벤트가 개별 발생하는지 확인
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

        // Add/Remove/Clear에 따라 Count가 올바르게 추적되는지 확인
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

        // Link 시 기존 리스트의 요소가 Set에 동기화되는지 확인
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

        // Link 후 리스트에 Add/Remove하면 Set에 자동 반영되는지 확인
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

        // Unlink 후 리스트 변경이 Set에 반영되지 않는지 확인
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

        // foreach로 모든 요소를 순회할 수 있는지 확인
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
