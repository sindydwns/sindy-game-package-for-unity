using System.Collections.Generic;
using Sindy.Reactive;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ObservableList&lt;T&gt; — Add/Remove/Replace/Move/Reset 5종 이벤트와 인덱스 검증.
    /// FR-DATA-01, FR-DATA-02 충족 여부 확인.
    /// </summary>
    class TestObservableList : TestCase
    {
        public override void Run()
        {
            AddEmitsAddWithIndex();
            InsertEmitsAddWithIndex();
            RemoveEmitsRemoveWithIndex();
            RemoveAtEmitsRemoveWithIndex();
            IndexerSetEmitsReplaceWithIndices();
            MoveEmitsMoveWithBothIndices();
            MoveSameIndexIsNoop();
            ClearEmitsResetOnce();
            ResetWithItemsEmitsResetOnce();
            CountTracked();
            EnumerationWorks();
        }

        // Add는 ListChangeAction.Add 이벤트를 NewIndex=현재길이로 발행한다.
        private void AddEmitsAddWithIndex()
        {
            var list = new ObservableList<string>();
            ListChange<string>? last = null;
            list.OnChanged += e => last = e;

            list.Add("A");
            Assert.AreEqual(ListChangeAction.Add, last.Value.Action);
            Assert.AreEqual(0, last.Value.NewIndex);
            Assert.AreEqual("A", last.Value.NewItem);

            list.Add("B");
            Assert.AreEqual(ListChangeAction.Add, last.Value.Action);
            Assert.AreEqual(1, last.Value.NewIndex);
            Assert.AreEqual("B", last.Value.NewItem);
        }

        // Insert도 Add 이벤트를 지정한 index로 발행한다.
        private void InsertEmitsAddWithIndex()
        {
            var list = new ObservableList<string> { };
            list.Add("A");
            list.Add("C");

            ListChange<string>? last = null;
            list.OnChanged += e => last = e;

            list.Insert(1, "B");
            Assert.AreEqual(ListChangeAction.Add, last.Value.Action);
            Assert.AreEqual(1, last.Value.NewIndex);
            Assert.AreEqual("B", last.Value.NewItem);
        }

        // Remove는 OldIndex로 Remove 이벤트를 발행한다.
        private void RemoveEmitsRemoveWithIndex()
        {
            var list = new ObservableList<string>();
            list.Add("A"); list.Add("B"); list.Add("C");

            ListChange<string>? last = null;
            list.OnChanged += e => last = e;

            var removed = list.Remove("B");
            Assert.IsTrue(removed);
            Assert.AreEqual(ListChangeAction.Remove, last.Value.Action);
            Assert.AreEqual(1, last.Value.OldIndex);
            Assert.AreEqual("B", last.Value.OldItem);
        }

        private void RemoveAtEmitsRemoveWithIndex()
        {
            var list = new ObservableList<int> { };
            list.Add(10); list.Add(20); list.Add(30);

            ListChange<int>? last = null;
            list.OnChanged += e => last = e;

            list.RemoveAt(0);
            Assert.AreEqual(ListChangeAction.Remove, last.Value.Action);
            Assert.AreEqual(0, last.Value.OldIndex);
            Assert.AreEqual(10, last.Value.OldItem);
        }

        // 인덱서 set은 Replace 이벤트를 OldItem/NewItem과 함께 발행한다.
        private void IndexerSetEmitsReplaceWithIndices()
        {
            var list = new ObservableList<string>();
            list.Add("A"); list.Add("B"); list.Add("C");

            ListChange<string>? last = null;
            list.OnChanged += e => last = e;

            list[1] = "B2";
            Assert.AreEqual(ListChangeAction.Replace, last.Value.Action);
            Assert.AreEqual(1, last.Value.OldIndex);
            Assert.AreEqual(1, last.Value.NewIndex);
            Assert.AreEqual("B", last.Value.OldItem);
            Assert.AreEqual("B2", last.Value.NewItem);
        }

        // Move는 단일 Move 이벤트를 OldIndex/NewIndex와 함께 발행한다.
        private void MoveEmitsMoveWithBothIndices()
        {
            var list = new ObservableList<string>();
            list.Add("A"); list.Add("B"); list.Add("C"); list.Add("D");

            var events = new List<ListChange<string>>();
            list.OnChanged += e => events.Add(e);

            list.Move(0, 2);

            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(ListChangeAction.Move, events[0].Action);
            Assert.AreEqual(0, events[0].OldIndex);
            Assert.AreEqual(2, events[0].NewIndex);
            Assert.AreEqual("A", events[0].NewItem);
            // 결과: B C A D
            Assert.AreEqual("B", list[0]);
            Assert.AreEqual("C", list[1]);
            Assert.AreEqual("A", list[2]);
            Assert.AreEqual("D", list[3]);
        }

        // 동일 인덱스로 Move는 이벤트를 발행하지 않는다.
        private void MoveSameIndexIsNoop()
        {
            var list = new ObservableList<int>();
            list.Add(1); list.Add(2);

            var fired = 0;
            list.OnChanged += _ => fired++;

            list.Move(1, 1);
            Assert.AreEqual(0, fired);
        }

        // Clear는 항목별 Remove 이벤트가 아니라 단일 Reset 이벤트를 발행한다 (vs ReactiveList).
        private void ClearEmitsResetOnce()
        {
            var list = new ObservableList<string>();
            list.Add("A"); list.Add("B"); list.Add("C");

            var actions = new List<ListChangeAction>();
            list.OnChanged += e => actions.Add(e.Action);

            list.Clear();

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(ListChangeAction.Reset, actions[0]);
            Assert.AreEqual(0, list.Count);
        }

        private void ResetWithItemsEmitsResetOnce()
        {
            var list = new ObservableList<int>();
            list.Add(1); list.Add(2);

            var actions = new List<ListChangeAction>();
            list.OnChanged += e => actions.Add(e.Action);

            list.Reset(new[] { 10, 20, 30 });

            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual(ListChangeAction.Reset, actions[0]);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(10, list[0]);
            Assert.AreEqual(30, list[2]);
        }

        private void CountTracked()
        {
            var list = new ObservableList<int>();
            Assert.AreEqual(0, list.Count);
            list.Add(1); list.Add(2);
            Assert.AreEqual(2, list.Count);
            list.RemoveAt(0);
            Assert.AreEqual(1, list.Count);
            list.Clear();
            Assert.AreEqual(0, list.Count);
        }

        private void EnumerationWorks()
        {
            var list = new ObservableList<int>();
            list.Add(1); list.Add(2); list.Add(3);
            int sum = 0;
            foreach (var v in list) sum += v;
            Assert.AreEqual(6, sum);
        }
    }
}
