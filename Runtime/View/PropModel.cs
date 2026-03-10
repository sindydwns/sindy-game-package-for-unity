using System;
using Sindy.Common;
using Sindy.Reactive;

namespace Sindy.View
{
    public class PropModel<T> : ViewModel, IPropModel<T>
    {
        public State<T> State { get; private set; } = new();
        public T Value
        {
            get => State.Value;
            set => State.Value = value;
        }
        public PropModel() : base() { }
        public PropModel(T value) : this()
        {
            State.Value = value;
        }
        public PropModel(State<T> property) : this()
        {
            State.Value = property.Value;
            property.Subscribe(State).AddTo(disposables);
        }

        public override void Dispose()
        {
            base.Dispose();
            State.Dispose();
        }

        public IDisposable Subscribe(Action<T> onNext) => State.Subscribe(onNext);
    }
}
