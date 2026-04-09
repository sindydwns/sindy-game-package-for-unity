using TMPro;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class LabelComponent : SindyComponent<PropModel<string>>
    {
        [SerializeField] private TMP_Text label;

        protected override void Init(PropModel<string> model)
        {
            model.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }

    public class LabelModel : PropModel<string>
    {
        public LabelModel() { }
        public LabelModel(string text) : base(text) { }
    }
}
