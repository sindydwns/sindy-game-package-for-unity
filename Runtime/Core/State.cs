using System;
using System.Collections.Generic;
using Sindy.Common;

namespace Sindy.Core
{
    public class State<T> : IStream<T>, IDisposable
    {
        private T _value;
        private List<Action<T>> _subscribers = new List<Action<T>>();
        private bool _isDisposed = false;
        private readonly object _lock = new object();
        private readonly IEqualityComparer<T> _equalityComparer;

        public State(T initialValue) : this(initialValue, EqualityComparer<T>.Default)
        {
        }

        public State(T initialValue, IEqualityComparer<T> equalityComparer)
        {
            _value = initialValue;
            _equalityComparer = equalityComparer ?? EqualityComparer<T>.Default;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (_isDisposed) return;

                if (!_equalityComparer.Equals(_value, value))
                {
                    _value = value;
                    OnNext(_value);
                }
            }
        }

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

            // Fire immediately for State (BehaviorSubject semantics)
            onNext(_value);

            return new AnonymousDisposable(() =>
            {
                lock (_lock)
                {
                    _subscribers.Remove(onNext);
                }
            });
        }

        private void OnNext(T value)
        {
            Action<T>[] currentSubscribers;
            lock (_lock)
            {
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
