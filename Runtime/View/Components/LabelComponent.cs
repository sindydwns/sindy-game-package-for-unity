using Sindy.View.Model;
using TMPro;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class LabelComponent : SindyComponent<StringPropModel>
    {
        [SerializeField] private TMP_Text label;

        protected override void Init(StringPropModel model)
        {
            model.Text.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }

    public class LabelModel : StringPropModel
    {
        public LabelModel() { }
        public LabelModel(string text) : base(text) { }
    }
}
