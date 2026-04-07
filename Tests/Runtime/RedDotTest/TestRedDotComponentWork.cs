using Sindy.RedDot;
using Sindy.View;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Sindy.Test
{
    class TestRedDotComponentWork : TestCase
    {
        private readonly SindyComponent _component;

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
            // _component.SetModel(new RedDotModel("inven.new_item.copper_bar", true));
        }
    }
}
