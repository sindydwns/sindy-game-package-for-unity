using System;
using System.Collections.Generic;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// <see cref="AnchorFeature"/>를 RectTransform에 적용한다 (바인딩 시 1회).
    /// 모델 해제 시에는 되돌리지 않는다 — 앵커에는 "의미 있는 기본값"이 없고, 다음 Apply가 전체를 다시 지정한다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Anchor Feature View")]
    public class AnchorFeatureView : FeatureView<AnchorFeature>
    {
        protected override void Bind(AnchorFeature feature, ICollection<IDisposable> disposables)
        {
            var rect = transform as RectTransform;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AnchorFeature.IsOverriddenByParentLayout(rect))
                Debug.LogWarning(
                    $"AnchorFeatureView('{name}'): 부모에 LayoutGroup이 있어 Anchor/Inset 지정이 덮어써집니다. " +
                    $"LayoutGroup 자식의 크기는 Size/Flexible로 지정하세요.", this);
#endif
            feature.Apply(rect);
        }
    }
}
