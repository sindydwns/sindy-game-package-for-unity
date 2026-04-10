using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class ViewModelTests
    {
        [Test] public void FormatNumberPropModel() { using var t = new TestFormatNumberPropModel(); t.Run(); }
        [Test] public void ViewModelCore() { using var t = new TestViewModelCore(); t.Run(); }
    }
}
