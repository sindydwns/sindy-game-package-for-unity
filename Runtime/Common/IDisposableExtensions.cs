using System;
using System.Collections.Generic;

namespace Sindy.Common
{
    public static class IDisposableExtensions
    {
        public static void AddTo(this IDisposable disposable, List<IDisposable> disposables)
        {
            if (disposable == null || disposables == null)
            {
                return;
            }

            lock (disposables)
            {
                disposables.Add(disposable);
            }
        }
    }
}
