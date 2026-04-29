using System;
using System.Collections.Generic;
using Sindy.Reactive;
using Sindy.View.Scroller;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Section&lt;TVM&gt; — 콘텐츠 ObservableList 변경 이벤트의 비제네릭 어댑팅,
    /// IsAttached 가드(post-attach mutation 차단), Detach 후 재변경 가능 동작을 검증한다.
    /// FR-SEC-02, FR-CELL-06/07 정신과 부합하는지 단위 수준에서 확인한다.
    /// </summary>
    class TestSection : TestCase
    {
        private SectionOption MakeOption()
        {
            var opt = ScriptableObject.CreateInstance<SectionOption>();
            opt.CellMinWidth = 80f;
            opt.CellPreferredWidth = 120f;
            opt.CellMaxWidth = 200f;
            return opt;
        }

        public override void Run()
        {
            ContentVMTypeIsKnownEvenIfEmpty();
            IndexOfContentVM_FindsAndReturnsMinusOneOnMismatch();
            ContentChangeEvents_AreAdaptedToObjectEvents();
            HeaderMutationBeforeAttach_Allowed();
            HeaderMutationAfterAttach_Throws();
            FooterAndEmptyMutationAfterAttach_Throws();
            DetachReleasesGuard_AllowsMutationAgain();
        }

        // FR-CELL-06. ContentVMType은 ObservableList가 비어있어도 제네릭 매개변수에서 알 수 있다.
        private void ContentVMTypeIsKnownEvenIfEmpty()
        {
            var content = new ObservableList<string>();
            var section = new Section<string>(content, MakeOption());
            Assert.AreEqual(typeof(string), section.ContentVMType);
            Assert.AreEqual(0, section.ContentCount);
        }

        private void IndexOfContentVM_FindsAndReturnsMinusOneOnMismatch()
        {
            var content = new ObservableList<string>();
            content.Add("A");
            content.Add("B");
            var section = new Section<string>(content, MakeOption());

            Assert.AreEqual(0, section.IndexOfContentVM("A"));
            Assert.AreEqual(1, section.IndexOfContentVM("B"));
            Assert.AreEqual(-1, section.IndexOfContentVM("Z"));
            // 다른 타입 → -1
            Assert.AreEqual(-1, section.IndexOfContentVM(123));
        }

        // ObservableList<TVM>의 5종 이벤트가 ISection.OnContentChanged(ListChange<object>)로
        // 동일 의미를 유지하며 어댑팅되는지 검증.
        private void ContentChangeEvents_AreAdaptedToObjectEvents()
        {
            var content = new ObservableList<string>();
            var section = new Section<string>(content, MakeOption());

            var events = new List<ListChange<object>>();
            ((ISection)section).OnContentChanged += e => events.Add(e);
            section.AttachListener();

            content.Add("A");
            content.Add("B");
            content[0] = "A2";
            content.Move(0, 1);
            content.RemoveAt(0);
            content.Clear();

            // 6번 이벤트: Add("A", 0), Add("B", 1), Replace("A","A2", 0), Move("A2", 0->1), Remove(...,0), Reset()
            Assert.AreEqual(6, events.Count);

            Assert.AreEqual(ListChangeAction.Add, events[0].Action);
            Assert.AreEqual(0, events[0].NewIndex);
            Assert.AreEqual("A", events[0].NewItem);

            Assert.AreEqual(ListChangeAction.Add, events[1].Action);
            Assert.AreEqual(1, events[1].NewIndex);
            Assert.AreEqual("B", events[1].NewItem);

            Assert.AreEqual(ListChangeAction.Replace, events[2].Action);
            Assert.AreEqual(0, events[2].NewIndex);
            Assert.AreEqual("A", events[2].OldItem);
            Assert.AreEqual("A2", events[2].NewItem);

            Assert.AreEqual(ListChangeAction.Move, events[3].Action);
            Assert.AreEqual(0, events[3].OldIndex);
            Assert.AreEqual(1, events[3].NewIndex);

            Assert.AreEqual(ListChangeAction.Remove, events[4].Action);
            Assert.AreEqual(0, events[4].OldIndex);

            Assert.AreEqual(ListChangeAction.Reset, events[5].Action);
        }

        // 부착 전(=AttachListener 호출 전)에는 Header/Footer/EmptyContent가 자유롭게 설정된다.
        private void HeaderMutationBeforeAttach_Allowed()
        {
            var section = new Section<string>(new ObservableList<string>(), MakeOption());
            section.Header = "h1";
            section.Footer = "f1";
            section.EmptyContent = "e1";
            Assert.AreEqual("h1", section.Header);
            Assert.AreEqual("f1", section.Footer);
            Assert.AreEqual("e1", section.EmptyContent);
        }

        // FR-CELL-06/07. SetSections로 부착된 상태(=AttachListener 호출됨) 이후의 mutation은 throw.
        private void HeaderMutationAfterAttach_Throws()
        {
            var section = new Section<string>(new ObservableList<string>(), MakeOption());
            section.AttachListener();
            try
            {
                section.Header = "h1";
                Assert.IsTrue(false, "expected InvalidOperationException");
            }
            catch (InvalidOperationException) { /* 예상 */ }
        }

        private void FooterAndEmptyMutationAfterAttach_Throws()
        {
            var section = new Section<string>(new ObservableList<string>(), MakeOption());
            section.AttachListener();

            try { section.Footer = "f1"; Assert.IsTrue(false); }
            catch (InvalidOperationException) { }

            try { section.EmptyContent = "e1"; Assert.IsTrue(false); }
            catch (InvalidOperationException) { }
        }

        // DetachListener 호출 후에는 가드가 해제되어 다시 변경 가능 (다음 SetSections 재호출 시 재검증·재캐시됨).
        private void DetachReleasesGuard_AllowsMutationAgain()
        {
            var section = new Section<string>(new ObservableList<string>(), MakeOption());
            section.AttachListener();
            section.DetachListener();

            section.Header = "h2";
            Assert.AreEqual("h2", section.Header);
        }
    }
}
