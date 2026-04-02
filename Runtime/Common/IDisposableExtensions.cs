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

        public static void DisposeAll(this IEnumerable<IDisposable> disposables)
        {
            if (disposables == null)
            {
                return;
            }
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
        }

        public static void DisposeAllClear(this List<IDisposable> disposables)
        {
            if (disposables == null)
            {
                return;
            }
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }
            disposables.Clear();
        }
    }
}
