using Sindy.View.Model;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class IconComponent : SindyComponent<SpritePropModel>
    {
        [SerializeField] private Image image;

        protected override void Init(SpritePropModel model)
        {
            model.Sprite.Subscribe(v => image.sprite = v).AddTo(disposables);
        }
    }

    public class IconModel : SpritePropModel
    {
        public IconModel() { }
        public IconModel(UnityEngine.Sprite sprite) : base(sprite) { }
    }
}
