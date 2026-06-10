namespace Sindy.View.Features
{
    /// <summary>게이지(0~1) 출력 능력. <see cref="FeatureViews.GaugeFeatureView"/>와 1:1 대칭.</summary>
    public class GaugeFeature : ModelFeature
    {
        public PropModel<float> Ratio { get; }

        public GaugeFeature(float ratio = 0f)
        {
            Ratio = new PropModel<float>(ratio);
            Ratio.AddTo(this);
        }

        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public GaugeFeature(PropModel<float> external)
        {
            Ratio = external ?? throw new System.ArgumentNullException(nameof(external));
            Ratio.AddTo(this);
        }
    }
}
