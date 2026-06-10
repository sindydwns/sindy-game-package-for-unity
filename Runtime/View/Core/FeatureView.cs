using System;
using System.Collections.Generic;
using R3;
using Sindy.Common;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// FeatureView 비제네릭 마커 인터페이스.
    /// SindyComponent(허브)가 Editor/Dev 빌드에서 모델의 Feature 목록과
    /// 부착된 FeatureView 목록을 대조 검증할 때 사용한다.
    /// </summary>
    public interface IFeatureView
    {
        Type FeatureType { get; }
    }

    /// <summary>
    /// ModelFeature(모델) ↔ FeatureView(뷰) 1:1 대칭의 뷰 측 베이스.
    ///
    /// - <see cref="SindyComponent"/>(허브)의 <c>Model</c> 스트림을 스스로 구독한다(역방향 참조).
    ///   허브는 FeatureView의 존재를 모른다(릴리스 빌드 기준 순수 pub/sub).
    /// - 모델 교체 시 dispose-then-bind가 구조적으로 강제되므로
    ///   구현자는 <see cref="Bind"/>/<see cref="Clear"/>만 작성하면 된다.
    /// - ReactiveProperty의 "구독 즉시 현재 값 방출" 의미론 덕분에
    ///   비활성 Bind, 늦은 Awake, 런타임 AddComponent 타이밍이 모두 자연스럽게 처리된다.
    /// </summary>
    [RequireComponent(typeof(SindyComponent))]
    public abstract class FeatureView<TFeature> : MonoBehaviour, IFeatureView where TFeature : ModelFeature
    {
        protected readonly List<IDisposable> disposables = new();
        private IDisposable modelSubscription;

        public Type FeatureType => typeof(TFeature);

        protected virtual void Awake()
        {
            modelSubscription = GetComponent<SindyComponent>().Model.Subscribe(OnModelChanged);
        }

        private void OnModelChanged(IViewModel model)
        {
            // 항상 해제 먼저 — 구현자가 틀릴 수 없도록 베이스에서 고정한다.
            disposables.DisposeAllClear();

            var feature = model?.Feature<TFeature>();
            if (feature != null)
            {
                Bind(feature, disposables);
            }
            else
            {
                Clear();
            }
        }

        /// <summary>
        /// Feature가 도착했을 때 호출된다. 모든 구독은 반드시 <paramref name="disposables"/>에 추가할 것.
        /// 다음 모델 교체/해제 시 베이스가 일괄 해제한다.
        /// </summary>
        protected abstract void Bind(TFeature feature, ICollection<IDisposable> disposables);

        /// <summary>모델이 null이 되거나 Feature가 없는 모델로 교체됐을 때의 UI 초기화 훅.</summary>
        protected virtual void Clear() { }

        protected virtual void OnDestroy()
        {
            modelSubscription?.Dispose();
            modelSubscription = null;
            disposables.DisposeAllClear();
        }
    }
}
