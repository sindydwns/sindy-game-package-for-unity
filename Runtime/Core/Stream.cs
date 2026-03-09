using System;
using System.Collections.Generic;

namespace Sindy.Core
{
    public class Stream<T> : IStream<T>, IDisposable
    {
        private List<Action<T>> _subscribers = new List<Action<T>>();
        private bool _isDisposed = false;
        private readonly object _lock = new object();

        public IDisposable Subscribe(Action<T> onNext)
        {
            if (_isDisposed)
            {
                return new AnonymousDisposable(() => { });
            }

            lock (_lock)
            {
                _subscribers.Add(onNext);
            }

            return new AnonymousDisposable(() =>
            {
                lock (_lock)
                {
                    _subscribers.Remove(onNext);
                }
            });
        }

        public void OnNext(T value)
        {
            if (_isDisposed) return;

            Action<T>[] currentSubscribers;
            lock (_lock)
            {
                // To avoid modification during enumeration, we operate on a copy if multiple subscribers exist,
                // or just iterate. Array copy is safer if subscribers add/remove during OnNext.
                currentSubscribers = _subscribers.ToArray();
            }

            foreach (var subscriber in currentSubscribers)
            {
                if (!_isDisposed)
                {
                    subscriber(value);
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;

            lock (_lock)
            {
                _isDisposed = true;
                _subscribers.Clear();
            }
        }
    }
}
