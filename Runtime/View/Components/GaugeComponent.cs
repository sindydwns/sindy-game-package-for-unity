using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class GaugeComponent : SindyComponent<PropModel<float>>
    {
        [SerializeField] private Image fill;

        protected override void Init(PropModel<float> model)
        {
            model.Subscribe(v => fill.fillAmount = Mathf.Clamp01(v)).AddTo(disposables);
        }
    }

    public class GaugeModel : PropModel<float>
    {
        public GaugeModel() { }
        public GaugeModel(float value) : base(value) { }
    }
}
