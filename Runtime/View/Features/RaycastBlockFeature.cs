namespace Sindy.View.Features
{
    public class RaycastBlockFeature : ModelFeature
    {
        public PropModel<bool> IgnoreRaycast { get; }

        public RaycastBlockFeature(bool initialValue = false)
        {
            IgnoreRaycast = new PropModel<bool>(initialValue);
            IgnoreRaycast.AddTo(this);
        }
    }
}
