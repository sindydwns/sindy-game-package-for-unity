using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.FeatureViews
{
    /// <summary>Graphic.color에 <see cref="ColorFeature"/>를 출력한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Color Feature View")]
    public class ColorFeatureView : FeatureView<ColorFeature>
    {
        [SerializeField] private Graphic target;

        protected virtual void Reset()
        {
            target = GetComponent<Graphic>();
        }

        protected override void Bind(ColorFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Color.Subscribe(v => target.color = v).AddTo(disposables);
        }
    }
}
