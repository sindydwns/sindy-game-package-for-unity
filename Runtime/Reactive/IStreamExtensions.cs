using System;
using Sindy.Common;

namespace Sindy.Reactive
{
    public static class IStreamExtensions
    {
        public static IStream<U> Select<T, U>(this IStream<T> source, Func<T, U> selector) => new SelectStream<T, U>(source, selector);
        private class SelectStream<T, U> : IStream<U>
        {
            private readonly IStream<T> _source;
            private readonly Func<T, U> _selector;

            public SelectStream(IStream<T> source, Func<T, U> selector)
            {
                _source = source;
                _selector = selector;
            }

            public IDisposable Subscribe(Action<U> onNext)
            {
                return _source.Subscribe(x => onNext(_selector(x)));
            }
        }

        public static IStream<T> Where<T>(this IStream<T> source, Func<T, bool> predicate) => new WhereStream<T>(source, predicate);
        private class WhereStream<T> : IStream<T>
        {
            private readonly IStream<T> _source;
            private readonly Func<T, bool> _predicate;

            public WhereStream(IStream<T> source, Func<T, bool> predicate)
            {
                _source = source;
                _predicate = predicate;
            }

            public IDisposable Subscribe(Action<T> onNext)
            {
                return _source.Subscribe(x =>
                {
                    if (_predicate(x))
                    {
                        onNext(x);
                    }
                });
            }
        }

        public static IStream<T> Switch<T>(this IStream<IStream<T>> source) => new SwitchStream<T>(source);
        private class SwitchStream<T> : IStream<T>
        {
            private readonly IStream<IStream<T>> _source;

            public SwitchStream(IStream<IStream<T>> source)
            {
                _source = source;
            }

            public IDisposable Subscribe(Action<T> onNext)
            {
                IDisposable currentInnerSubscription = null;
                var lockObject = new object();

                var outerSubscription = _source.Subscribe(innerStream =>
                {
                    lock (lockObject)
                    {
                        currentInnerSubscription?.Dispose();
                        if (innerStream != null)
                        {
                            currentInnerSubscription = innerStream.Subscribe(onNext);
                        }
                        else
                        {
                            currentInnerSubscription = null;
                        }
                    }
                });

                return new AnonymousDisposable(() =>
                {
                    outerSubscription.Dispose();
                    lock (lockObject)
                    {
                        currentInnerSubscription?.Dispose();
                    }
                });
            }
        }
    }
}
