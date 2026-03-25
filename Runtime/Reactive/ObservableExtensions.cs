using System;
using R3;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this Observable<T> source, ReactiveProperty<T> state)
    {
        return source.Subscribe(value => state.Value = value);
    }
}
