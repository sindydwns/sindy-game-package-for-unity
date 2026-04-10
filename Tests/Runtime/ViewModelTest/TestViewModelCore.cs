using R3;
using Sindy.View;
using Sindy.View.Features;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    class TestViewModelCore : TestCase
    {
        public override void Run()
        {
            TestPropModelValueBinding();
            TestSubjModelEvent();
            TestViewModelChildHierarchy();
            TestViewModelFeature();
            TestViewModelDispose();
        }

        private void TestPropModelValueBinding()
        {
            var model = new PropModel<int>(10);
            Assert.AreEqual(10, model.Value);

            int received = 0;
            model.Subscribe(v => received = v).AddTo(disposables);
            model.Value = 42;
            Assert.AreEqual(42, received);

            model.Dispose();
            Assert.IsTrue(model.IsDisposed);
        }

        private void TestSubjModelEvent()
        {
            var model = new SubjModel<string>();
            string received = null;
            model.Subj.Subscribe(v => received = v).AddTo(disposables);

            model.OnNext("hello");
            Assert.AreEqual("hello", received);

            model.Dispose();
            Assert.IsTrue(model.IsDisposed);
        }

        private void TestViewModelChildHierarchy()
        {
            var parent = new ViewModel();
            var child = new PropModel<int>(5);
            parent.AddChild("score", child);

            var found = parent.GetChild<PropModel<int>>("score");
            Assert.AreEqual(5, found.Value);

            // nested path
            var nested = new PropModel<string>("deep");
            parent.AddChild("ui.label", nested);
            var foundNested = parent.GetChild<PropModel<string>>("ui.label");
            Assert.AreEqual("deep", foundNested.Value);

            // disposeWithParent: child disposed when parent disposes
            parent.Dispose();
            Assert.IsTrue(child.IsDisposed);
            Assert.IsTrue(nested.IsDisposed);
        }

        private void TestViewModelFeature()
        {
            var vm = new ViewModel();
            var visibility = new VisibilityFeature(true);
            vm.With(visibility);

            var found = vm.Feature<VisibilityFeature>();
            Assert.IsNotNull(found);
            Assert.AreEqual(true, found.Show.Value);

            found.Show.Value = false;
            Assert.AreEqual(false, found.Show.Value);

            // feature disposed with parent
            vm.Dispose();
            Assert.IsTrue(visibility.IsDisposed);
        }

        private void TestViewModelDispose()
        {
            var vm = new ViewModel();
            Assert.IsFalse(vm.IsDisposed);
            vm.Dispose();
            Assert.IsTrue(vm.IsDisposed);

            // double dispose should not throw
            vm.Dispose();
            Assert.IsTrue(vm.IsDisposed);
        }
    }
}
