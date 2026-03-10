using System;

namespace Sindy.Common
{
    internal class AnonymousDisposable : IDisposable
    {
        private Action _dispose;

        public AnonymousDisposable(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
