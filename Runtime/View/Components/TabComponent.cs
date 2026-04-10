using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Components
{
    public class TabComponent : SindyComponent<PropModel<int>>
    {
        [SerializeField] private List<Toggle> tabs;

        protected override void Init(PropModel<int> model)
        {
            model.Prop
                .Subscribe(index =>
                {
                    for (int i = 0; i < tabs.Count; i++)
                        tabs[i].SetIsOnWithoutNotify(i == index);
                })
                .AddTo(disposables);

            for (int i = 0; i < tabs.Count; i++)
            {
                int capturedIndex = i;
                BindUnityEvent<bool>(tabs[i].onValueChanged, isOn => { if (isOn) model.Value = capturedIndex; });
            }
        }
    }

    public class TabModel : PropModel<int>
    {
        public TabModel() { }
        public TabModel(int selectedIndex) : base(selectedIndex) { }
    }
}
