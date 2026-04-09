using TMPro;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    /// <summary>
    /// PropModel<string> 기반 포맷 텍스트 표시 컴포넌트.
    /// StringPropModel과 FormatNumberPropModel<T> 모두 수용합니다.
    /// </summary>
    public class FormatLabelComponent : SindyComponent<PropModel<string>>
    {
        [SerializeField] private TMP_Text label;

        protected override void Init(PropModel<string> model)
        {
            model.Prop.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }

    /// <summary>
    /// FormatLabelComponent 전용 모델.
    /// FormatNumberPropModel<T>를 사용할 경우 직접 전달도 가능합니다.
    /// </summary>
    public class FormatLabelModel : PropModel<string>
    {
        public ReactiveProperty<string> Text => Prop;

        public FormatLabelModel() { }
        public FormatLabelModel(string text) : base(text) { }
    }
}
