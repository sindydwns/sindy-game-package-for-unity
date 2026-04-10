using Sindy.RedDot;
using Sindy.View;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Sindy.Test
{
    class TestRedDotComponentWork : TestCase
    {
        private readonly SindyComponent _component;
        private RedDotModel _model;

        public TestRedDotComponentWork(SindyComponent component) : base()
        {
            _component = component;
            component.GetComponent<Button>().onClick.AddListener(() =>
            {
                var leaf = RedDotNode.Root.GetLeaf("inven.new_item.iron_bar");
                leaf.Count.Value++;
                if (leaf.Count.CurrentValue > 5)
                {
                    leaf.Count.Value = 0;
                }

                var inven = RedDotNode.Root.GetNode("inven").Count.CurrentValue;
                var newItem = RedDotNode.Root.GetNode("inven.new_item").Count.CurrentValue;
                var ironBar = RedDotNode.Root.GetNode("inven.new_item.iron_bar").Count.CurrentValue;
                var copperBar = RedDotNode.Root.GetNode("inven.new_item.copper_bar").Count.CurrentValue;
                Assert.AreEqual(leaf.Count.Value == 0 ? 0 : 1, inven);
                Assert.AreEqual(leaf.Count.Value == 0 ? 0 : 1, newItem);
                Assert.AreEqual(leaf.Count.CurrentValue, ironBar);
                Assert.AreEqual(0, copperBar);
            });
        }

        public override void Run()
        {
            _model = new RedDotModel("inven.new_item.copper_bar", true);
            _component.SetModel(_model);

            Assert.IsTrue(_component.IsInitialized);

            // leaf 노드에 값 설정 후 모델에 반영되는지 확인
            var leaf = RedDotNode.Root.GetLeaf("inven.new_item.copper_bar");
            Assert.IsNotNull(leaf);

            leaf.Count.Value = 3;
            Assert.AreEqual(3, _model.Value);

            leaf.Clear();
            Assert.AreEqual(0, _model.Value);
        }

        protected override void Cleanup()
        {
            _component?.SetModel(null);
            _model?.Dispose();
        }
    }
}
