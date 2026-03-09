using System;

namespace Sindy.Core
{
    public interface IStream<T>
    {
        IDisposable Subscribe(Action<T> onNext);
    }
}
