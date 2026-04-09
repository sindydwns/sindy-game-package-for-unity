using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class ColorComponent : SindyComponent<PropModel<Color>>
    {
        [SerializeField] private Graphic target;

        protected override void Init(PropModel<Color> model)
        {
            model.Subscribe(v => target.color = v).AddTo(disposables);
        }
    }

    public class ColorModel : PropModel<Color>
    {
        public ColorModel() { }
        public ColorModel(Color color) : base(color) { }
    }
}
