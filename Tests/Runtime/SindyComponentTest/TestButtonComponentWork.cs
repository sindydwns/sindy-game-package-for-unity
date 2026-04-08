using R3;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    class TestButtonComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestButtonComponentWork(SindyComponent component) : base()
        {
            this.component = component;
        }

        public override void Run()
        {
            var onClick = new SubjModel<Unit>();
            onClick.Subj.Subscribe(x => Debug.Log("Button clicked")).AddTo(disposables);

            component.SetModel(onClick);
        }
    }
}
