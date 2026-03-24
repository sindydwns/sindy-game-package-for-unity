using R3;

namespace Sindy.View
{
    public interface IStreamModel { }

    public interface IStreamModel<T> : IViewModel, IStreamModel
    {
        ReactiveProperty<T> Stream { get; }
    }
}
