using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// ToggleComponent — 양방향 바인딩 테스트
    /// - 모델 변경 → UI 반영 (SetIsOnWithoutNotify)
    /// - UI 클릭 → 모델 변경 (씬에서 직접 Toggle 클릭으로 확인)
    /// </summary>
    class TestToggleComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private BoolPropModel model;

        public TestToggleComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new BoolPropModel(false);
            model.Show.Subscribe(v => Debug.Log($"[Toggle] isOn = {v}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = true;
            model.Value = false;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
