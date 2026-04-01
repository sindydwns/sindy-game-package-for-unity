using System;
using R3;
using Sindy.Common;
using Sindy.Reactive;

namespace Sindy.View.Model
{
    public class FormatNumberPropModel<T> : PropModel<string>, IViewModel
    {
        public ReactiveProperty<T> Source { get; private set; }
        public ReactiveProperty<Func<T, string>> Format { get; private set; }

        public ReactiveProperty<string> Text => Prop;

        public FormatNumberPropModel(T value = default, Func<T, string> format = null)
            : this(value.ToWrap(), format.ToWrap()) { }

        public FormatNumberPropModel(ObservableWrapper<T> value, ObservableWrapper<Func<T, string>> format = default)
        {
            Source = ToReactiveProperty(value);
            Format = ToReactiveProperty(format);
            Format.Value ??= DefaultFormatter();

            // 소스가 끊기면 이 모델도 같이 끊어지도록 설정
            Source.Subscribe(DoNothing, Dispose).AddTo(disposables);

            Observable.CombineLatest(Source, Format, (x, f) => f(x))
                .Subscribe(x => Text.Value = x, Dispose)
                .AddTo(disposables);
        }

        protected ReactiveProperty<PropType> ToReactiveProperty<PropType>(ObservableWrapper<PropType> source)
        {
            if (source.Observable == default)
            {
                var prop = new ReactiveProperty<PropType>(source.Value);
                disposables.Add(prop);
                return prop;
            }
            else if (source.Observable is ReactiveProperty<PropType> property)
            {
                return property;
            }
            else
            {
                var prop = new ReactiveProperty<PropType>(source.Value);
                source.Observable.Subscribe(x => prop.Value = x).AddTo(disposables);
                disposables.Add(prop);
                return prop;
            }
        }

        private static Func<T, string> DefaultFormatter()
        {
            var type = typeof(T);
            if (type == typeof(float) || type == typeof(double))
            {
                return v => $"{v:F2}";
            }
            else if (type == typeof(long) || type == typeof(ulong) || type == typeof(int)
                || type == typeof(uint) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(byte) || type == typeof(sbyte))
            {
                return v => string.Format("{0:n0}", v);
            }

            return v => v.ToString();
        }
    }
}
