using System;
using System.Collections.Generic;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// <see cref="LayoutFeature"/>를 RectTransform에 적용한다 (바인딩 시 1회).
    /// 모델 해제 시에는 레이아웃 영향을 비활성화해 풀링 재사용 시 잔존을 막는다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Layout Feature View")]
    public class LayoutFeatureView : FeatureView<LayoutFeature>
    {
        protected override void Bind(LayoutFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Apply(transform as RectTransform);
        }

        protected override void Clear()
        {
            LayoutFeature.Deactivate(transform as RectTransform);
        }
    }
}
