using System.Collections.Generic;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class PageComponent : SindyComponent<PropModel<int>>
    {
        [SerializeField] private List<GameObject> pages;

        protected override void Init(PropModel<int> model)
        {
            model.Prop.Subscribe(Refresh).AddTo(disposables);
        }

        private void Refresh(int index)
        {
            for (int i = 0; i < pages.Count; i++)
                pages[i].SetActive(i == index);
        }
    }

    public class PageModel : PropModel<int>
    {
        public PageModel() { }
        public PageModel(int pageIndex) : base(pageIndex) { }
    }
}
