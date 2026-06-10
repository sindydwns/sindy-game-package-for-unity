using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// CanvasGroup으로 <see cref="InteractableFeature"/>를 적용한다.
    /// interactable과 blocksRaycasts를 함께 제어하므로 비활성 시 포인터 입력이 차단된다.
    /// CanvasGroup이 없으면 자동 추가한다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Interactable Feature View")]
    public class InteractableFeatureView : FeatureView<InteractableFeature>
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [Tooltip("비활성 상태에서의 알파. 1이면 시각 변화 없음.")]
        [Range(0f, 1f)]
        [SerializeField] private float disabledAlpha = 0.5f;

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

        protected override void Bind(InteractableFeature feature, ICollection<IDisposable> disposables)
        {
            EnsureCanvasGroup();
            feature.Interactable.Subscribe(v =>
            {
                canvasGroup.interactable = v;
                canvasGroup.blocksRaycasts = v;
                canvasGroup.alpha = v ? 1f : disabledAlpha;
            }).AddTo(disposables);
        }

        protected override void Clear()
        {
            if (canvasGroup == null) return;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }
}
