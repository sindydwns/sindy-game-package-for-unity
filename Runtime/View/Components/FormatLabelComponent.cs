using TMPro;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    /// <summary>
    /// PropModel&lt;string&gt; 기반 포맷 텍스트 표시 컴포넌트.
    /// StringPropModel과 FormatNumberPropModel&lt;T&gt; 모두 수용합니다.
    /// </summary>
    public class FormatLabelComponent : SindyComponent<PropModel<string>>
    {
        [SerializeField] private TMP_Text label;

        protected override void Init(PropModel<string> model)
        {
            model.Prop.Subscribe(v => label.text = v).AddTo(disposables);
        }
    }
}
