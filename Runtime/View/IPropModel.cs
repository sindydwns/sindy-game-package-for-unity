using Sindy.Reactive;

namespace Sindy.View
{
    public interface IPropModel { }

    public interface IPropModel<T> : IViewModel, IStream<T>, IPropModel
    {
        State<T> State { get; }
        T Value
        {
            get => State.Value;
            set => State.Value = value;
        }
    }
}
