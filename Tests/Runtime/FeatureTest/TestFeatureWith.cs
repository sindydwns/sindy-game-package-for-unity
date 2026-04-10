using Sindy.View;
using Sindy.View.Features;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ViewModel.With / Feature API — 부착, 조회, 체이닝, Dispose 연쇄
    /// </summary>
    class TestFeatureWith : TestCase
    {
        public override void Run()
        {
            AttachAndRetrieve();
            FeatureReturnsNullWhenNotAttached();
            ChainingMultipleFeatures();
            FeatureDisposedWithModel();
            FeatureOverwritesSameType();
        }

        // Feature를 With()로 부착한 뒤 Feature<T>()로 조회할 수 있는지 확인
        private void AttachAndRetrieve()
        {
            var button = new SubjModel<R3.Unit>();
            button.With(new InteractableFeature());

            var feature = button.Feature<InteractableFeature>();

            Assert.IsNotNull(feature);
            Assert.IsTrue(feature.Interactable.Value);

            button.Dispose();
        }

        // Feature를 부착하지 않은 모델에서 Feature<T>() 호출 시 null을 반환하는지 확인
        private void FeatureReturnsNullWhenNotAttached()
        {
            var model = new PropModel<string>("test");

            var feature = model.Feature<InteractableFeature>();

            Assert.IsNull(feature);

            model.Dispose();
        }

        // With()를 체이닝하여 여러 Feature를 동시에 부착하고 각각 조회 가능한지 확인
        private void ChainingMultipleFeatures()
        {
            var button = new SubjModel<R3.Unit>();
            button
                .With(new InteractableFeature())
                .With(new VisibilityFeature())
                .With(new HighlightFeature());

            Assert.IsNotNull(button.Feature<InteractableFeature>());
            Assert.IsNotNull(button.Feature<VisibilityFeature>());
            Assert.IsNotNull(button.Feature<HighlightFeature>());

            button.Dispose();
        }

        // 모델 Dispose 시 부착된 Feature도 함께 Dispose되는지 확인
        private void FeatureDisposedWithModel()
        {
            var button = new SubjModel<R3.Unit>();
            var interactable = new InteractableFeature();
            button.With(interactable);

            button.Dispose();

            Assert.IsTrue(interactable.IsDisposed);
        }

        // 같은 타입의 Feature를 다시 부착하면 기존 것이 덮어씌워지는지 확인
        private void FeatureOverwritesSameType()
        {
            var model = new PropModel<string>("test");
            var first = new InteractableFeature(true);
            var second = new InteractableFeature(false);

            model.With(first);
            model.With(second);

            var retrieved = model.Feature<InteractableFeature>();
            Assert.AreEqual(false, retrieved.Interactable.Value);

            model.Dispose();
        }
    }
}
