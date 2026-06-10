namespace Sindy.View.Features
{
    /// <summary>온/오프 토글 능력 (양방향). <see cref="FeatureViews.ToggleFeatureView"/>와 1:1 대칭.</summary>
    public class ToggleFeature : ModelFeature
    {
        public PropModel<bool> IsOn { get; }

        public ToggleFeature(bool isOn = false)
        {
            IsOn = new PropModel<bool>(isOn);
            IsOn.AddTo(this);
        }

        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public ToggleFeature(PropModel<bool> external)
        {
            IsOn = external ?? throw new System.ArgumentNullException(nameof(external));
            IsOn.AddTo(this);
        }
    }
}
