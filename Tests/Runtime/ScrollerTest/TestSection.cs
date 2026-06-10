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
    /// 비제네릭 Section — 콘텐츠 ObservableList 변경 이벤트 전달,
    /// IsAttached 가드(post-attach mutation 차단), Detach 후 재변경 가능 동작,
    /// 셀 키/명시 prefab 설정 가드를 검증한다.
    /// FR-SEC-02(셀 키로 대체), FR-CELL-06/07 정신과 부합하는지 단위 수준에서 확인한다.
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
            ContentCountAndIndexOf();
            ContentChangeEvents_AreDelivered();
            MutationBeforeAttach_Allowed();
            MutationAfterAttach_Throws();
            DetachReleasesGuard_AllowsMutationAgain();
            AttachIsIdempotent_NoDoubleSubscribe();
            DetachWithoutAttach_IsNoOp();
        }

        private void ContentCountAndIndexOf()
        {
            var content = new ObservableList<IViewModel>();
            var a = new TestVM("A");
            var b = new TestVM("B");
            var z = new TestVM("Z");
            content.Add(a);
            content.Add(b);
            var section = new Section(content, MakeOption());

            Assert.AreEqual(2, section.ContentCount);
            Assert.AreEqual(0, section.IndexOfContentVM(a));
            Assert.AreEqual(1, section.IndexOfContentVM(b));
            Assert.AreEqual(-1, section.IndexOfContentVM(z));
            Assert.AreEqual(a, section.GetContentVMAt(0));
        }

        // ObservableList의 5종 이벤트가 Section.OnContentChanged로 동일 의미로 전달되는지 검증.
        private void ContentChangeEvents_AreDelivered()
        {
            var content = new ObservableList<IViewModel>();
            var section = new Section(content, MakeOption());

            var events = new List<ListChange<IViewModel>>();
            section.OnContentChanged += e => events.Add(e);
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

        // 부착 전(=AttachListener 호출 전)에는 모든 슬롯 설정이 자유롭다.
        private void MutationBeforeAttach_Allowed()
        {
            var section = new Section(new ObservableList<IViewModel>(), MakeOption());
            var h1 = new TestVM("h1");
            var f1 = new TestVM("f1");
            var e1 = new TestVM("e1");
            section.ContentKey = "item";
            section.Header = h1;
            section.HeaderKey = "header";
            section.Footer = f1;
            section.EmptyContent = e1;
            Assert.AreEqual("item", section.ContentKey);
            Assert.AreEqual(h1, section.Header);
            Assert.AreEqual("header", section.HeaderKey);
            Assert.AreEqual(f1, section.Footer);
            Assert.AreEqual(e1, section.EmptyContent);
        }

        // FR-CELL-06/07. 스크롤러에 부착된 상태(=AttachListener 호출됨) 이후의 mutation은 throw.
        private void MutationAfterAttach_Throws()
        {
            var section = new Section(new ObservableList<IViewModel>(), MakeOption());
            section.AttachListener();

            try { section.Header = new TestVM("h1"); Assert.IsTrue(false, "expected InvalidOperationException"); }
            catch (InvalidOperationException) { /* 예상 */ }

            try { section.ContentKey = "item"; Assert.IsTrue(false); }
            catch (InvalidOperationException) { }

            try { section.Footer = new TestVM("f1"); Assert.IsTrue(false); }
            catch (InvalidOperationException) { }

            try { section.EmptyContent = new TestVM("e1"); Assert.IsTrue(false); }
            catch (InvalidOperationException) { }
        }

        // DetachListener 호출 후에는 가드가 해제되어 다시 변경 가능.
        private void DetachReleasesGuard_AllowsMutationAgain()
        {
            var section = new Section(new ObservableList<IViewModel>(), MakeOption());
            section.AttachListener();
            section.DetachListener();

            var h1 = new TestVM("h1");
            section.Header = h1;
            Assert.AreEqual(h1, section.Header);
        }

        // 멱등성: AttachListener를 두 번 호출해도 OnContentChanged가 한 번만 발행되어야 한다.
        private void AttachIsIdempotent_NoDoubleSubscribe()
        {
            var content = new ObservableList<IViewModel>();
            var section = new Section(content, MakeOption());

            var fired = 0;
            section.OnContentChanged += _ => fired++;

            section.AttachListener();
            section.AttachListener();  // 재호출 — 이중 구독되어선 안 됨

            content.Add(new TestVM("A"));
            Assert.AreEqual(1, fired, "OnContentChanged가 이중 구독되어 두 번 발행되었습니다.");
        }

        // 부착되지 않은 상태의 DetachListener 호출은 no-op이며 isAttached 가드를 잘못 해제하지 않는다.
        private void DetachWithoutAttach_IsNoOp()
        {
            var section = new Section(new ObservableList<IViewModel>(), MakeOption());

            // 부착하지 않은 상태에서 Detach 호출
            section.DetachListener();

            // 그 후 Attach → 부착 후 mutation은 여전히 throw해야 한다 (가드가 정상 작동)
            section.AttachListener();
            try
            {
                section.Header = new TestVM("h1");
                Assert.IsTrue(false, "AttachListener 후 Header mutation은 throw되어야 합니다.");
            }
            catch (InvalidOperationException) { /* 예상 */ }
        }
    }
}
