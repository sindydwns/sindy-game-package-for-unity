using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// TabComponent — 양방향 바인딩 테스트
    /// - 모델 인덱스 변경 → 해당 Toggle만 On 상태로 반영
    /// - UI Toggle 클릭 → 모델 인덱스 변경 (씬에서 직접 확인)
    /// Inspector에서 TabComponent의 tabs 리스트에 Toggle들을 등록해야 함
    /// </summary>
    class TestTabComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestTabComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var tab = new IntPropModel(0);

            tab.Number
                .Subscribe(v => Debug.Log($"[Tab] selectedIndex = {v}"))
                .AddTo(disposables);

            component.SetModel(tab);

            // 모델 → UI 방향 확인
            tab.Value = 1;
            tab.Value = 2;
            tab.Value = 0;

            // UI → 모델 방향은 씬에서 Toggle을 직접 클릭하여 콘솔 로그로 확인
        }
    }
}
