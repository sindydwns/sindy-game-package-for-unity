using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// PageComponent — IntPropModel 인덱스 변경 시 해당 페이지만 활성화되는지 확인
    /// Inspector에서 PageComponent의 pages 리스트에 GameObject들을 등록해야 함
    /// </summary>
    class TestPageComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestPageComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var page = new IntPropModel(0);

            page.Number
                .Subscribe(v => Debug.Log($"[Page] currentPage = {v}"))
                .AddTo(disposables);

            component.SetModel(page);

            page.Value = 1;
            page.Value = 2;
            page.Value = 0;
        }
    }
}
