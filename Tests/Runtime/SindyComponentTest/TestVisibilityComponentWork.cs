using R3;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// VisibilityComponent — PropModel<bool> 변경 시 GameObject 활성/비활성에 반영되는지 확인
    /// </summary>
    class TestVisibilityComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private PropModel<bool> model;

        public TestVisibilityComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new PropModel<bool>(true);
            model.Subscribe(v => Debug.Log($"[Visibility] visible = {v}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = false;
            model.Value = true;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
