using System;
using R3;

public static class ObservableExtensions
{
    public static IDisposable Subscribe<T>(this Observable<T> stream, ReactiveProperty<T> state)
    {
        return stream.Subscribe(value => state.Value = value);
    }
}
