using System;
using Sindy.Common;
using R3;
using Sindy.Reactive;

namespace Sindy.View
{
    public class PropModel<T> : ObservableModel<T>
    {
        public ReactiveProperty<T> Prop { get; private set; } = new();
        public override Observable<T> Obs => Prop;
        public T Value
        {
            get => Prop.Value;
            set => Prop.Value = value;
        }

        public PropModel() : base() { }
        public PropModel(T value) : this()
        {
            Prop.Value = value;
        }
        public PropModel(ReactiveProperty<T> property) : this()
        {
            Prop.Value = property.Value;
            property.Subscribe(Prop).AddTo(disposables);
        }

        public override void Dispose()
        {
            base.Dispose();
            Prop.Dispose();
        }

        public IDisposable Subscribe(Action<T> onNext) => Prop.Subscribe(onNext);
    }
}
