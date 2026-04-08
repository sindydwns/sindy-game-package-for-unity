using Sindy.View.Model;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Sindy.View.Components
{
    public class GaugeComponent : SindyComponent<FloatPropModel>
    {
        [SerializeField] private Image fill;

        protected override void Init(FloatPropModel model)
        {
            model.Number.Subscribe(v => fill.fillAmount = Mathf.Clamp01(v)).AddTo(disposables);
        }
    }
}
