using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// VisibilityComponent — BoolPropModel.Show 변경 시 GameObject 활성/비활성에 반영되는지 확인
    /// </summary>
    class TestVisibilityComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestVisibilityComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var visibility = new BoolPropModel(true);

            visibility.Show
                .Subscribe(v => Debug.Log($"[Visibility] visible = {v}"))
                .AddTo(disposables);

            component.SetModel(visibility);

            visibility.Value = false;
            visibility.Value = true;
        }
    }
}
