using System;
using R3;

namespace Sindy.View
{
    public class SubjModel<T> : ObservableModel<T>
    {
        public Subject<T> Subj { get; } = new();
        public override Observable<T> Obs => Subj;

        public override void Dispose()
        {
            base.Dispose();
            Subj.Dispose();
        }

        public void Subscribe(Action<T> onNext) => Subj.Subscribe(onNext);
        public void OnNext(T value) => Subj.OnNext(value);
    }
}
