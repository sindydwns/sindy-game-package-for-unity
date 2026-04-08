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

        public TestListComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var list = new ListViewModel();

            list.Items
                .Subscribe(items => Debug.Log($"[List] itemCount = {items.Count}"))
                .AddTo(disposables);

            component.SetModel(list);

            // 아이템 3개로 시작
            list.SetItems(new List<IViewModel>
            {
                new StringPropModel("Item A"),
                new StringPropModel("Item B"),
                new StringPropModel("Item C"),
            });

            // 아이템 1개로 축소 — 나머지 풀 항목이 비활성화되는지 확인
            list.SetItems(new List<IViewModel>
            {
                new StringPropModel("Item A (reduced)"),
            });

            // 빈 목록
            list.SetItems(null);
        }
    }
}
