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

        public TestColorComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var color = new ColorPropModel(Color.white);

            color.Color
                .Subscribe(v => Debug.Log($"[Color] color = {v}"))
                .AddTo(disposables);

            component.SetModel(color);

            color.Value = Color.red;
            color.Value = new Color(0.2f, 0.8f, 0.4f);
        }
    }
}
