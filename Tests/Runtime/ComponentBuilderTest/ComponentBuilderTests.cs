using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class ComponentBuilderTests
    {
        [Test] public void ComponentBuilder() { using var t = new TestComponentBuilder(); t.Run(); }
    }
}
