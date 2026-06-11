namespace Sindy.Samples.ComponentShowcase
{
    /// <summary>
    /// 스크롤러 셀 키 모음. 오타 방지를 위해 const로 선언한다.
    /// CellCatalog 에셋(ShopCellCatalog)의 키와 1:1로 일치해야 한다.
    /// 배너 셀은 키 등록 없이 Section.ContentPrefab 직접 지정 방식을 시연하므로 여기 없다.
    /// </summary>
    public static class ShopCellKeys
    {
        public const string Item = "shop.item";
        public const string Header = "shop.header";
    }
}
