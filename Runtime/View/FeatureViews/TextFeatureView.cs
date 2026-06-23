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

        // 프리팹/Variant가 지정한 기본 폰트 크기. 모델이 FontSize<=0이거나 해제될 때 복원한다(풀링 안전).
        private float originalFontSize;
        private bool captured;

        protected override void Awake()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                originalFontSize = label.fontSize;
                captured = true;
            }
            base.Awake();
        }

        protected virtual void Reset()
        {
            label = GetComponentInChildren<TMP_Text>(true);
        }

        protected override void Bind(TextFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Text.Subscribe(v => label.text = v).AddTo(disposables);
            feature.FontSize.Subscribe(v =>
            {
                if (label == null) return;
                label.fontSize = v > 0f ? v : originalFontSize;
            }).AddTo(disposables);
        }

        protected override void Clear()
        {
            if (captured && label != null) label.fontSize = originalFontSize;
        }
    }
}
