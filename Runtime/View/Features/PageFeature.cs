namespace Sindy.View.Features
{
    /// <summary>페이지 전환 능력. <see cref="FeatureViews.PageFeatureView"/>와 1:1 대칭.</summary>
    public class PageFeature : ModelFeature
    {
        public PropModel<int> PageIndex { get; }

        public PageFeature(int pageIndex = 0)
        {
            PageIndex = new PropModel<int>(pageIndex);
            PageIndex.AddTo(this);
        }
    }
}
