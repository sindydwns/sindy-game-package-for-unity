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
    
        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public HighlightFeature(PropModel<bool> external)
        {
            Highlight = external ?? throw new System.ArgumentNullException(nameof(external));
            Highlight.AddTo(this);
        }
}
}
