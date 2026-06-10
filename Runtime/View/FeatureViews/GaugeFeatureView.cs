using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.FeatureViews
{
    /// <summary>Image.fillAmount에 <see cref="GaugeFeature"/>의 비율(0~1)을 출력한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Gauge Feature View")]
    public class GaugeFeatureView : FeatureView<GaugeFeature>
    {
        [SerializeField] private Image fill;

        protected virtual void Reset()
        {
            fill = GetComponentInChildren<Image>(true);
        }

        protected override void Bind(GaugeFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Ratio.Subscribe(v => fill.fillAmount = Mathf.Clamp01(v)).AddTo(disposables);
        }
    }
}
