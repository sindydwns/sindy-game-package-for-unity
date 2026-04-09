using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// FormatLabelComponent — FormatNumberPropModel<int>의 포맷된 문자열이 TMP_Text에 반영되는지 확인
    /// </summary>
    class TestFormatLabelComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private FormatNumberPropModel<int> model;

        public TestFormatLabelComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new FormatNumberPropModel<int>(0);
            model.Text.Subscribe(v => Debug.Log($"[FormatLabel] formatted = \"{v}\"")).AddTo(disposables);

            component.SetModel(model);

            model.Source.Value = 1000;
            model.Source.Value = 9999999;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
