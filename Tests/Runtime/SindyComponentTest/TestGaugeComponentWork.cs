using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// GaugeComponent — FloatPropModel.Number 변경 시 Slider/Image fillAmount에 반영되는지 확인
    /// 값은 0~1 범위로 Clamp됨
    /// </summary>
    class TestGaugeComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestGaugeComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var gauge = new FloatPropModel(1f);

            gauge.Number
                .Subscribe(v => Debug.Log($"[Gauge] value = {v:F2}"))
                .AddTo(disposables);

            component.SetModel(gauge);

            gauge.Value = 0.75f;
            gauge.Value = 0.5f;
            gauge.Value = 0f;
            gauge.Value = 1.5f;  // Clamp01 → 1.0 으로 표시되어야 함
            gauge.Value = -0.1f; // Clamp01 → 0.0 으로 표시되어야 함
        }
    }
}
