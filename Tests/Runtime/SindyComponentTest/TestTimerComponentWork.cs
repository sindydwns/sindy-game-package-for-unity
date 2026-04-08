using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// TimerLabelComponent — TimerModel 카운트다운이 TMP_Text에 반영되는지 확인
    /// Start() 직후 자동으로 타이머가 시작됨
    /// </summary>
    class TestTimerComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestTimerComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var timer = new TimerModel(10f);

            timer.Remaining
                .Subscribe(v => Debug.Log($"[Timer] remaining = {v:F1}s"))
                .AddTo(disposables);

            timer.IsFinished
                .Where(finished => finished)
                .Subscribe(_ => Debug.Log("[Timer] finished!"))
                .AddTo(disposables);

            component.SetModel(timer);

            // Pause / Resume 확인
            // timer.Pause();
            // timer.Resume();

            // 재시작 확인
            // timer.Reset(5f);
        }
    }
}
