using R3;

namespace Sindy.View
{
    public interface IPropModel { }

    public interface IPropModel<T> : IViewModel, IPropModel
    {
        ReactiveProperty<T> Prop { get; }
        T Value
        {
            get => Prop.Value;
            set => Prop.Value = value;
        }
    }
}
