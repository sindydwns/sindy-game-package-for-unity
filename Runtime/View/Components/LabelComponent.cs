using System;
using R3;
using Sindy.Reactive;
using TMPro;
using UnityEngine;

namespace Sindy.View.Components
{
    public class LabelComponent : SindyComponent<PropModel<string>>
    {
        [SerializeField] private TMP_Text label;

        protected override void Init(PropModel<string> model)
        {
            model.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }

    public class LabelModel : PropModel<string>
    {
        public LabelModel() { }
        public LabelModel(string text) : base(text) { }
    }

    /// <summary>
    /// 카운트다운 타이머 모델. PropModel<string>을 상속하므로 LabelComponent에 직접 전달할 수 있습니다.
    /// Prop에는 format에 따라 포맷된 남은 시간 문자열이 자동으로 업데이트됩니다.
    /// </summary>
    public class TimerModel : PropModel<string>
    {
        public ReactiveProperty<float> Remaining { get; } = new();
        public ReadOnlyReactiveProperty<bool> IsFinished { get; }
        public bool IsPaused { get; private set; }

        /// <param name="duration">초 단위 초기 시간</param>
        /// <param name="format">TimeSpan 복합 서식 문자열. 예: @"mm\:ss", @"hh\:mm\:ss"</param>
        public TimerModel(float duration, string format = @"mm\:ss")
        {
            Remaining.Value = duration;
            IsFinished = Remaining.Select(t => t <= 0f).ToReadOnlyReactiveProperty();
            disposables.Add(IsFinished);

            Remaining
                .Subscribe(v => Prop.Value = TimeSpan.FromSeconds(v).ToString(format))
                .AddTo(disposables);

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    if (!IsPaused && Remaining.Value > 0f)
                        Remaining.Value = MathF.Max(0f, Remaining.Value - Time.deltaTime);
                })
                .AddTo(disposables);
        }

        public void Reset(float duration) { Remaining.Value = duration; IsPaused = false; }
        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;

        public override void Dispose()
        {
            base.Dispose();
            Remaining.Dispose();
        }
    }

    /// <summary>
    /// 수치 값과 서식 함수를 CombineLatest로 결합해 string으로 변환하는 모델.
    /// PropModel<string>을 상속하므로 LabelComponent에 직접 전달할 수 있습니다.
    /// </summary>
    public class FormatNumberPropModel<T> : PropModel<string>
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
                return v => $"{v:F2}";
            if (type == typeof(long) || type == typeof(ulong) || type == typeof(int)
                || type == typeof(uint) || type == typeof(short) || type == typeof(ushort)
                || type == typeof(byte) || type == typeof(sbyte))
                return v => string.Format("{0:n0}", v);
            return v => v.ToString();
        }
    }
}
