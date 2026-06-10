using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary><see cref="HighlightFeature"/>로 하이라이트 오브젝트의 표시를 제어한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Highlight Feature View")]
    public class HighlightFeatureView : FeatureView<HighlightFeature>
    {
        [SerializeField] private GameObject highlightTarget;

        protected override void Bind(HighlightFeature feature, ICollection<IDisposable> disposables)
        {
            if (highlightTarget == null) return;
            feature.Highlight.Subscribe(v => highlightTarget.SetActive(v)).AddTo(disposables);
        }

        protected override void Clear()
        {
            if (highlightTarget != null) highlightTarget.SetActive(false);
        }
    }
}
