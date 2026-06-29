using System;
using Sindy.Common;
using R3;
using Sindy.Reactive;

namespace Sindy.View
{
    /// <summary>
    /// 현재 값을 보유하는 상태 모델. <see cref="ReactiveProperty{T}"/>를 감싸 변경을 방출하며,
    /// 구독 즉시 현재 값이 전달된다. 일회성 이벤트에는 <see cref="SubjModel{T}"/>를 쓴다.
    /// </summary>
    public class PropModel<T> : ObservableModel<T>
    {
        /// <summary>내부 ReactiveProperty. 값 보유와 방출의 실체.</summary>
        public ReactiveProperty<T> Prop { get; private set; } = new();

        /// <inheritdoc/>
        public override Observable<T> Obs => Prop;

        /// <summary>현재 값. 설정 시 변경이 구독자에게 방출된다.</summary>
        public T Value
        {
            get => Prop.Value;
            set => Prop.Value = value;
        }

        /// <summary>기본값으로 초기화한다.</summary>
        public PropModel() : base() { }

        /// <summary>초기 값으로 생성한다.</summary>
        public PropModel(T value) : this()
        {
            Prop.Value = value;
        }

        /// <summary>외부 ReactiveProperty의 값을 받아 이후 변경을 구독·미러링한다.</summary>
        public PropModel(ReactiveProperty<T> property) : this()
        {
            Prop.Value = property.Value;
            property.Subscribe(Prop).AddTo(disposables);
        }

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            Prop.Dispose();
        }

        /// <summary>값 변경을 구독한다(구독 즉시 현재 값 1회 전달).</summary>
        public IDisposable Subscribe(Action<T> onNext) => Prop.Subscribe(onNext);
    }
}
