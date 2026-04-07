using System;
using System.Linq;
using R3;

namespace Sindy.Reactive
{
    public static class ObservableExtensions
    {
        public static IDisposable Subscribe<T>(this Observable<T> source, Action onNext)
        {
            return source.Subscribe(_ => onNext.Invoke());
        }

        public static IDisposable Subscribe<T>(this Observable<T> source, ReactiveProperty<T> state)
        {
            return source.Subscribe(value => state.Value = value);
        }

        public static IDisposable Subscribe<T>(this Observable<T> source, ReactiveProperty<T> state, Action<Result> onComplete)
        {
            return source.Subscribe(value => state.Value = value, onComplete);
        }

        public static IDisposable Subscribe<T>(this Observable<T> source, ReactiveProperty<T> state, ReactiveProperty<Result> onComplete)
        {
            return source.Subscribe(value => state.Value = value, x => onComplete.Value = x);
        }

        public static Observable<T> ToSwitch<T>(this Observable<T> observable) where T : class
        {
            return observable
                .Select(x => observable)
                .Switch()
                .DistinctUntilChanged();
        }

        public static Observable<T2> ToSwitch<T1, T2>(this Observable<T1> observable, Func<T1, T2> func) where T1 : class
        {
            return observable
                .Select(x => observable)
                .Switch()
                .DistinctUntilChanged()
                .Select(x => x == null ? default : func(x));
        }
    }
}
