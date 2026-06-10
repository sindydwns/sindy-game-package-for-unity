using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.FeatureViews
{
    /// <summary>Image에 <see cref="ImageFeature"/>의 스프라이트를 출력한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Image Feature View")]
    public class ImageFeatureView : FeatureView<ImageFeature>
    {
        [SerializeField] private Image image;

        protected virtual void Reset()
        {
            image = GetComponentInChildren<Image>(true);
        }

        protected override void Bind(ImageFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Sprite.Subscribe(v => image.sprite = v).AddTo(disposables);
        }
    }
}
