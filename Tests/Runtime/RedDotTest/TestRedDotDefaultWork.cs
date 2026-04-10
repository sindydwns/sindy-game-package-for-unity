using Sindy.RedDot;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    class TestRedDotDefaultWork : TestCase
    {

        public override void Run()
        {
            RedDotNode.Root.Reset();
            Case1();
            RedDotNode.Root.Reset();
            Case2();
        }

        /// <summary>
        /// 두 개 이상의 하위 노드를 가진 상위 노드에서, 하위 노드의 활성화 상태와 Count가 상위 노드에 올바르게 반영되는지 테스트.
        /// </summary>
        private void Case2()
        {
            // 노드 생성 및 초기 상태 확인
            var mainQuest = RedDotNode.Root.EnsureLeaf("quest.main");
            var sideQuest = RedDotNode.Root.EnsureLeaf("quest.side");
            var quest = RedDotNode.Root.GetBranch("quest");

            Assert.AreEqual("quest", quest.Name);
            Assert.AreEqual("quest", quest.Path);
            Assert.AreEqual(0, quest.Count.CurrentValue);
            Assert.AreEqual(false, quest.IsActive.CurrentValue);

            Assert.AreEqual("main", mainQuest.Name);
            Assert.AreEqual("quest.main", mainQuest.Path);
            Assert.AreEqual(0, mainQuest.Count.CurrentValue);
            Assert.AreEqual(false, mainQuest.IsActive.CurrentValue);

            Assert.AreEqual("side", sideQuest.Name);
            Assert.AreEqual("quest.side", sideQuest.Path);
            Assert.AreEqual(0, sideQuest.Count.CurrentValue);
            Assert.AreEqual(false, sideQuest.IsActive.CurrentValue);

            // 하위 노드의 Count 변경이 상위 노드에 반영되는지 확인
            mainQuest.Count.Value = 2;
            sideQuest.Count.Value = 3;
            Assert.AreEqual(2, quest.Count.CurrentValue);
            Assert.AreEqual(true, quest.IsActive.CurrentValue);

            // UseActiveCount이 true일 때는 하위 노드의 활성화 여부에 따라 상위 노드의 Count가 결정되는지 확인
            quest.UseActiveCount.Value = false;
            Assert.AreEqual(5, quest.Count.CurrentValue);
            Assert.AreEqual(true, quest.IsActive.CurrentValue);

            // 하위 노드 초기화 후 상태 확인1
            mainQuest.Clear();
            Assert.AreEqual(3, quest.Count.CurrentValue);
            Assert.AreEqual(true, quest.IsActive.CurrentValue);

            // 하위 노드 초기화 후 상태 확인2
            sideQuest.Clear();
            Assert.AreEqual(0, quest.Count.CurrentValue);
            Assert.AreEqual(false, quest.IsActive.CurrentValue);
        }

        /// <summary>
        /// 상위 노드와 하위 노드의 Count가 연동되는지, IsActive가 Count에 따라 올바르게 변하는지 테스트.
        /// </summary>
        private void Case1()
        {
            // 노드 생성 및 초기 상태 확인
            var sword = RedDotNode.Root.EnsureLeaf("inventory.new_item.sword");
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(0, sword.Count.CurrentValue);
            Assert.AreEqual(false, sword.IsActive.CurrentValue);

            // 상위 노드가 자동으로 생성되고 초기 상태 확인
            var newItem = RedDotNode.Root.GetBranch("inventory.new_item");
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(0, newItem.Count.CurrentValue);
            Assert.AreEqual(false, newItem.IsActive.CurrentValue);

            var inventory = RedDotNode.Root.GetBranch("inventory");
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(0, inventory.Count.CurrentValue);
            Assert.AreEqual(false, inventory.IsActive.CurrentValue);

            // 하위 노드의 Count 변경이 상위 노드에 반영되는지 확인
            sword.Count.Value = 5;
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(5, sword.Count.CurrentValue);
            Assert.AreEqual(true, sword.IsActive.CurrentValue);
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(1, newItem.Count.CurrentValue);
            Assert.AreEqual(true, newItem.IsActive.CurrentValue);
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(1, inventory.Count.CurrentValue);
            Assert.AreEqual(true, inventory.IsActive.CurrentValue);

            // UseActiveCount이 true일 때는 하위 노드의 활성화 여부에 따라 상위 노드의 Count가 결정되는지 확인
            newItem.UseActiveCount.Value = false;
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(5, sword.Count.CurrentValue);
            Assert.AreEqual(true, sword.IsActive.CurrentValue);
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(5, newItem.Count.CurrentValue);
            Assert.AreEqual(true, newItem.IsActive.CurrentValue);
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(1, inventory.Count.CurrentValue);
            Assert.AreEqual(true, inventory.IsActive.CurrentValue);

            // 하위 노드 초기화 후 상태 확인
            sword.Clear();
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(0, sword.Count.CurrentValue);
            Assert.AreEqual(false, sword.IsActive.CurrentValue);
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(0, newItem.Count.CurrentValue);
            Assert.AreEqual(false, newItem.IsActive.CurrentValue);
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(0, inventory.Count.CurrentValue);
            Assert.AreEqual(false, inventory.IsActive.CurrentValue);

            // 하위 노드의 Count 변경이 상위 노드에 반영되는지 확인
            sword.Count.Value = 3;
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(3, sword.Count.CurrentValue);
            Assert.AreEqual(true, sword.IsActive.CurrentValue);
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(3, newItem.Count.CurrentValue);
            Assert.AreEqual(true, newItem.IsActive.CurrentValue);
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(1, inventory.Count.CurrentValue);
            Assert.AreEqual(true, inventory.IsActive.CurrentValue);

            // 상위 노드 초기화 후 상태 확인
            newItem.Clear();
            Assert.AreEqual("sword", sword.Name);
            Assert.AreEqual("inventory.new_item.sword", sword.Path);
            Assert.AreEqual(0, sword.Count.CurrentValue);
            Assert.AreEqual(false, sword.IsActive.CurrentValue);
            Assert.AreEqual("new_item", newItem.Name);
            Assert.AreEqual("inventory.new_item", newItem.Path);
            Assert.AreEqual(0, newItem.Count.CurrentValue);
            Assert.AreEqual(false, newItem.IsActive.CurrentValue);
            Assert.AreEqual("inventory", inventory.Name);
            Assert.AreEqual("inventory", inventory.Path);
            Assert.AreEqual(0, inventory.Count.CurrentValue);
            Assert.AreEqual(false, inventory.IsActive.CurrentValue);
        }
    }
}
