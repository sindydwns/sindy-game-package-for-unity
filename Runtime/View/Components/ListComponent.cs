using System.Collections.Generic;
using Sindy.View.Model;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class ListModel : ListViewModel { }

    public class ListComponent : SindyComponent<ListViewModel>
    {
        [SerializeField] private SindyComponent prefab;
        [SerializeField] private Transform container;

        private readonly List<SindyComponent> pool = new();
        private bool isDestroying;

        protected override void OnDestroy()
        {
            isDestroying = true;
            base.OnDestroy();
        }

        protected override void Init(ListViewModel model)
        {
            model.Items.Subscribe(Refresh).AddTo(disposables);
        }

        protected override void Clear(ListViewModel model)
        {
            if (isDestroying)
            {
                pool.Clear();
                return;
            }

            foreach (var item in pool)
            {
                if (item != null) Destroy(item.gameObject);
            }
            pool.Clear();
        }

        private void Refresh(IReadOnlyList<IViewModel> items)
        {
            while (pool.Count < items.Count)
            {
                pool.Add(Instantiate(prefab, container));
            }

            for (int i = 0; i < pool.Count; i++)
            {
                if (i < items.Count)
                {
                    pool[i].gameObject.SetActive(true);
                    pool[i].SetModel(items[i]);
                }
                else
                {
                    pool[i].gameObject.SetActive(false);
                    pool[i].SetModel(null);
                }
            }
        }
    }
}
