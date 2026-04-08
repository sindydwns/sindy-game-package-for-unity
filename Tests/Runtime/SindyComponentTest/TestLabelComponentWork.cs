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

        public TestLabelComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var label = new StringPropModel("Hello, World!");

            label.Text
                .Subscribe(v => Debug.Log($"[Label] text = \"{v}\""))
                .AddTo(disposables);

            component.SetModel(label);

            // 값 변경 — 컴포넌트에 즉시 반영되는지 콘솔에서 확인
            label.Value = "Changed Text";
            label.Value = "Final Text";
        }
    }
}
