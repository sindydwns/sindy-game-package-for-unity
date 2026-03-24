using System;
using R3;

public static class SubjectExtensions
{
    public static IDisposable Subscribe<T>(this Observable<T> stream, ReactiveProperty<T> state)
    {
        return stream.Subscribe(value => state.Value = value);
    }
}
