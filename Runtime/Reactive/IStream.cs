using System;

namespace Sindy.Reactive
{
    public interface IStream<T>
    {
        IDisposable Subscribe(Action<T> onNext);
    }
}
