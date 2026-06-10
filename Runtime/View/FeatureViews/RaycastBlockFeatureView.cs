using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// CanvasGroup.blocksRaycasts로 <see cref="RaycastBlockFeature"/>를 적용한다.
    /// IgnoreRaycast=true → blocksRaycasts=false (포인터가 통과).
    /// CanvasGroup이 없으면 자동 추가한다.
    /// InteractableFeatureView와 같은 CanvasGroup을 공유하는 경우 마지막 변경이 우선한다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Raycast Block Feature View")]
    public class RaycastBlockFeatureView : FeatureView<RaycastBlockFeature>
    {
        [SerializeField] private CanvasGroup canvasGroup;

        protected override void Awake()
        {
            EnsureCanvasGroup();
            base.Awake();
        }

        protected virtual void Reset()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void EnsureCanvasGroup()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        protected override void Bind(RaycastBlockFeature feature, ICollection<IDisposable> disposables)
        {
            EnsureCanvasGroup();
            feature.IgnoreRaycast.Subscribe(v => canvasGroup.blocksRaycasts = !v).AddTo(disposables);
        }

        protected override void Clear()
        {
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
        }
    }
}
