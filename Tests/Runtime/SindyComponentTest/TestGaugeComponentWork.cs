using R3;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// GaugeComponent — PropModel<float> 값 변경 시 fillAmount에 반영되는지 확인
    /// 값은 0~1 범위로 Clamp됨
    /// </summary>
    class TestGaugeComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private PropModel<float> model;

        public TestGaugeComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new PropModel<float>(1f);
            model.Subscribe(v => Debug.Log($"[Gauge] value = {v:F2}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = 0.75f;
            model.Value = 0.5f;
            model.Value = 0f;
            model.Value = 1.5f;  // Clamp01 → 1.0 으로 표시되어야 함
            model.Value = -0.1f; // Clamp01 → 0.0 으로 표시되어야 함
            model.Value = 0.5f;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
