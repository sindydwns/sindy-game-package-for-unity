using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// LabelComponent — PropModel&lt;string&gt; 변경 시 TMP_Text에 반영되는지 확인
    /// </summary>
    class TestLabelComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private PropModel<string> model;

        public TestLabelComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new PropModel<string>("Hello, World!");
            component.SetModel(model);

            Assert.IsTrue(component.IsInitialized);
            Assert.AreEqual("Hello, World!", model.Value);

            model.Value = "Changed Text";
            Assert.AreEqual("Changed Text", model.Value);

            model.Value = "Final Text";
            Assert.AreEqual("Final Text", model.Value);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
