using System.Collections.Generic;
using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// ListComponent — ListViewModel 아이템 목록 변경 시 풀에서 컴포넌트가 활성/비활성되는지 확인
    /// Inspector에서 ListComponent의 prefab과 container를 반드시 연결해야 함
    /// </summary>
    class TestListComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private ListViewModel model;

        public TestListComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new ListViewModel();
            model.Items.Subscribe(items => Debug.Log($"[List] itemCount = {items.Count}")).AddTo(disposables);

            component.SetModel(model);

            model.SetItems(new List<IViewModel>
            {
                new StringPropModel("Item A"),
                new StringPropModel("Item B"),
                new StringPropModel("Item C"),
            });

            model.SetItems(new List<IViewModel>
            {
                new StringPropModel("Item A (reduced)"),
            });

            model.SetItems(null);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
