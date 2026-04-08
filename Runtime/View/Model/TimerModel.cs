using R3;
using UnityEngine;

namespace Sindy.View.Model
{
    public class TimerModel : ViewModel
    {
        public ReactiveProperty<float> Remaining { get; } = new();
        public ReadOnlyReactiveProperty<bool> IsFinished { get; }
        public bool IsPaused { get; private set; }

        public TimerModel(float duration)
        {
            Remaining.Value = duration;
            IsFinished = Remaining.Select(t => t <= 0f).ToReadOnlyReactiveProperty();
            disposables.Add(IsFinished);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (!IsPaused && Remaining.Value > 0f)
                        Remaining.Value = Mathf.Max(0f, Remaining.Value - Time.deltaTime);
                })
                .AddTo(disposables);
        }

        public void Reset(float duration)
        {
            Remaining.Value = duration;
            IsPaused = false;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public override void Dispose()
        {
            base.Dispose();
            Remaining.Dispose();
        }
    }
}
