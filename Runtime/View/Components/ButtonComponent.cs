using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Components
{
    public class ButtonComponent : SindyComponent<SubjModel<Unit>>
    {
        [SerializeField] private Button button;

        protected override void Init(SubjModel<Unit> model)
        {
            void OnClick() => model.OnNext(Unit.Default);
            button.onClick.AddListener(OnClick);
            disposables.Add(Disposable.Create(() => button.onClick.RemoveListener(OnClick)));
        }
    }

    public class ButtonModel : SubjModel<Unit>
    {
    }
}
