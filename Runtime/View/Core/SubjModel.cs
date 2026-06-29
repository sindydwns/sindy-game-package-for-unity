using System;
using R3;

namespace Sindy.View
{
    /// <summary>
    /// 발생 시점에만 값을 흘리는 이벤트 모델. <see cref="Subject{T}"/>를 감싸며 현재 값을
    /// 보유하지 않는다(클릭 등 일회성 신호용). 상태 값에는 <see cref="PropModel{T}"/>를 쓴다.
    /// </summary>
    public class SubjModel<T> : ObservableModel<T>
    {
        /// <summary>내부 Subject. 이벤트 방출의 실체.</summary>
        public Subject<T> Subj { get; } = new();

        /// <inheritdoc/>
        public override Observable<T> Obs => Subj;

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            Subj.Dispose();
        }

        /// <summary>이벤트를 구독한다.</summary>
        public void Subscribe(Action<T> onNext) => Subj.Subscribe(onNext);

        /// <summary>이벤트를 한 번 방출한다.</summary>
        public void OnNext(T value) => Subj.OnNext(value);
    }
}
