using System;
using UnityEngine;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 한 섹션의 한 슬롯을 식별하는 키.
    /// Slot 의미: -1=Header, -2=Footer, -3=Empty, &gt;=0=ContentItemIndex
    /// </summary>
    internal readonly struct CellKey : IEquatable<CellKey>
    {
        public const int HeaderSlot = -1;
        public const int FooterSlot = -2;
        public const int EmptySlot = -3;

        public readonly int Section;
        public readonly int Slot;

        public CellKey(int section, int slot) { Section = section; Slot = slot; }

        public bool Equals(CellKey other) => Section == other.Section && Slot == other.Slot;
        public override bool Equals(object obj) => obj is CellKey k && Equals(k);
        public override int GetHashCode() => unchecked(Section * 397) ^ Slot;
    }

    internal struct ActiveCell
    {
        public SindyComponent Instance;
        public SindyComponent Prefab;
    }

    /// <summary>
    /// 한 섹션의 레이아웃 캐시. 데이터 변경 또는 컨테이너 너비 변경 시 재계산된다.
    /// </summary>
    internal struct SectionLayout
    {
        public bool IsVisible;        // FR-EMPTY-02. 섹션 자체가 표시되지 않으면 false (마진도 미적용)
        public bool ShowHeader;
        public bool ShowFooter;
        public bool ShowEmpty;

        public float TopY;            // 콘텐츠 좌표계 기준 섹션 시작 y (0 = 콘텐츠 최상단)
        public float TopMargin;
        public float BottomMargin;

        public float HeaderHeight;
        public float ContentHeight;   // 그리드 영역 또는 빈 콘텐츠 영역의 높이
        public float FooterHeight;
        public float TotalHeight;

        public GridLayout Grid;
        public int RowCount;
        public float CellHeight;

        public SindyComponent ContentPrefab;
        public SindyComponent HeaderPrefab;
        public SindyComponent FooterPrefab;
        public SindyComponent EmptyPrefab;

        public Vector2 EmptyPrefabSize;

        public float HeaderTopY => TopY + TopMargin;
        public float ContentTopY => HeaderTopY + HeaderHeight;
        public float FooterTopY => ContentTopY + ContentHeight;
    }
}
