using R3;
using UnityEngine;

namespace Sindy.View.Model
{
    public class TimerModel : ViewModel
    {
        public ReactiveProperty<float> Remaining { get; } = new();
        public ReadOnlyReactiveProperty<bool> IsFinished { get; }

        public TimerModel(float duration)
        {
            Remaining.Value = duration;
            IsFinished = Remaining.Select(t => t <= 0f).ToReadOnlyReactiveProperty();
            disposables.Add(IsFinished);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (Remaining.Value > 0f)
                        Remaining.Value = Mathf.Max(0f, Remaining.Value - Time.deltaTime);
                })
                .AddTo(disposables);
        }

        public void Reset(float duration)
        {
            Remaining.Value = duration;
        }

        public override void Dispose()
        {
            base.Dispose();
            Remaining.Dispose();
        }
    }
}
