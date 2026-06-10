using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary><see cref="PageFeature"/>의 인덱스에 해당하는 페이지만 활성화한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Page Feature View")]
    public class PageFeatureView : FeatureView<PageFeature>
    {
        [SerializeField] private List<GameObject> pages;

        protected override void Bind(PageFeature feature, ICollection<IDisposable> disposables)
        {
            feature.PageIndex.Subscribe(index =>
            {
                for (int i = 0; i < pages.Count; i++)
                {
                    pages[i].SetActive(i == index);
                }
            }).AddTo(disposables);
        }
    }
}
