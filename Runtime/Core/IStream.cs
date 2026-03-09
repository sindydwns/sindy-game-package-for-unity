using System;

namespace Sindy.Foundation
{
    public interface IStream<T>
    {
        IDisposable Subscribe(Action<T> onNext);
    }
}
