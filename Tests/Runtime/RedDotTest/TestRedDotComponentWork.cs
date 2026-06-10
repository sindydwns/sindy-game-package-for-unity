using Sindy.RedDot;
using Sindy.View;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Sindy.Test
{
    /// <summary>
    /// RedDotFeature — RedDotNode 트리 카운트가 Feature.Count로 전파되는지,
    /// 허브 Bind를 통해 RedDotFeatureView와 연동 가능한 형태인지 검증한다.
    /// </summary>
    class TestRedDotComponentWork : TestCase
    {
        private readonly SindyComponent _component;
        private ViewModel _model;
        private RedDotFeature _feature;

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
            _feature = new RedDotFeature("inven.new_item.copper_bar", isLeaf: true);
            _model = new ViewModel().With(_feature);
            _component.Bind(_model);

            Assert.AreEqual(_model, _component.CurrentModel);

            // leaf 노드에 값 설정 후 Feature에 반영되는지 확인
            var leaf = RedDotNode.Root.GetLeaf("inven.new_item.copper_bar");
            Assert.IsNotNull(leaf);

            leaf.Count.Value = 3;
            Assert.AreEqual(3, _feature.Count.Value);

            leaf.Clear();
            Assert.AreEqual(0, _feature.Count.Value);
        }

        protected override void Cleanup()
        {
            _component?.Bind(null);
            _model?.Dispose();
        }
    }
}
