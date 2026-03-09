using System;

namespace Sindy.Core
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
            if (_dispose != null)
            {
                _dispose.Invoke();
                _dispose = null;
            }
        }
    }
}
