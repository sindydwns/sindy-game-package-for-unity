namespace Sindy.View.Scroller
{
    /// <summary>FR-SCROLL-02. 스크롤 점프 시 대상이 뷰포트 내에서 정렬되는 위치.</summary>
    public enum ScrollAlignment
    {
        Top,
        Center,
        Bottom,
    }

    /// <summary>FR-GRID-02. 산출 셀 너비가 최대 너비를 초과할 때의 가로 정렬 정책.</summary>
    public enum GridHorizontalAlignment
    {
        Stretch,
        Left,
        Center,
    }
}
