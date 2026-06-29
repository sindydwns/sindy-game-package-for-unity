using R3;

namespace Sindy.View
{
    /// <summary>관찰 가능한 단일 값 스트림(<see cref="Obs"/>)을 노출하는 모델 인터페이스.</summary>
    public interface IObservableModel<T> : IViewModel
    {
        /// <summary>구독 가능한 값 스트림.</summary>
        Observable<T> Obs { get; }
    }

    /// <summary>
    /// 값 스트림 하나를 표면화하는 모델 베이스. <see cref="PropModel{T}"/>(상태)와
    /// <see cref="SubjModel{T}"/>(이벤트)의 공통 부모다.
    /// </summary>
    public abstract class ObservableModel<T> : ViewModel, IObservableModel<T>
    {
        /// <summary>구독 가능한 값 스트림.</summary>
        public abstract Observable<T> Obs { get; }
    }
}
