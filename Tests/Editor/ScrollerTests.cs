using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class ScrollerTests
    {
        [Test] public void GridLayoutResolver() { using var t = new TestGridLayoutResolver(); t.Run(); }
    }
}
