using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// <see cref="ScreenFeature"/>의 variant 키에 따라 등록된 RectTransform들의
    /// 앵커·오프셋·피벗을 일괄 전환한다.
    ///
    /// 좌표는 뷰의 직렬화 데이터(variants)에 저장되며, 모델은 variant 키만 안다.
    /// 코드로 구성할 때는 <see cref="RectState.From"/>으로 현재 배치를 캡처해 등록할 수 있다.
    ///
    /// 주의: 전환 후 ScrollRect 등 크기에 민감한 컴포넌트는 소비자가 재계산을 트리거해야
    /// 할 수 있다 (예: 스크롤러 허브의 Reload()).
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Responsive Layout Feature View")]
    public class ResponsiveLayoutFeatureView : FeatureView<ScreenFeature>
    {
        /// <summary>RectTransform 배치 스냅샷.</summary>
        [Serializable]
        public class RectState
        {
            public RectTransform target;
            public Vector2 anchorMin;
            public Vector2 anchorMax;
            public Vector2 offsetMin;
            public Vector2 offsetMax;
            public Vector2 pivot;

            public void Apply()
            {
                if (target == null) return;
                target.anchorMin = anchorMin;
                target.anchorMax = anchorMax;
                target.pivot = pivot;
                target.offsetMin = offsetMin;
                target.offsetMax = offsetMax;
            }

            /// <summary>현재 배치를 캡처한다.</summary>
            public static RectState From(RectTransform rect) => new()
            {
                target = rect,
                anchorMin = rect.anchorMin,
                anchorMax = rect.anchorMax,
                offsetMin = rect.offsetMin,
                offsetMax = rect.offsetMax,
                pivot = rect.pivot,
            };
        }

        [Serializable]
        public class Variant
        {
            public string key;
            public List<RectState> rects = new();
        }

        [SerializeField] private List<Variant> variants = new();

        /// <summary>현재 적용된 variant 키 (없으면 null).</summary>
        public string CurrentVariant { get; private set; }

        protected override void Bind(ScreenFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Variant.Subscribe(Apply).AddTo(disposables);
        }

        /// <summary>코드로 variant를 등록한다. 같은 키가 있으면 교체한다.</summary>
        public void SetVariant(string key, params RectState[] states)
        {
            var variant = variants.Find(v => v.key == key);
            if (variant == null)
            {
                variant = new Variant { key = key };
                variants.Add(variant);
            }
            variant.rects = new List<RectState>(states);
        }

        private void Apply(string key)
        {
            if (string.IsNullOrEmpty(key)) return;

            var variant = variants.Find(v => v.key == key);
            if (variant == null)
            {
                Debug.LogWarning($"ResponsiveLayoutFeatureView: variant '{key}' not found.", this);
                return;
            }

            foreach (var state in variant.rects)
                state.Apply();

            CurrentVariant = key;
        }
    }
}
