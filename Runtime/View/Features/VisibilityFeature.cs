namespace Sindy.View.Features
{
    public class VisibilityFeature : ViewModelFeature
    {
        public PropModel<bool> Show { get; }

        public VisibilityFeature(bool initialValue = true)
        {
            Show = new PropModel<bool>(initialValue);
            Show.AddTo(this);
        }
    }
}
