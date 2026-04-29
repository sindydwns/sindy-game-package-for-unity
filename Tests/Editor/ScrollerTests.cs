using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class ScrollerTests
    {
        [Test] public void GridLayoutResolver() { using var t = new TestGridLayoutResolver(); t.Run(); }
        [Test] public void Section() { using var t = new TestSection(); t.Run(); }
    }
}
