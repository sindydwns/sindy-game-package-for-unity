using R3;
using Sindy.View.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Components
{
    public class ToggleComponent : SindyComponent<BoolPropModel>
    {
        [SerializeField] private Toggle toggle;

        protected override void Init(BoolPropModel model)
        {
            model.Show
                .Subscribe(v => toggle.SetIsOnWithoutNotify(v))
                .AddTo(disposables);

            void OnValueChanged(bool v) => model.Show.Value = v;
            toggle.onValueChanged.AddListener(OnValueChanged);
            disposables.Add(Disposable.Create(() => toggle.onValueChanged.RemoveListener(OnValueChanged)));
        }
    }

    public class ToggleModel : BoolPropModel
    {
        public ToggleModel() { }
        public ToggleModel(bool isOn) : base(isOn) { }
    }
}
