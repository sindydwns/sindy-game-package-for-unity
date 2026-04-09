using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// ColorComponent — ColorPropModel.Color 변경 시 Graphic 색상에 반영되는지 확인
    /// </summary>
    class TestColorComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private ColorPropModel model;

        public TestColorComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new ColorPropModel(Color.white);
            model.Color.Subscribe(v => Debug.Log($"[Color] color = {v}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = Color.red;
            model.Value = new Color(0.2f, 0.8f, 0.4f);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
