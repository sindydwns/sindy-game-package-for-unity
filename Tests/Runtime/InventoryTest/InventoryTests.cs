using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class InventoryTests
    {
        [Test] public void InventoryCrud() { using var t = new TestInventoryCrud(); t.Run(); }
        [Test] public void InventoryEvent() { using var t = new TestInventoryEvent(); t.Run(); }
        [Test] public void InventoryMoveTo() { using var t = new TestInventoryMoveTo(); t.Run(); }
        [Test] public void InventorySetOps() { using var t = new TestInventorySetOps(); t.Run(); }
        [Test] public void InventorySerialize() { using var t = new TestInventorySerialize(); t.Run(); }
    }
}
