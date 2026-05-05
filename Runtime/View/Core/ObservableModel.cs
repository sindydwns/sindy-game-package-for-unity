using R3;

namespace Sindy.View
{
    public interface IObservableModel<T> : IViewModel
    {
        Observable<T> Obs { get; }
    }

    public abstract class ObservableModel<T> : ViewModel, IObservableModel<T>
    {
        public abstract Observable<T> Obs { get; }
    }
}
