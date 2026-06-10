namespace Sindy.View.Features
{
    public class VisibilityFeature : ModelFeature
    {
        public PropModel<bool> Show { get; }

        public VisibilityFeature(bool initialValue = true)
        {
            Show = new PropModel<bool>(initialValue);
            Show.AddTo(this);
        }
    
        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public VisibilityFeature(PropModel<bool> external)
        {
            Show = external ?? throw new System.ArgumentNullException(nameof(external));
            Show.AddTo(this);
        }
}
}
