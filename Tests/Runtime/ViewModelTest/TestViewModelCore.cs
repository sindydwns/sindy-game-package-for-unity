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

        // PropModel의 Value 설정과 Subscribe 구독이 올바르게 동작하는지 확인
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

        // SubjModel의 Subject에 OnNext로 값을 발행하면 구독자에게 전달되는지 확인
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

        // ViewModel에 자식 모델을 추가/조회하고 부모 Dispose 시 자식도 Dispose되는지 확인
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

        // ViewModel에 Feature를 부착/조회하고 부모 Dispose 시 Feature도 Dispose되는지 확인
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

        // ViewModel Dispose 후 IsDisposed가 true이고 이중 Dispose 시 예외가 발생하지 않는지 확인
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
