using UnityEngine;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 그리드 레이아웃 1행의 셀 배치 결과.
    /// </summary>
    internal readonly struct GridLayout
    {
        public readonly int Columns;
        public readonly float CellWidth;
        public readonly float StartOffset; // 컨텐츠 영역 안 row의 좌측 시작 X
        public readonly float Gap;
        // 부록 A. alignment는 cellWidth > cellMax일 때만 의미가 있다 (FR-GRID-02).
        // cellMax 미초과 시에는 옵션의 정렬 모드와 무관하게 Stretch로 동작한다고 보는 것이 자연스럽다.
        // 이 필드는 그 의미를 명시화한 것으로, PositionCell이 anchor/offset 산출 시 분기 기준으로 사용한다.
        public readonly GridHorizontalAlignment EffectiveAlignment;

        public GridLayout(int columns, float cellWidth, float startOffset, float gap, GridHorizontalAlignment effectiveAlignment)
        {
            Columns = columns;
            CellWidth = cellWidth;
            StartOffset = startOffset;
            Gap = gap;
            EffectiveAlignment = effectiveAlignment;
        }

        public float CellX(int colIndex) => StartOffset + colIndex * (CellWidth + Gap);
    }

    /// <summary>
    /// 부록 A. 그리드 컬럼 산출 알고리즘.
    /// </summary>
    internal static class GridLayoutResolver
    {
        public static GridLayout Resolve(float containerWidth, SectionOption opt)
        {
            var paddingLeft = opt.HorizontalPadding != null ? opt.HorizontalPadding.left : 0;
            var paddingRight = opt.HorizontalPadding != null ? opt.HorizontalPadding.right : 0;

            var available = Mathf.Max(0f, containerWidth - paddingLeft - paddingRight);
            var gap = Mathf.Max(0f, opt.HorizontalGap);

            // 잘못된 SectionOption 입력에 대한 방어:
            //   CellPreferredWidth/CellMinWidth가 0 또는 음수면 분모가 0에 가까워져 cols가 폭주하고,
            //   이어지는 while 루프가 cols를 1씩 감소시키며 수백만 회 반복하여 stall이 발생할 수 있다.
            // 따라서 1f 이상의 값으로 가드하여 cols 초기값과 cellMin 기반 상한을 모두 sane한 범위에서 산출한다.
            // CellMaxWidth도 동일 정책을 적용 — 0 또는 음수 입력 시 Left/Center 분기에서 cellWidth가
            // 0/음수가 되어 broken layout이 되는 것을 막는다. 또한 max < min인 경우 max를 min으로 끌어올려
            // "max < min"의 모순된 입력에서도 일관된 동작을 보장한다.
            var safePref = Mathf.Max(1f, opt.CellPreferredWidth);
            var safeMin = Mathf.Max(1f, opt.CellMinWidth);
            var safeMax = Mathf.Max(safeMin, opt.CellMaxWidth);

            // 1) 선호 기준 컬럼 수 추정
            int cols = Mathf.FloorToInt((available + gap) / (safePref + gap));

            // 1.5) cellMin이 허용하는 최대 컬럼 수로 상한을 두어 while 루프의 반복 횟수를 유한하게 보장
            int maxColsByMin = Mathf.FloorToInt((available + gap) / (safeMin + gap));
            if (maxColsByMin < 1) maxColsByMin = 1;
            cols = Mathf.Clamp(cols, 1, maxColsByMin);

            // 2) cellWidth 계산 + 검증/조정 루프 (이제 cols가 최대 maxColsByMin이라 수렴 보장)
            float cellWidth;
            while (true)
            {
                cellWidth = (available - gap * (cols - 1)) / cols;

                // FR-GRID-04. 최소 1개의 컬럼은 보장. cellMin보다 작더라도 1로 유지하며 가용너비에 맞춰 축소.
                // safeMin(>=1f)을 사용하여 opt.CellMinWidth가 0 또는 음수일 때도 가드가 일관되게 적용되도록 한다.
                if (cellWidth < safeMin && cols > 1)
                {
                    cols--;
                    continue;
                }

                break;
            }

            // 3) cellMax 초과 시 정렬 정책 적용. cellMax 미초과면 옵션과 무관하게 Stretch로 간주한다.
            // safeMax(>=safeMin)를 사용하여 opt.CellMaxWidth가 0/음수/min미만인 경우에도 cellWidth가
            // 0/음수가 되지 않도록 한다 (safePref/safeMin과 동일한 가드 정책).
            var startOffset = (float)paddingLeft;
            var effectiveAlignment = GridHorizontalAlignment.Stretch;
            if (cellWidth > safeMax)
            {
                effectiveAlignment = opt.HorizontalAlignment;
                switch (opt.HorizontalAlignment)
                {
                    case GridHorizontalAlignment.Stretch:
                        // 셀 너비를 그대로 사용. 셀 최대 너비 초과를 허용.
                        break;
                    case GridHorizontalAlignment.Left:
                        cellWidth = safeMax;
                        // startOffset은 좌측 패딩 그대로 (남은 공간이 우측에 자연스럽게 남음)
                        break;
                    case GridHorizontalAlignment.Center:
                        cellWidth = safeMax;
                        var rowWidth = cols * cellWidth + (cols - 1) * gap;
                        var slack = available - rowWidth;
                        startOffset = paddingLeft + slack * 0.5f;
                        break;
                }
            }

            return new GridLayout(cols, cellWidth, startOffset, gap, effectiveAlignment);
        }

        public static int RowCount(int itemCount, int columns)
        {
            if (itemCount <= 0 || columns <= 0) return 0;
            return (itemCount + columns - 1) / columns;
        }
    }
}
