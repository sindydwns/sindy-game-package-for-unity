namespace Sindy.View.Features
{
    /// <summary>탭 선택 능력 (양방향). <see cref="FeatureViews.TabFeatureView"/>와 1:1 대칭.</summary>
    public class TabFeature : ModelFeature
    {
        public PropModel<int> SelectedIndex { get; }

        public TabFeature(int selectedIndex = 0)
        {
            SelectedIndex = new PropModel<int>(selectedIndex);
            SelectedIndex.AddTo(this);
        }
    }
}
