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

        public GridLayout(int columns, float cellWidth, float startOffset, float gap)
        {
            Columns = columns;
            CellWidth = cellWidth;
            StartOffset = startOffset;
            Gap = gap;
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
            var gap = opt.HorizontalGap;

            // 1) 선호 기준 컬럼 수 추정
            int cols = Mathf.FloorToInt((available + gap) / Mathf.Max(0.0001f, opt.CellPreferredWidth + gap));
            if (cols < 1) cols = 1;

            // 2) cellWidth 계산 + 검증/조정 루프
            float cellWidth;
            while (true)
            {
                cellWidth = (available - gap * (cols - 1)) / cols;

                // FR-GRID-04. 최소 1개의 컬럼은 보장. cellMin보다 작더라도 1로 유지하며 가용너비에 맞춰 축소.
                if (cellWidth < opt.CellMinWidth && cols > 1)
                {
                    cols--;
                    continue;
                }

                break;
            }

            // 3) cellMax 초과 시 정렬 정책 적용
            var startOffset = (float)paddingLeft;
            if (cellWidth > opt.CellMaxWidth)
            {
                switch (opt.HorizontalAlignment)
                {
                    case GridHorizontalAlignment.Stretch:
                        // 셀 너비를 그대로 사용. 셀 최대 너비 초과를 허용.
                        break;
                    case GridHorizontalAlignment.Left:
                        cellWidth = opt.CellMaxWidth;
                        // startOffset은 좌측 패딩 그대로 (남은 공간이 우측에 자연스럽게 남음)
                        break;
                    case GridHorizontalAlignment.Center:
                        cellWidth = opt.CellMaxWidth;
                        var rowWidth = cols * cellWidth + (cols - 1) * gap;
                        var slack = available - rowWidth;
                        startOffset = paddingLeft + slack * 0.5f;
                        break;
                }
            }

            return new GridLayout(cols, cellWidth, startOffset, gap);
        }

        public static int RowCount(int itemCount, int columns)
        {
            if (itemCount <= 0 || columns <= 0) return 0;
            return (itemCount + columns - 1) / columns;
        }
    }
}
