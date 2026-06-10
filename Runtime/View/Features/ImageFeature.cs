using UnityEngine;

namespace Sindy.View.Features
{
    /// <summary>스프라이트 출력 능력. <see cref="FeatureViews.ImageFeatureView"/>와 1:1 대칭.</summary>
    public class ImageFeature : ModelFeature
    {
        public PropModel<Sprite> Sprite { get; }

        public ImageFeature(Sprite sprite = null)
        {
            Sprite = new PropModel<Sprite>(sprite);
            Sprite.AddTo(this);
        }

        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public ImageFeature(PropModel<Sprite> external)
        {
            Sprite = external ?? throw new System.ArgumentNullException(nameof(external));
            Sprite.AddTo(this);
        }
    }
}
