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
    }
}
