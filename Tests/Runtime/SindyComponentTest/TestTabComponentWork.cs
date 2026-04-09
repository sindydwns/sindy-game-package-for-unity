using R3;
using Sindy.View;
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
        private PropModel<int> model;

        public TestTabComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new PropModel<int>(0);
            model.Subscribe(v => Debug.Log($"[Tab] selectedIndex = {v}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = 1;
            model.Value = 2;
            model.Value = 0;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
