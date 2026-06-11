using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Features
{
    public enum Direction { Horizontal, Vertical }

    public class LayoutFeature : ModelFeature
    {
        internal float MarginTop, MarginRight, MarginBottom, MarginLeft;
        internal bool HasMargin;

        internal Direction? LayoutDirection;
        internal float Spacing;

        internal float PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
        internal bool HasPadding;

        internal TextAnchor? Alignment;

        internal float PreferredWidth = -1;
        internal float PreferredHeight = -1;

        internal bool HasLayout => LayoutDirection.HasValue;
        internal bool HasAlignment => Alignment.HasValue;
        internal bool HasSize => PreferredWidth >= 0 || PreferredHeight >= 0;

        // ── 공개 구성 API (ComponentBlueprint와 동일 시그니처) ─────────────────
        // Blueprint 없이도 ViewModel.With(new LayoutFeature().Layout(...))로 사용할 수 있다.

        /// <summary>자식 배치 방향과 간격을 지정한다.</summary>
        public LayoutFeature Layout(Direction direction, float spacing = 0)
        {
            LayoutDirection = direction;
            Spacing = spacing;
            return this;
        }

        /// <summary>내부 여백을 지정한다 (사방 동일).</summary>
        public LayoutFeature Padding(float all) => Padding(all, all, all, all);

        /// <summary>내부 여백을 지정한다.</summary>
        public LayoutFeature Padding(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            PaddingTop = top; PaddingRight = right; PaddingBottom = bottom; PaddingLeft = left;
            HasPadding = true;
            return this;
        }

        /// <summary>자식 정렬 기준을 지정한다.</summary>
        public LayoutFeature Align(TextAnchor anchor)
        {
            Alignment = anchor;
            return this;
        }

        /// <summary>선호 크기를 지정한다. -1이면 미지정.</summary>
        public LayoutFeature Size(float width = -1, float height = -1)
        {
            PreferredWidth = width;
            PreferredHeight = height;
            return this;
        }

        /// <summary>외부 여백을 지정한다.</summary>
        public LayoutFeature Margin(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            MarginTop = top; MarginRight = right; MarginBottom = bottom; MarginLeft = left;
            HasMargin = true;
            return this;
        }

        internal LayoutFeature Clone() => new()
        {
            MarginTop = MarginTop,
            MarginRight = MarginRight,
            MarginBottom = MarginBottom,
            MarginLeft = MarginLeft,
            HasMargin = HasMargin,
            LayoutDirection = LayoutDirection,
            Spacing = Spacing,
            PaddingTop = PaddingTop,
            PaddingRight = PaddingRight,
            PaddingBottom = PaddingBottom,
            PaddingLeft = PaddingLeft,
            HasPadding = HasPadding,
            Alignment = Alignment,
            PreferredWidth = PreferredWidth,
            PreferredHeight = PreferredHeight,
        };

        public void Apply(RectTransform target)
        {
            if (target == null) return;

            if (HasLayout)
            {
                // 재바인딩 시 중복 추가 방지: 같은 방향의 기존 LayoutGroup을 재사용한다.
                var group = LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.GetComponent<HorizontalLayoutGroup>()
                    : target.gameObject.GetComponent<VerticalLayoutGroup>();
                group ??= LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.AddComponent<HorizontalLayoutGroup>()
                    : target.gameObject.AddComponent<VerticalLayoutGroup>();

                group.spacing = Spacing;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                group.childControlWidth = true;
                group.childControlHeight = true;

                if (HasPadding)
                    group.padding = new RectOffset(
                        Mathf.RoundToInt(PaddingLeft),
                        Mathf.RoundToInt(PaddingRight),
                        Mathf.RoundToInt(PaddingTop),
                        Mathf.RoundToInt(PaddingBottom));

                if (HasAlignment)
                    group.childAlignment = Alignment.Value;
            }

            if (HasSize)
            {
                var element = target.gameObject.GetComponent<LayoutElement>()
                              ?? target.gameObject.AddComponent<LayoutElement>();
                if (PreferredWidth >= 0) element.preferredWidth = PreferredWidth;
                if (PreferredHeight >= 0) element.preferredHeight = PreferredHeight;
            }

            if (HasMargin)
            {
                target.offsetMin = new Vector2(MarginLeft, MarginBottom);
                target.offsetMax = new Vector2(-MarginRight, -MarginTop);
            }
        }
    }
}
