using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Features
{
    public enum Direction { Horizontal, Vertical }

    public class LayoutFeature : ModelFeature
    {
        internal Direction? LayoutDirection;
        internal float Spacing;

        internal float PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
        internal bool HasPadding;

        internal TextAnchor? Alignment;

        internal float PreferredWidth = -1;
        internal float PreferredHeight = -1;

        internal float FlexibleWidth = -1;
        internal float FlexibleHeight = -1;

        internal bool HasLayout => LayoutDirection.HasValue;
        internal bool HasAlignment => Alignment.HasValue;
        internal bool HasSize => PreferredWidth >= 0 || PreferredHeight >= 0;
        internal bool HasFlexible => FlexibleWidth >= 0 || FlexibleHeight >= 0;
        internal bool HasLayoutElement => HasSize || HasFlexible;

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

        /// <summary>
        /// 유연 크기 가중치를 지정한다 (LayoutElement.flexibleWidth/Height). -1이면 미지정.
        /// 부모 LayoutGroup의 남는 공간을 형제들과 가중치 비율로 나눠 갖는다 —
        /// 예: 행에서 라벨이 남은 가로폭을 모두 채우게 하려면 Flexible(width: 1).
        /// </summary>
        public LayoutFeature Flexible(float width = -1, float height = -1)
        {
            FlexibleWidth = width;
            FlexibleHeight = height;
            return this;
        }

        internal LayoutFeature Clone() => new()
        {
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
            FlexibleWidth = FlexibleWidth,
            FlexibleHeight = FlexibleHeight,
        };

        /// <summary>
        /// 이 Feature의 전체 상태를 대상에 반영한다 (full-spec).
        /// 지정하지 않은 속성은 기본값으로 리셋되므로, 셀 풀링·재바인딩 시
        /// 이전 모델의 레이아웃이 잔존하지 않는다.
        /// </summary>
        public void Apply(RectTransform target)
        {
            if (target == null) return;

            if (HasLayout)
            {
                // LayoutGroup은 DisallowMultipleComponent — 방향 전환 시 기존 그룹을 즉시 제거해야
                // 새 방향 그룹을 추가할 수 있다 (재바인딩 시 그룹 충돌 방지).
                var opposite = LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.GetComponent<VerticalLayoutGroup>()
                    : target.gameObject.GetComponent<HorizontalLayoutGroup>();
                if (opposite != null)
                    Object.DestroyImmediate(opposite);

                var group = LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.GetComponent<HorizontalLayoutGroup>()
                    : target.gameObject.GetComponent<VerticalLayoutGroup>();
                group ??= LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.AddComponent<HorizontalLayoutGroup>()
                    : target.gameObject.AddComponent<VerticalLayoutGroup>();

                group.enabled = true;
                group.spacing = Spacing;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                group.childControlWidth = true;
                group.childControlHeight = true;

                // full-spec: 미지정이면 0/기본값으로 리셋
                group.padding = HasPadding
                    ? new RectOffset(
                        Mathf.RoundToInt(PaddingLeft),
                        Mathf.RoundToInt(PaddingRight),
                        Mathf.RoundToInt(PaddingTop),
                        Mathf.RoundToInt(PaddingBottom))
                    : new RectOffset(0, 0, 0, 0);
                group.childAlignment = Alignment ?? TextAnchor.UpperLeft;
            }
            else
            {
                // 이 Feature에 레이아웃이 없으면 이전 모델이 남긴 그룹을 비활성화한다.
                SetGroupsEnabled(target, false);
            }

            if (HasLayoutElement)
            {
                var element = target.gameObject.GetComponent<LayoutElement>()
                              ?? target.gameObject.AddComponent<LayoutElement>();
                element.enabled = true;
                // full-spec: 미지정 축은 -1(미사용)로 리셋
                element.preferredWidth = PreferredWidth;
                element.preferredHeight = PreferredHeight;
                element.flexibleWidth = FlexibleWidth;
                element.flexibleHeight = FlexibleHeight;
            }
            else
            {
                var element = target.gameObject.GetComponent<LayoutElement>();
                if (element != null) element.enabled = false;
            }
        }

        /// <summary>
        /// 적용했던 레이아웃 영향을 비활성화한다 (모델 해제 시 LayoutFeatureView.Clear에서 호출).
        /// 셀 풀링 성능을 위해 컴포넌트를 파괴하지 않고 비활성 토글만 한다 —
        /// 다음 Apply가 전체 상태를 다시 설정한다.
        /// </summary>
        public static void Deactivate(RectTransform target)
        {
            if (target == null) return;
            SetGroupsEnabled(target, false);
            var element = target.gameObject.GetComponent<LayoutElement>();
            if (element != null) element.enabled = false;
        }

        private static void SetGroupsEnabled(RectTransform target, bool enabled)
        {
            var group = target.gameObject.GetComponent<HorizontalOrVerticalLayoutGroup>();
            if (group != null) group.enabled = enabled;
        }
    }
}
