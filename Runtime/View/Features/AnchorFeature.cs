using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Features
{
    /// <summary>
    /// 루트 배치 프리셋 — 부모 RectTransform 안에서 어디에, 어떤 방식으로 놓일지.
    /// uGUI Inspector의 앵커 프리셋 격자와 같은 이름 체계를 쓴다 (행: Top/Middle/Bottom/Stretch × 열: Left/Center/Right/Stretch).
    /// </summary>
    public enum AnchorPreset
    {
        /// <summary>전체 채움 (0,0)~(1,1). 전체 페이지.</summary>
        Stretch,
        /// <summary>중앙 점 고정. 중앙 다이얼로그 — 크기는 Anchor(width:, height:)로.</summary>
        Center,

        /// <summary>상단에 붙여 가로로 늘림. 탑바.</summary>
        TopStretch,
        /// <summary>하단에 붙여 가로로 늘림. 바텀시트.</summary>
        BottomStretch,
        /// <summary>좌측에 붙여 세로로 늘림. 좌측 드로어.</summary>
        LeftStretch,
        /// <summary>우측에 붙여 세로로 늘림. 우측 드로어.</summary>
        RightStretch,
        /// <summary>가로만 늘리고 세로는 중앙 고정. 배너.</summary>
        HorizontalStretch,
        /// <summary>세로만 늘리고 가로는 중앙 고정.</summary>
        VerticalStretch,

        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleRight,
        BottomLeft, BottomCenter, BottomRight,
    }

    /// <summary>
    /// RectTransform의 앵커·피벗·크기·인셋을 결정하는 배치 Feature. <see cref="FeatureViews.AnchorFeatureView"/>와 1:1 대칭.
    ///
    /// <see cref="LayoutFeature"/>가 "자식을 어떻게 늘어놓을지"(LayoutGroup)를 다룬다면,
    /// AnchorFeature는 "이 노드 자신이 부모 안 어디에 놓일지"(RectTransform 앵커)를 다룬다.
    /// 보통 ComponentBlueprint 체인의 <c>.Anchor(...)</c>/<c>.Inset(...)</c>으로 루트에 선언한다.
    ///
    /// 주의: 부모에 LayoutGroup이 있으면 앵커·위치·크기를 LayoutGroup이 매 프레임 덮어쓰므로 무효다
    /// (Dev 빌드에서 경고). 부모가 LayoutGroup을 쓰는 자식은 <see cref="LayoutFeature.Size"/>/<see cref="LayoutFeature.Flexible"/>로 크기를 정한다.
    /// </summary>
    public class AnchorFeature : ModelFeature
    {
        internal Vector2 AnchorMin = Vector2.zero;
        internal Vector2 AnchorMax = Vector2.one;
        internal Vector2 Pivot = new(0.5f, 0.5f);
        internal bool HasAnchor;

        /// <summary>점 고정 축의 크기. -1이면 프리팹의 크기를 유지한다. 늘림 축에서는 무시된다.</summary>
        internal float Width = -1;
        internal float Height = -1;

        /// <summary>가장자리로부터의 여백. 늘림 축은 양 끝을 줄이고, 점 고정 축은 붙은 변에서 안쪽으로 민다.</summary>
        internal float InsetTop, InsetRight, InsetBottom, InsetLeft;
        internal bool HasInset;

        internal bool StretchX => AnchorMin.x != AnchorMax.x;
        internal bool StretchY => AnchorMin.y != AnchorMax.y;

        // ── 공개 구성 API (ComponentBlueprint와 동일 시그니처) ─────────────────

        /// <summary>
        /// 프리셋으로 배치를 지정한다. 점 고정 축의 크기는 <paramref name="width"/>/<paramref name="height"/>로 주고,
        /// -1이면 프리팹의 크기를 유지한다. 늘림 축은 <see cref="Inset"/>으로 여백만 조절한다.
        /// </summary>
        public AnchorFeature Anchor(AnchorPreset preset, float width = -1, float height = -1)
        {
            var (min, max, pivot) = Resolve(preset);
            AnchorMin = min; AnchorMax = max; Pivot = pivot;
            HasAnchor = true;
            Width = width; Height = height;
            return this;
        }

        /// <summary>
        /// 정규화 좌표(0~1)로 앵커 사각형을 직접 지정한다 — 예: 화면의 6%~94% × 22%~78%를 차지하는 중앙 창.
        /// 두 축 모두 앵커에 맞춰 늘어나고(오프셋 0), 피벗은 앵커 사각형의 중심이 된다.
        /// </summary>
        public AnchorFeature Anchor(Vector2 anchorMin, Vector2 anchorMax)
        {
            AnchorMin = anchorMin; AnchorMax = anchorMax;
            Pivot = (anchorMin + anchorMax) * 0.5f;
            HasAnchor = true;
            Width = -1; Height = -1;
            return this;
        }

        /// <summary>가장자리 여백을 지정한다 (사방 동일).</summary>
        public AnchorFeature Inset(float all) => Inset(all, all, all, all);

        /// <summary>
        /// 가장자리 여백을 지정한다. 늘림 축은 양 끝에서 안쪽으로 줄이고(바텀시트 좌우 여백),
        /// 점 고정 축은 붙은 변에서 안쪽으로 민다(바텀시트를 바닥에서 띄우기).
        /// </summary>
        public AnchorFeature Inset(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            InsetTop = top; InsetRight = right; InsetBottom = bottom; InsetLeft = left;
            HasInset = true;
            return this;
        }

        internal AnchorFeature Clone() => new()
        {
            AnchorMin = AnchorMin,
            AnchorMax = AnchorMax,
            Pivot = Pivot,
            HasAnchor = HasAnchor,
            Width = Width,
            Height = Height,
            InsetTop = InsetTop,
            InsetRight = InsetRight,
            InsetBottom = InsetBottom,
            InsetLeft = InsetLeft,
            HasInset = HasInset,
        };

        /// <summary>
        /// 앵커·피벗·위치·크기를 대상에 반영한다. 앵커를 지정하지 않았으면(Inset만) 프리팹의 앵커를 유지한 채 여백만 반영한다.
        /// 늘림 축: 크기 = 부모 - (양쪽 인셋). 점 고정 축: 크기 = Width/Height(미지정 시 유지), 위치 = 붙은 변에서 인셋만큼 안쪽.
        /// </summary>
        public void Apply(RectTransform target)
        {
            if (target == null) return;

            if (HasAnchor)
            {
                target.anchorMin = AnchorMin;
                target.anchorMax = AnchorMax;
                target.pivot = Pivot;
            }

            var stretchX = target.anchorMin.x != target.anchorMax.x;
            var stretchY = target.anchorMin.y != target.anchorMax.y;
            var pivot = target.pivot;
            var size = target.sizeDelta;

            // 늘림 축은 인셋만큼 줄이고, 점 고정 축은 지정 크기를 쓴다(미지정 시 프리팹 크기 유지).
            size.x = stretchX ? -(InsetLeft + InsetRight) : (Width >= 0 ? Width : size.x);
            size.y = stretchY ? -(InsetTop + InsetBottom) : (Height >= 0 ? Height : size.y);

            // anchoredPosition은 피벗 기준이므로 인셋을 피벗 비율로 배분한다.
            //   피벗 0(좌/하단에 붙음) → +inset, 피벗 1(우/상단에 붙음) → -inset, 피벗 0.5 → 양쪽 차이의 절반.
            var pos = new Vector2(
                InsetLeft * (1f - pivot.x) - InsetRight * pivot.x,
                InsetBottom * (1f - pivot.y) - InsetTop * pivot.y);

            target.sizeDelta = size;
            target.anchoredPosition = pos;
        }

        /// <summary>프리셋 → (anchorMin, anchorMax, pivot).</summary>
        internal static (Vector2 min, Vector2 max, Vector2 pivot) Resolve(AnchorPreset preset) => preset switch
        {
            AnchorPreset.Stretch           => (new Vector2(0, 0),     new Vector2(1, 1),     new Vector2(0.5f, 0.5f)),
            AnchorPreset.Center            => (new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f)),

            AnchorPreset.TopStretch        => (new Vector2(0, 1),     new Vector2(1, 1),     new Vector2(0.5f, 1)),
            AnchorPreset.BottomStretch     => (new Vector2(0, 0),     new Vector2(1, 0),     new Vector2(0.5f, 0)),
            AnchorPreset.LeftStretch       => (new Vector2(0, 0),     new Vector2(0, 1),     new Vector2(0, 0.5f)),
            AnchorPreset.RightStretch      => (new Vector2(1, 0),     new Vector2(1, 1),     new Vector2(1, 0.5f)),
            AnchorPreset.HorizontalStretch => (new Vector2(0, 0.5f),  new Vector2(1, 0.5f),  new Vector2(0.5f, 0.5f)),
            AnchorPreset.VerticalStretch   => (new Vector2(0.5f, 0),  new Vector2(0.5f, 1),  new Vector2(0.5f, 0.5f)),

            AnchorPreset.TopLeft           => (new Vector2(0, 1),     new Vector2(0, 1),     new Vector2(0, 1)),
            AnchorPreset.TopCenter         => (new Vector2(0.5f, 1),  new Vector2(0.5f, 1),  new Vector2(0.5f, 1)),
            AnchorPreset.TopRight          => (new Vector2(1, 1),     new Vector2(1, 1),     new Vector2(1, 1)),
            AnchorPreset.MiddleLeft        => (new Vector2(0, 0.5f),  new Vector2(0, 0.5f),  new Vector2(0, 0.5f)),
            AnchorPreset.MiddleRight       => (new Vector2(1, 0.5f),  new Vector2(1, 0.5f),  new Vector2(1, 0.5f)),
            AnchorPreset.BottomLeft        => (new Vector2(0, 0),     new Vector2(0, 0),     new Vector2(0, 0)),
            AnchorPreset.BottomCenter      => (new Vector2(0.5f, 0),  new Vector2(0.5f, 0),  new Vector2(0.5f, 0)),
            AnchorPreset.BottomRight       => (new Vector2(1, 0),     new Vector2(1, 0),     new Vector2(1, 0)),

            _ => (Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f)),
        };

        /// <summary>
        /// 부모 LayoutGroup이 이 노드의 배치를 덮어쓰는지 검사한다 (Dev 빌드 경고용).
        /// </summary>
        internal static bool IsOverriddenByParentLayout(RectTransform target)
        {
            var parent = target != null ? target.parent : null;
            if (parent == null) return false;
            var group = parent.GetComponent<LayoutGroup>();
            return group != null && group.enabled;
        }
    }
}
