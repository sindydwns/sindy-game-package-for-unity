using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class DisposibleTests
    {
        [Test] public void Disposible() { using var t = new TestCaseDisposible(); t.Run(); }
    }
}
