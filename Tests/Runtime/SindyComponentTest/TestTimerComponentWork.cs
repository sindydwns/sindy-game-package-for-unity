using R3;
using Sindy.View;
using Sindy.View.Components;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// LabelComponent + TimerModel — 카운트다운이 TMP_Text에 반영되는지 확인
    /// TimerModel은 PropModel<string>을 상속하므로 LabelComponent에 직접 전달 가능합니다.
    /// TimerModel은 생성 즉시 EveryUpdate 구독을 시작하므로 반드시 명시적으로 Dispose해야 합니다.
    /// </summary>
    class TestTimerComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private TimerModel model;

        public TestTimerComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new TimerModel(10f);

            model.Remaining
                .Subscribe(v => Debug.Log($"[Timer] remaining = {v:F1}s"))
                .AddTo(disposables);

            model.IsFinished
                .Where(finished => finished)
                .Subscribe(_ => Debug.Log("[Timer] finished!"))
                .AddTo(disposables);

            component.SetModel(model);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
