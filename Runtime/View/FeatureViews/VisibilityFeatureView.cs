using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// <see cref="VisibilityFeature"/>로 GameObject 활성 상태를 제어한다.
    /// target이 비어 있으면 자기 자신의 GameObject를 제어한다.
    /// 자기 자신을 끄더라도 모델 스트림 구독은 유지되므로 다시 켤 수 있다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Visibility Feature View")]
    public class VisibilityFeatureView : FeatureView<VisibilityFeature>
    {
        [Tooltip("비우면 자기 자신의 GameObject를 제어한다.")]
        [SerializeField] private GameObject target;

        protected override void Bind(VisibilityFeature feature, ICollection<IDisposable> disposables)
        {
            var obj = target != null ? target : gameObject;
            feature.Show.Subscribe(v => obj.SetActive(v)).AddTo(disposables);
        }
    }
}
