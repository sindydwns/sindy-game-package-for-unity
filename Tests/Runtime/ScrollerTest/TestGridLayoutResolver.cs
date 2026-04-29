using Sindy.View.Scroller;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// GridLayoutResolver — 부록 A의 컬럼 산출 알고리즘 검증.
    /// FR-GRID-01 ~ FR-GRID-04 충족 여부 확인.
    /// </summary>
    class TestGridLayoutResolver : TestCase
    {
        public override void Run()
        {
            PreferredWidthFits_UsesPreferredColumnCount();
            WidthBelowMin_FallsBackToOneColumn();
            ExceedsMax_StretchKeepsComputedWidth();
            ExceedsMax_LeftClampsToCellMaxAndKeepsLeftPadding();
            ExceedsMax_CenterClampsAndCentersRow();
            HorizontalGapReducesCellWidth();
            HorizontalPaddingShrinksAvailable();
            RowCount_IsCeilOfItemsOverColumns();
            ZeroItems_ProducesZeroRows();
            AlwaysAtLeastOneColumn();
            EffectiveAlignment_IsStretchWhenWithinMax();
            EffectiveAlignment_PreservesOptionWhenExceedsMax();
            InvalidConfig_DoesNotProduceTinyCells();
            InvalidMaxWidth_DoesNotProduceZeroOrNegativeCellWidth();
        }

        // CellMaxWidth가 0/음수/min 미만일 때 Left/Center 분기에서 cellWidth가 0/음수가 되지 않는지 검증.
        // safeMax = max(safeMin, opt.CellMaxWidth)로 가드하여 cellWidth >= safeMin >= 1f이 보장된다.
        private void InvalidMaxWidth_DoesNotProduceZeroOrNegativeCellWidth()
        {
            // max=0 + Left
            var optL = MakeOption(min: 80, pref: 100, max: 0, align: GridHorizontalAlignment.Left);
            var gL = GridLayoutResolver.Resolve(480f, optL);
            Assert.IsTrue(gL.CellWidth >= 1f - 0.01f, $"Left max=0: cellWidth = {gL.CellWidth}");

            // max=-50 + Center
            var optC = MakeOption(min: 80, pref: 100, max: -50, align: GridHorizontalAlignment.Center);
            var gC = GridLayoutResolver.Resolve(480f, optC);
            Assert.IsTrue(gC.CellWidth >= 1f - 0.01f, $"Center max=-50: cellWidth = {gC.CellWidth}");

            // max < min (모순 입력) → safeMax는 safeMin으로 끌어올려진다
            var optM = MakeOption(min: 100, pref: 110, max: 50, align: GridHorizontalAlignment.Left);
            var gM = GridLayoutResolver.Resolve(480f, optM);
            Assert.IsTrue(gM.CellWidth >= 100f - 0.01f, $"max<min: cellWidth({gM.CellWidth})는 적어도 min(100)이어야 함");
        }

        private static SectionOption MakeOption(
            float min, float pref, float max,
            float gap = 0f,
            int padLeft = 0, int padRight = 0,
            GridHorizontalAlignment align = GridHorizontalAlignment.Stretch)
        {
            var opt = ScriptableObject.CreateInstance<SectionOption>();
            opt.CellMinWidth = min;
            opt.CellPreferredWidth = pref;
            opt.CellMaxWidth = max;
            opt.HorizontalGap = gap;
            opt.HorizontalPadding = new RectOffset(padLeft, padRight, 0, 0);
            opt.HorizontalAlignment = align;
            return opt;
        }

        // 가용너비 600, 선호 100 → 6 컬럼 (gap 없음).
        private void PreferredWidthFits_UsesPreferredColumnCount()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 200);
            var g = GridLayoutResolver.Resolve(600f, opt);
            Assert.AreEqual(6, g.Columns);
            Assert.AreEqual(100f, g.CellWidth, 0.01f);
        }

        // 가용너비가 cellMin보다 작아도 컬럼은 1 (FR-GRID-04). 셀은 가용너비에 맞춰 축소된다.
        private void WidthBelowMin_FallsBackToOneColumn()
        {
            var opt = MakeOption(min: 200, pref: 220, max: 240);
            var g = GridLayoutResolver.Resolve(120f, opt);
            Assert.AreEqual(1, g.Columns);
            // cellMin보다 작더라도 컬럼 1 + 가용너비 그대로
            Assert.AreEqual(120f, g.CellWidth, 0.01f);
        }

        // Stretch 정렬에서 cellWidth가 cellMax를 초과해도 그대로 사용한다 (FR-GRID-02).
        private void ExceedsMax_StretchKeepsComputedWidth()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 110, align: GridHorizontalAlignment.Stretch);
            // 가용너비 480, 선호 100 → 4 컬럼, cellWidth = 120 (max 110 초과)
            var g = GridLayoutResolver.Resolve(480f, opt);
            Assert.AreEqual(4, g.Columns);
            Assert.AreEqual(120f, g.CellWidth, 0.01f); // Stretch는 max 무시
        }

        // Left 정렬은 cellWidth를 cellMax로 고정하고 좌측 패딩에서 시작한다.
        private void ExceedsMax_LeftClampsToCellMaxAndKeepsLeftPadding()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 110, padLeft: 0, padRight: 0,
                align: GridHorizontalAlignment.Left);
            var g = GridLayoutResolver.Resolve(480f, opt);
            Assert.AreEqual(4, g.Columns);
            Assert.AreEqual(110f, g.CellWidth, 0.01f);
            Assert.AreEqual(0f, g.StartOffset, 0.01f);
            // 0번째 셀 x = 0
            Assert.AreEqual(0f, g.CellX(0), 0.01f);
            // 1번째 셀 x = 110
            Assert.AreEqual(110f, g.CellX(1), 0.01f);
        }

        // Center 정렬은 cellWidth를 cellMax로 고정하고 남은 공간을 좌우 균등 분배.
        private void ExceedsMax_CenterClampsAndCentersRow()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 110,
                align: GridHorizontalAlignment.Center);
            // 가용너비 480, 4 컬럼, cellMax 110 → 행 너비 440, slack 40 → 좌우 20씩
            var g = GridLayoutResolver.Resolve(480f, opt);
            Assert.AreEqual(4, g.Columns);
            Assert.AreEqual(110f, g.CellWidth, 0.01f);
            Assert.AreEqual(20f, g.StartOffset, 0.01f);
            Assert.AreEqual(20f, g.CellX(0), 0.01f);
            Assert.AreEqual(130f, g.CellX(1), 0.01f);
        }

        // 가로 간격이 있으면 셀 너비가 그만큼 줄어든다.
        private void HorizontalGapReducesCellWidth()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 200, gap: 20f);
            // (가용 + gap) / (pref + gap) = (600 + 20) / 120 = 5.16 → 5 컬럼
            // cellWidth = (600 - 20*4) / 5 = 520/5 = 104
            var g = GridLayoutResolver.Resolve(600f, opt);
            Assert.AreEqual(5, g.Columns);
            Assert.AreEqual(104f, g.CellWidth, 0.01f);
            Assert.AreEqual(20f, g.Gap, 0.01f);
            Assert.AreEqual(0f, g.CellX(0), 0.01f);
            Assert.AreEqual(124f, g.CellX(1), 0.01f); // 104 + 20
        }

        // 좌우 패딩은 가용너비에서 빠진다.
        private void HorizontalPaddingShrinksAvailable()
        {
            var opt = MakeOption(min: 50, pref: 100, max: 200, padLeft: 50, padRight: 50);
            // 가용 = 600 - 50 - 50 = 500. 선호 100 → 5 컬럼, cellWidth = 100
            var g = GridLayoutResolver.Resolve(600f, opt);
            Assert.AreEqual(5, g.Columns);
            Assert.AreEqual(100f, g.CellWidth, 0.01f);
            Assert.AreEqual(50f, g.StartOffset, 0.01f);
        }

        private void RowCount_IsCeilOfItemsOverColumns()
        {
            Assert.AreEqual(0, GridLayoutResolver.RowCount(0, 4));
            Assert.AreEqual(1, GridLayoutResolver.RowCount(1, 4));
            Assert.AreEqual(1, GridLayoutResolver.RowCount(4, 4));
            Assert.AreEqual(2, GridLayoutResolver.RowCount(5, 4));
            Assert.AreEqual(3, GridLayoutResolver.RowCount(9, 4));
        }

        private void ZeroItems_ProducesZeroRows()
        {
            Assert.AreEqual(0, GridLayoutResolver.RowCount(0, 1));
            Assert.AreEqual(0, GridLayoutResolver.RowCount(0, 999));
        }

        // FR-GRID-04. 어떤 입력이라도 최소 1 컬럼 보장.
        private void AlwaysAtLeastOneColumn()
        {
            var opt = MakeOption(min: 1000, pref: 1000, max: 1000);
            var g = GridLayoutResolver.Resolve(50f, opt);
            Assert.AreEqual(1, g.Columns);
        }

        // 부록 A의 의미: alignment는 cellWidth > cellMax일 때만 의미가 있고,
        // 미초과 시에는 옵션과 무관하게 Stretch로 동작한다 (PositionCell이 이를 분기 기준으로 사용).
        private void EffectiveAlignment_IsStretchWhenWithinMax()
        {
            var opt = MakeOption(min: 80, pref: 100, max: 200, align: GridHorizontalAlignment.Center);
            // cellWidth = 100 < max 200 → effective는 Stretch
            var g = GridLayoutResolver.Resolve(600f, opt);
            Assert.AreEqual(GridHorizontalAlignment.Stretch, g.EffectiveAlignment);

            var opt2 = MakeOption(min: 80, pref: 100, max: 200, align: GridHorizontalAlignment.Left);
            var g2 = GridLayoutResolver.Resolve(600f, opt2);
            Assert.AreEqual(GridHorizontalAlignment.Stretch, g2.EffectiveAlignment);
        }

        // cellWidth > cellMax일 때만 옵션 alignment가 effective로 적용된다.
        private void EffectiveAlignment_PreservesOptionWhenExceedsMax()
        {
            var optC = MakeOption(min: 80, pref: 100, max: 110, align: GridHorizontalAlignment.Center);
            var gC = GridLayoutResolver.Resolve(480f, optC);
            Assert.AreEqual(GridHorizontalAlignment.Center, gC.EffectiveAlignment);

            var optL = MakeOption(min: 80, pref: 100, max: 110, align: GridHorizontalAlignment.Left);
            var gL = GridLayoutResolver.Resolve(480f, optL);
            Assert.AreEqual(GridHorizontalAlignment.Left, gL.EffectiveAlignment);

            var optS = MakeOption(min: 80, pref: 100, max: 110, align: GridHorizontalAlignment.Stretch);
            var gS = GridLayoutResolver.Resolve(480f, optS);
            Assert.AreEqual(GridHorizontalAlignment.Stretch, gS.EffectiveAlignment);
        }

        // 디자이너가 SectionOption을 잘못 설정해 min/pref/gap이 0 또는 음수가 된 경우에도
        // while 루프가 발산하거나 의미 없이 작은 셀이 양산되지 않아야 한다.
        // safeMin(>=1f)이 while 루프 비교에도 일관되게 적용되어, cellWidth가 1f 이상으로 수렴한다.
        private void InvalidConfig_DoesNotProduceTinyCells()
        {
            var opt = MakeOption(min: 0, pref: 0, max: 0);
            var g = GridLayoutResolver.Resolve(600f, opt);

            // safeMin = 1f로 가드되어 cellWidth가 1f 미만이 되지 않는다.
            Assert.IsTrue(g.CellWidth >= 1f - 0.01f, $"cellWidth too small: {g.CellWidth}");
            // cols가 폭주하지 않는다 (available / safeMin = 600).
            Assert.IsTrue(g.Columns <= 600, $"cols too large: {g.Columns}");

            var optNeg = MakeOption(min: -10, pref: -5, max: 0);
            var gN = GridLayoutResolver.Resolve(600f, optNeg);
            Assert.IsTrue(gN.CellWidth >= 1f - 0.01f);
            Assert.IsTrue(gN.Columns <= 600);
        }
    }
}
