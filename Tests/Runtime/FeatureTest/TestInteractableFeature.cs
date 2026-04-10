using Sindy.View;
using Sindy.View.Features;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// InteractableFeature — 초기값, 토글, 구독, Dispose
    /// </summary>
    class TestInteractableFeature : TestCase
    {
        public override void Run()
        {
            DefaultIsTrue();
            CustomInitialValue();
            ToggleValue();
            SubscribeToChanges();
            DisposeCleansProp();
        }

        private void DefaultIsTrue()
        {
            var feature = new InteractableFeature();

            Assert.IsTrue(feature.Interactable.Value);

            feature.Dispose();
        }

        private void CustomInitialValue()
        {
            var feature = new InteractableFeature(false);

            Assert.IsFalse(feature.Interactable.Value);

            feature.Dispose();
        }

        private void ToggleValue()
        {
            var feature = new InteractableFeature(true);

            feature.Interactable.Value = false;
            Assert.IsFalse(feature.Interactable.Value);

            feature.Interactable.Value = true;
            Assert.IsTrue(feature.Interactable.Value);

            feature.Dispose();
        }

        private void SubscribeToChanges()
        {
            var feature = new InteractableFeature(true);
            bool lastValue = true;
            feature.Interactable.Subscribe(v => lastValue = v).AddTo(disposables);

            feature.Interactable.Value = false;

            Assert.IsFalse(lastValue);

            feature.Dispose();
        }

        private void DisposeCleansProp()
        {
            var feature = new InteractableFeature();

            feature.Dispose();

            Assert.IsTrue(feature.IsDisposed);
        }
    }
}
