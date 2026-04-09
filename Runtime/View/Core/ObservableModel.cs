using R3;
using Sindy.Common;

namespace Sindy.View
{
    public interface IViewModel : IDisposeChain
    {
        public T GetChild<T>(string name) where T : IViewModel;
    }

    public interface IObservableModel<T> : IViewModel
    {
        Observable<T> Obs { get; }
    }

    public abstract class ObservableModel<T> : ViewModel, IObservableModel<T>
    {
        public abstract Observable<T> Obs { get; }
    }
}
