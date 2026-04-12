using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class RedDotTests
    {
        [Test] public void RedDotDefaultWork() { using var t = new TestRedDotDefaultWork(); t.Run(); }
    }
}
