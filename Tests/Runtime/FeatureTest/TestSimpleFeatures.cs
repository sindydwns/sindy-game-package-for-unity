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

        // VisibilityFeature 기본 생성 시 Show가 true인지 확인
        private void VisibilityDefaultTrue()
        {
            var feature = new VisibilityFeature();
            Assert.IsTrue(feature.Show.Value);
            feature.Dispose();
        }

        // VisibilityFeature의 Show 값을 토글할 수 있는지 확인
        private void VisibilityToggle()
        {
            var feature = new VisibilityFeature(false);
            Assert.IsFalse(feature.Show.Value);

            feature.Show.Value = true;
            Assert.IsTrue(feature.Show.Value);

            feature.Dispose();
        }

        // VisibilityFeature의 Show를 구독하여 값 변경이 콜백으로 전달되는지 확인
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

        // HighlightFeature 기본 생성 시 Highlight가 false인지 확인
        private void HighlightDefaultFalse()
        {
            var feature = new HighlightFeature();
            Assert.IsFalse(feature.Highlight.Value);
            feature.Dispose();
        }

        // HighlightFeature의 Highlight 값을 토글할 수 있는지 확인
        private void HighlightToggle()
        {
            var feature = new HighlightFeature(false);

            feature.Highlight.Value = true;
            Assert.IsTrue(feature.Highlight.Value);

            feature.Highlight.Value = false;
            Assert.IsFalse(feature.Highlight.Value);

            feature.Dispose();
        }

        // HighlightFeature의 Highlight를 구독하여 값 변경이 콜백으로 전달되는지 확인
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

        // RaycastBlockFeature 기본 생성 시 IgnoreRaycast가 false인지 확인
        private void RaycastBlockDefaultFalse()
        {
            var feature = new RaycastBlockFeature();
            Assert.IsFalse(feature.IgnoreRaycast.Value);
            feature.Dispose();
        }

        // RaycastBlockFeature의 IgnoreRaycast 값을 토글할 수 있는지 확인
        private void RaycastBlockToggle()
        {
            var feature = new RaycastBlockFeature(false);

            feature.IgnoreRaycast.Value = true;
            Assert.IsTrue(feature.IgnoreRaycast.Value);

            feature.IgnoreRaycast.Value = false;
            Assert.IsFalse(feature.IgnoreRaycast.Value);

            feature.Dispose();
        }

        // RaycastBlockFeature의 IgnoreRaycast를 구독하여 값 변경이 콜백으로 전달되는지 확인
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

        // 모든 Feature를 Dispose한 뒤 IsDisposed가 true로 설정되는지 확인
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
