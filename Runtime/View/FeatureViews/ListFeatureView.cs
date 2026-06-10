using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// <see cref="ListFeature"/>의 아이템 목록을 prefab 인스턴스로 펼친다 (비가상화).
    /// 인스턴스는 풀로 재사용되며, 각 아이템 ViewModel은 인스턴스의 SindyComponent(허브)에 바인딩된다.
    /// 대량 데이터/가상화가 필요하면 ScrollerFeatureView를 사용할 것.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/List Feature View")]
    public class ListFeatureView : FeatureView<ListFeature>
    {
        [SerializeField] private SindyComponent prefab;
        [SerializeField] private Transform container;

        private readonly List<SindyComponent> pool = new();
        private bool isDestroying;

        protected override void Bind(ListFeature feature, ICollection<IDisposable> disposables)
        {
            feature.Items.Subscribe(Refresh).AddTo(disposables);
        }

        protected override void Clear()
        {
            if (isDestroying)
            {
                pool.Clear();
                return;
            }

            foreach (var item in pool)
            {
                if (item != null)
                {
                    item.Bind(null);
                    Destroy(item.gameObject);
                }
            }
            pool.Clear();
        }

        protected override void OnDestroy()
        {
            isDestroying = true;
            base.OnDestroy();
        }

        private void Refresh(IReadOnlyList<IViewModel> items)
        {
            while (pool.Count < items.Count)
            {
                pool.Add(Instantiate(prefab, container != null ? container : transform));
            }

            for (int i = 0; i < pool.Count; i++)
            {
                if (i < items.Count)
                {
                    pool[i].gameObject.SetActive(true);
                    pool[i].Bind(items[i]);
                }
                else
                {
                    pool[i].Bind(null);
                    pool[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
