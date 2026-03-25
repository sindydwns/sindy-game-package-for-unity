using R3;

namespace Sindy.View
{
    public class StreamModel<T> : ViewModel, IStreamModel<T>
    {
        public Subject<T> Stream { get; } = new();
    }
}
