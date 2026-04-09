using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// LabelComponent — StringPropModel.Text 변경 시 TMP_Text에 반영되는지 확인
    /// </summary>
    class TestLabelComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private StringPropModel model;

        public TestLabelComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new StringPropModel("Hello, World!");
            model.Text.Subscribe(v => Debug.Log($"[Label] text = \"{v}\"")).AddTo(disposables);

            component.SetModel(model);

            model.Value = "Changed Text";
            model.Value = "Final Text";
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
