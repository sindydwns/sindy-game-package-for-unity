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

        private void AttachAndRetrieve()
        {
            var button = new SubjModel<R3.Unit>();
            button.With(new InteractableFeature());

            var feature = button.Feature<InteractableFeature>();

            Assert.IsNotNull(feature);
            Assert.IsTrue(feature.Interactable.Value);

            button.Dispose();
        }

        private void FeatureReturnsNullWhenNotAttached()
        {
            var model = new PropModel<string>("test");

            var feature = model.Feature<InteractableFeature>();

            Assert.IsNull(feature);

            model.Dispose();
        }

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

        private void FeatureDisposedWithModel()
        {
            var button = new SubjModel<R3.Unit>();
            var interactable = new InteractableFeature();
            button.With(interactable);

            button.Dispose();

            Assert.IsTrue(interactable.IsDisposed);
        }

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
