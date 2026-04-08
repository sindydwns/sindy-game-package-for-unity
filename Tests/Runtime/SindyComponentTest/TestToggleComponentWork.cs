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

        public TestToggleComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var toggle = new BoolPropModel(false);

            toggle.Show
                .Subscribe(v => Debug.Log($"[Toggle] isOn = {v}"))
                .AddTo(disposables);

            component.SetModel(toggle);

            // 모델 → UI 방향 확인
            toggle.Value = true;
            toggle.Value = false;

            // UI → 모델 방향은 씬에서 Toggle을 직접 클릭하여 콘솔 로그로 확인
        }
    }
}
