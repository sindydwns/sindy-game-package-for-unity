using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    class TestButtonComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private ButtonModel model;

        public TestButtonComponentWork(SindyComponent component) : base()
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new ButtonModel();
            model.With(new InteractableFeature(true));

            int clickCount = 0;
            model.Subj.Subscribe(_ => clickCount++).AddTo(disposables);

            component.SetModel(model);
            Assert.IsTrue(component.IsInitialized);

            // interactable feature 동작 확인
            var interactable = model.Feature<InteractableFeature>();
            Assert.IsNotNull(interactable);
            Assert.AreEqual(true, interactable.Interactable.Value);

            interactable.Interactable.Value = false;
            Assert.AreEqual(false, interactable.Interactable.Value);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
