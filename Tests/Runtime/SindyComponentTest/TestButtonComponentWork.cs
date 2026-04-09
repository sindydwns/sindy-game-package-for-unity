using R3;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    class TestButtonComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private SubjModel<Unit> model;

        public TestButtonComponentWork(SindyComponent component) : base()
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new SubjModel<Unit>();
            model.Subj.Subscribe(x => Debug.Log("Button clicked")).AddTo(disposables);

            component.SetModel(model);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
