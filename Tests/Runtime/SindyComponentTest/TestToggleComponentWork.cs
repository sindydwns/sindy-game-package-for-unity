using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ToggleComponent — 양방향 바인딩 테스트
    /// - 모델 변경 → UI 반영 (SetIsOnWithoutNotify)
    /// - UI 클릭 → 모델 변경 (씬에서 직접 Toggle 클릭으로 확인)
    /// </summary>
    class TestToggleComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private PropModel<bool> model;

        public TestToggleComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            model = new PropModel<bool>(false);
            component.SetModel(model);

            Assert.IsTrue(component.IsInitialized);
            Assert.AreEqual(false, model.Value);

            model.Value = true;
            Assert.AreEqual(true, model.Value);

            model.Value = false;
            Assert.AreEqual(false, model.Value);
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
