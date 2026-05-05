using System;
using System.Collections.Generic;
using Sindy.Reactive;
using Sindy.View;
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
        private class TestVM : ViewModel
        {
            public string Tag;
            public TestVM(string tag) { Tag = tag; }
            public override string ToString() => Tag ?? string.Empty;
        }

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
            AttachIsIdempotent_NoDoubleSubscribe();
            DetachWithoutAttach_IsNoOp();
        }

        // 멱등성: AttachListener를 두 번 호출해도 OnContentChanged가 한 번만 발행되어야 한다.
        private void AttachIsIdempotent_NoDoubleSubscribe()
        {
            var content = new ObservableList<TestVM>();
            var section = new Section<TestVM>(content, MakeOption());

            var fired = 0;
            ((ISection)section).OnContentChanged += _ => fired++;

            section.AttachListener();
            section.AttachListener();  // 재호출 — 이중 구독되어선 안 됨

            content.Add(new TestVM("A"));
            Assert.AreEqual(1, fired, "OnContentChanged가 이중 구독되어 두 번 발행되었습니다.");
        }

        // 부착되지 않은 상태의 DetachListener 호출은 no-op이며 isAttached 가드를 잘못 해제하지 않는다.
        private void DetachWithoutAttach_IsNoOp()
        {
            var section = new Section<TestVM>(new ObservableList<TestVM>(), MakeOption());

            // 부착하지 않은 상태에서 Detach 호출
            section.DetachListener();

            // 그 후 Attach → 부착 후 mutation은 여전히 throw해야 한다 (가드가 정상 작동)
            section.AttachListener();
            try
            {
                section.Header = new TestVM("h1");
                Assert.IsTrue(false, "AttachListener 후 Header mutation은 throw되어야 합니다.");
            }
            catch (System.InvalidOperationException) { /* 예상 */ }
        }

        // FR-CELL-06. ContentVMType은 ObservableList가 비어있어도 제네릭 매개변수에서 알 수 있다.
        private void ContentVMTypeIsKnownEvenIfEmpty()
        {
            var content = new ObservableList<TestVM>();
            var section = new Section<TestVM>(content, MakeOption());
            Assert.AreEqual(typeof(TestVM), section.ContentVMType);
            Assert.AreEqual(0, section.ContentCount);
        }

        private void IndexOfContentVM_FindsAndReturnsMinusOneOnMismatch()
        {
            var content = new ObservableList<TestVM>();
            var a = new TestVM("A");
            var b = new TestVM("B");
            var z = new TestVM("Z");
            content.Add(a);
            content.Add(b);
            var section = new Section<TestVM>(content, MakeOption());

            Assert.AreEqual(0, section.IndexOfContentVM(a));
            Assert.AreEqual(1, section.IndexOfContentVM(b));
            Assert.AreEqual(-1, section.IndexOfContentVM(z));
            // 다른 타입(=TVM이 아닌 IViewModel) → -1
            Assert.AreEqual(-1, section.IndexOfContentVM(new ViewModel()));
        }

        // ObservableList<TVM>의 5종 이벤트가 ISection.OnContentChanged(ListChange<object>)로
        // 동일 의미를 유지하며 어댑팅되는지 검증.
        private void ContentChangeEvents_AreAdaptedToObjectEvents()
        {
            var content = new ObservableList<TestVM>();
            var section = new Section<TestVM>(content, MakeOption());

            var events = new List<ListChange<IViewModel>>();
            ((ISection)section).OnContentChanged += e => events.Add(e);
            section.AttachListener();

            var a = new TestVM("A");
            var b = new TestVM("B");
            var a2 = new TestVM("A2");
            content.Add(a);
            content.Add(b);
            content[0] = a2;
            content.Move(0, 1);
            content.RemoveAt(0);
            content.Clear();

            // 6번 이벤트: Add(a, 0), Add(b, 1), Replace(a, a2, 0), Move(a2, 0->1), Remove(...,0), Reset()
            Assert.AreEqual(6, events.Count);

            Assert.AreEqual(ListChangeAction.Add, events[0].Action);
            Assert.AreEqual(0, events[0].NewIndex);
            Assert.AreEqual(a, events[0].NewItem);

            Assert.AreEqual(ListChangeAction.Add, events[1].Action);
            Assert.AreEqual(1, events[1].NewIndex);
            Assert.AreEqual(b, events[1].NewItem);

            Assert.AreEqual(ListChangeAction.Replace, events[2].Action);
            Assert.AreEqual(0, events[2].NewIndex);
            Assert.AreEqual(a, events[2].OldItem);
            Assert.AreEqual(a2, events[2].NewItem);

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
            var section = new Section<TestVM>(new ObservableList<TestVM>(), MakeOption());
            var h1 = new TestVM("h1");
            var f1 = new TestVM("f1");
            var e1 = new TestVM("e1");
            section.Header = h1;
            section.Footer = f1;
            section.EmptyContent = e1;
            Assert.AreEqual(h1, section.Header);
            Assert.AreEqual(f1, section.Footer);
            Assert.AreEqual(e1, section.EmptyContent);
        }

        // FR-CELL-06/07. SetSections로 부착된 상태(=AttachListener 호출됨) 이후의 mutation은 throw.
        private void HeaderMutationAfterAttach_Throws()
        {
            var section = new Section<TestVM>(new ObservableList<TestVM>(), MakeOption());
            section.AttachListener();
            try
            {
                section.Header = new TestVM("h1");
                Assert.IsTrue(false, "expected InvalidOperationException");
            }
            catch (InvalidOperationException) { /* 예상 */ }
        }

        private void FooterAndEmptyMutationAfterAttach_Throws()
        {
            var section = new Section<TestVM>(new ObservableList<TestVM>(), MakeOption());
            section.AttachListener();

            try { section.Footer = new TestVM("f1"); Assert.IsTrue(false); }
            catch (InvalidOperationException) { }

            try { section.EmptyContent = new TestVM("e1"); Assert.IsTrue(false); }
            catch (InvalidOperationException) { }
        }

        // DetachListener 호출 후에는 가드가 해제되어 다시 변경 가능 (다음 SetSections 재호출 시 재검증·재캐시됨).
        private void DetachReleasesGuard_AllowsMutationAgain()
        {
            var section = new Section<TestVM>(new ObservableList<TestVM>(), MakeOption());
            section.AttachListener();
            section.DetachListener();

            var h2 = new TestVM("h2");
            section.Header = h2;
            Assert.AreEqual(h2, section.Header);
        }
    }
}
