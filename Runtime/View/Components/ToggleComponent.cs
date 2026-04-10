using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Components
{
    public class ToggleComponent : SindyComponent<PropModel<bool>>
    {
        [SerializeField] private Toggle toggle;

        protected override void Init(PropModel<bool> model)
        {
            model
                .Subscribe(v => toggle.SetIsOnWithoutNotify(v))
                .AddTo(disposables);

            BindUnityEvent<bool>(toggle.onValueChanged, v => model.Value = v);
        }
    }

    public class ToggleModel : PropModel<bool>
    {
        public ToggleModel() { }
        public ToggleModel(bool isOn) : base(isOn) { }
    }
}
