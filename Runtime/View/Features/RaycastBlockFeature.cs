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
    
        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public RaycastBlockFeature(PropModel<bool> external)
        {
            IgnoreRaycast = external ?? throw new System.ArgumentNullException(nameof(external));
            IgnoreRaycast.AddTo(this);
        }
}
}
