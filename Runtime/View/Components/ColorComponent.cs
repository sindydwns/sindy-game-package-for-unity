using Sindy.View.Model;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class ColorComponent : SindyComponent<ColorPropModel>
    {
        [SerializeField] private Graphic target;

        protected override void Init(ColorPropModel model)
        {
            model.Color.Subscribe(v => target.color = v).AddTo(disposables);
        }
    }

    public class ColorModel : ColorPropModel
    {
        public ColorModel() { }
        public ColorModel(Color color) : base(color) { }
    }
}
