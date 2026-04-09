using System;
using System.Collections.Generic;

namespace Sindy.Common
{
    public static class ListExtensions
    {
        public static void DisposeAll(this List<IDisposable> disposables)
        {
            for (var i = disposables.Count - 1; i >= 0; i--)
            {
                disposables[i].Dispose();
            }
            disposables.Clear();
        }
    }
}
