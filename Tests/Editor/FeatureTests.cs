using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class FeatureTests
    {
        [Test] public void FeatureWith() { using var t = new TestFeatureWith(); t.Run(); }
        [Test] public void InteractableFeature() { using var t = new TestInteractableFeature(); t.Run(); }
        [Test] public void ButtonFeature() { using var t = new TestButtonFeature(); t.Run(); }
        [Test] public void SimpleFeatures() { using var t = new TestSimpleFeatures(); t.Run(); }
        [Test] public void ScreenFeature() { using var t = new TestScreenFeature(); t.Run(); }
    }
}
