using Sindy.View.Features;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// VisibilityFeature, HighlightFeature, RaycastBlockFeature — 초기값, 토글, 구독, Dispose
    /// </summary>
    class TestSimpleFeatures : TestCase
    {
        public override void Run()
        {
            VisibilityDefaultTrue();
            VisibilityToggle();
            VisibilitySubscribe();
            HighlightDefaultFalse();
            HighlightToggle();
            HighlightSubscribe();
            RaycastBlockDefaultFalse();
            RaycastBlockToggle();
            RaycastBlockSubscribe();
            AllDisposeCleanly();
        }

        // ── VisibilityFeature ──

        private void VisibilityDefaultTrue()
        {
            var feature = new VisibilityFeature();
            Assert.IsTrue(feature.Show.Value);
            feature.Dispose();
        }

        private void VisibilityToggle()
        {
            var feature = new VisibilityFeature(false);
            Assert.IsFalse(feature.Show.Value);

            feature.Show.Value = true;
            Assert.IsTrue(feature.Show.Value);

            feature.Dispose();
        }

        private void VisibilitySubscribe()
        {
            var feature = new VisibilityFeature(true);
            bool lastValue = true;
            feature.Show.Subscribe(v => lastValue = v).AddTo(disposables);

            feature.Show.Value = false;
            Assert.IsFalse(lastValue);

            feature.Dispose();
        }

        // ── HighlightFeature ──

        private void HighlightDefaultFalse()
        {
            var feature = new HighlightFeature();
            Assert.IsFalse(feature.Highlight.Value);
            feature.Dispose();
        }

        private void HighlightToggle()
        {
            var feature = new HighlightFeature(false);

            feature.Highlight.Value = true;
            Assert.IsTrue(feature.Highlight.Value);

            feature.Highlight.Value = false;
            Assert.IsFalse(feature.Highlight.Value);

            feature.Dispose();
        }

        private void HighlightSubscribe()
        {
            var feature = new HighlightFeature();
            bool lastValue = false;
            feature.Highlight.Subscribe(v => lastValue = v).AddTo(disposables);

            feature.Highlight.Value = true;
            Assert.IsTrue(lastValue);

            feature.Dispose();
        }

        // ── RaycastBlockFeature ──

        private void RaycastBlockDefaultFalse()
        {
            var feature = new RaycastBlockFeature();
            Assert.IsFalse(feature.IgnoreRaycast.Value);
            feature.Dispose();
        }

        private void RaycastBlockToggle()
        {
            var feature = new RaycastBlockFeature(false);

            feature.IgnoreRaycast.Value = true;
            Assert.IsTrue(feature.IgnoreRaycast.Value);

            feature.IgnoreRaycast.Value = false;
            Assert.IsFalse(feature.IgnoreRaycast.Value);

            feature.Dispose();
        }

        private void RaycastBlockSubscribe()
        {
            var feature = new RaycastBlockFeature();
            bool lastValue = false;
            feature.IgnoreRaycast.Subscribe(v => lastValue = v).AddTo(disposables);

            feature.IgnoreRaycast.Value = true;
            Assert.IsTrue(lastValue);

            feature.Dispose();
        }

        // ── 공통 Dispose ──

        private void AllDisposeCleanly()
        {
            var vis = new VisibilityFeature();
            var hl = new HighlightFeature();
            var rc = new RaycastBlockFeature();

            vis.Dispose();
            hl.Dispose();
            rc.Dispose();

            Assert.IsTrue(vis.IsDisposed);
            Assert.IsTrue(hl.IsDisposed);
            Assert.IsTrue(rc.IsDisposed);
        }
    }
}
