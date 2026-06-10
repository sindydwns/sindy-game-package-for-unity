using System;
using System.Collections.Generic;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary><see cref="LayoutFeature"/>를 RectTransform에 적용한다 (바인딩 시 1회).</summary>
    [AddComponentMenu("Sindy/Feature Views/Layout Feature View")]
    public class LayoutFeatureView : FeatureView<LayoutFeature>
    {
        protected override void Bind(LayoutFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Apply(transform as RectTransform);
        }
    }
}
