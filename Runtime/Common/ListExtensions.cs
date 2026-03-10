using System;
using System.Collections.Generic;

namespace Sindy.Common
{
    public static class ListExtensions
    {
        public static void DisposeAll(this List<IDisposable> disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables.Clear();
        }
    }
}
