using System.Collections.Generic;
using Sindy.View.Model;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class PageComponent : SindyComponent<IntPropModel>
    {
        [SerializeField] private List<GameObject> pages;

        protected override void Init(IntPropModel model)
        {
            model.Prop.Subscribe(Refresh).AddTo(disposables);
        }

        private void Refresh(int index)
        {
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == index);
        }
    }
}
