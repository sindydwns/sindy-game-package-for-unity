using UnityEngine;

namespace Sindy.View.Features
{
    /// <summary>색상 출력 능력. <see cref="FeatureViews.ColorFeatureView"/>와 1:1 대칭.</summary>
    public class ColorFeature : ModelFeature
    {
        public PropModel<Color> Color { get; }

        public ColorFeature() : this(UnityEngine.Color.white) { }

        public ColorFeature(Color color)
        {
            Color = new PropModel<Color>(color);
            Color.AddTo(this);
        }

        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public ColorFeature(PropModel<Color> external)
        {
            Color = external ?? throw new System.ArgumentNullException(nameof(external));
            Color.AddTo(this);
        }
    }
}
