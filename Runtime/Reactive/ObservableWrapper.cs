using R3;

namespace Sindy.Reactive
{
    public struct ObservableWrapper<T>
    {
        public Observable<T> Observable { get; private set; }
        public T Value { get; private set; }

        public ObservableWrapper(Observable<T> observable, T initialValue = default)
        {
            Observable = observable;
            Value = initialValue;
        }

        public ObservableWrapper(T initialValue)
        {
            Observable = null;
            Value = initialValue;
        }
    }

    public static class ObservableWrapperExtensions
    {
        public static ObservableWrapper<T> ToObservableWrap<T>(this Observable<T> observable, T initialValue = default)
        {
            return new ObservableWrapper<T>(observable, initialValue);
        }

        public static ObservableWrapper<T> ToWrap<T>(this T value)
        {
            return new ObservableWrapper<T>(value);
        }
    }
}
