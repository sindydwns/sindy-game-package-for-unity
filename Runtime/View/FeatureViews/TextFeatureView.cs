using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using TMPro;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>TMP_Text에 <see cref="TextFeature"/>를 출력한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Text Feature View")]
    public class TextFeatureView : FeatureView<TextFeature>
    {
        [SerializeField] private TMP_Text label;

        protected virtual void Reset()
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        protected override void Bind(TextFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Text.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }
}
