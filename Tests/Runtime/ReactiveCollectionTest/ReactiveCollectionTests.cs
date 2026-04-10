using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class ReactiveCollectionTests
    {
        [Test] public void ReactiveList() { using var t = new TestReactiveList(); t.Run(); }
        [Test] public void ReactiveSet() { using var t = new TestReactiveSet(); t.Run(); }
        [Test] public void ReactiveDictionary() { using var t = new TestReactiveDictionary(); t.Run(); }
        [Test] public void ReactiveListCondition() { using var t = new TestReactiveListCondition(); t.Run(); }
    }
}
