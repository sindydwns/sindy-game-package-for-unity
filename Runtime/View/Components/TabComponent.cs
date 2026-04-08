using System.Collections.Generic;
using R3;
using Sindy.View.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Components
{
    public class TabComponent : SindyComponent<IntPropModel>
    {
        [SerializeField] private List<Toggle> tabs;

        protected override void Init(IntPropModel model)
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
                void OnValueChanged(bool isOn)
                {
                    if (isOn) model.Value = capturedIndex;
                }
                tabs[i].onValueChanged.AddListener(OnValueChanged);
                int closedIndex = capturedIndex;
                disposables.Add(Disposable.Create(() => tabs[closedIndex].onValueChanged.RemoveListener(OnValueChanged)));
            }
        }
    }
}
