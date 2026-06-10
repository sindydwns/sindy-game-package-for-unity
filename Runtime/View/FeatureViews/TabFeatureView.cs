using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.FeatureViews
{
    /// <summary>Toggle 리스트와 <see cref="TabFeature"/>의 선택 인덱스를 양방향 바인딩한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Tab Feature View")]
    public class TabFeatureView : FeatureView<TabFeature>
    {
        [SerializeField] private List<Toggle> tabs;

        protected override void Bind(TabFeature feature, ICollection<IDisposable> disposables)
        {
            feature.SelectedIndex.Subscribe(index =>
            {
                for (int i = 0; i < tabs.Count; i++)
                {
                    tabs[i].SetIsOnWithoutNotify(i == index);
                }
            }).AddTo(disposables);

            for (int i = 0; i < tabs.Count; i++)
            {
                int capturedIndex = i;
                void OnValueChanged(bool isOn)
                {
                    if (isOn) feature.SelectedIndex.Value = capturedIndex;
                }
                tabs[capturedIndex].onValueChanged.AddListener(OnValueChanged);
                disposables.Add(Disposable.Create(() => tabs[capturedIndex].onValueChanged.RemoveListener(OnValueChanged)));
            }
        }
    }
}
