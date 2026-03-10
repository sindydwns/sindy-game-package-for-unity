using Sindy.Reactive;

namespace Sindy.View
{
    public interface IStreamModel { }

    public interface IStreamModel<T> : IViewModel, IStream<T>, IStreamModel
    {
        Stream<T> Stream { get; }
    }
}
