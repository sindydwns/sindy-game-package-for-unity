using System;
using System.Threading;

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
            Interlocked.Exchange(ref _dispose, null)?.Invoke();
        }
    }
}
