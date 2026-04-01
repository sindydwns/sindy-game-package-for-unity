using System;
using R3;

public static class ObservableExtensions
{
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
}
