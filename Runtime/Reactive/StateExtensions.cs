using System;

namespace Sindy.Reactive
{
    public static class StateExtensions
    {
        public static IDisposable Subscribe<T>(this IStream<T> stream, State<T> state)
        {
            return stream.Subscribe(value => state.Value = value);
        }
    }
}
