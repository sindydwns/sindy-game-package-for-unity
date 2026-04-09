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
        private IntPropModel model;

        public TestPageComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new IntPropModel(0);
            model.Number.Subscribe(v => Debug.Log($"[Page] currentPage = {v}")).AddTo(disposables);

            component.SetModel(model);

            model.Value = 1;
            model.Value = 2;
            model.Value = 0;
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
