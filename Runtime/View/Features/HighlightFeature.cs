namespace Sindy.View.Features
{
    public class HighlightFeature : ModelFeature
    {
        public PropModel<bool> Highlight { get; }

        public HighlightFeature(bool initialValue = false)
        {
            Highlight = new PropModel<bool>(initialValue);
            Highlight.AddTo(this);
        }
    }
}
