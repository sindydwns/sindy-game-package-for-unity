using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.View.Components
{
    public class ListViewModel : ViewModel
    {
        private readonly ReactiveProperty<IReadOnlyList<IViewModel>> items = new(Array.Empty<IViewModel>());
        public ReadOnlyReactiveProperty<IReadOnlyList<IViewModel>> Items => items;

        public void SetItems(IReadOnlyList<IViewModel> list)
        {
            items.Value = list ?? Array.Empty<IViewModel>();
        }

        public override void Dispose()
        {
            base.Dispose();
            items.Dispose();
        }
    }

    public class ListViewModel<T> : ListViewModel where T : IViewModel
    {
        public void SetItems(IReadOnlyList<T> list)
        {
            if (list == null) { base.SetItems(null); return; }
            var converted = new IViewModel[list.Count];
            for (int i = 0; i < list.Count; i++) converted[i] = list[i];
            base.SetItems(converted);
        }
    }

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
            if (isDestroying) { pool.Clear(); return; }
            foreach (var item in pool)
                if (item != null) Destroy(item.gameObject);
            pool.Clear();
        }

        private void Refresh(IReadOnlyList<IViewModel> items)
        {
            while (pool.Count < items.Count)
                pool.Add(Instantiate(prefab, container));

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
