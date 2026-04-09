using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class IconComponent : SindyComponent<PropModel<Sprite>>
    {
        [SerializeField] private Image image;

        protected override void Init(PropModel<Sprite> model)
        {
            model.Subscribe(v => image.sprite = v).AddTo(disposables);
        }
    }

    public class IconModel : PropModel<Sprite>
    {
        public IconModel() { }
        public IconModel(Sprite sprite) : base(sprite) { }
    }
}
