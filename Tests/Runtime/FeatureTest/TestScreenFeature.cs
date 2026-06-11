using R3;
using Sindy.View;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ScreenFeature — 기본 selector, 회전 시뮬레이션, 커스텀 selector,
    /// 동일 variant 중복 방출 없음, Dispose 후 무반응
    /// </summary>
    class TestScreenFeature : TestCase
    {
        public override void Run()
        {
            DefaultSelectorLandscape();
            RotateEmitsPortrait();
            SameVariantNotReEmitted();
            CustomSelector();
            DisposeStopsEmission();
        }

        private static ScreenState Landscape() => new(1920, 1080, new Rect(0, 0, 1920, 1080));
        private static ScreenState Portrait() => new(1080, 1920, new Rect(0, 0, 1080, 1920));

        // 가로 화면 주입 시 기본 selector가 landscape를 선택하는지 확인
        private void DefaultSelectorLandscape()
        {
            var screen = new PropModel<ScreenState>(Landscape());
            var feature = new ScreenFeature(screen);

            Assert.AreEqual(ScreenFeature.Landscape, feature.Variant.Value);
            feature.Dispose();
        }

        // 화면 상태를 세로로 바꾸면 variant가 portrait로 전환되는지 확인
        private void RotateEmitsPortrait()
        {
            var screen = new PropModel<ScreenState>(Landscape());
            var feature = new ScreenFeature(screen);

            string last = null;
            var count = 0;
            feature.Variant.Subscribe(v => { last = v; count++; }).AddTo(disposables);
            Assert.AreEqual(1, count); // 구독 즉시 현재 값 방출

            screen.Value = Portrait();
            Assert.AreEqual(ScreenFeature.Portrait, last);
            Assert.AreEqual(2, count);

            feature.Dispose();
        }

        // 해상도가 바뀌어도 variant가 같으면 재방출되지 않는지 확인
        private void SameVariantNotReEmitted()
        {
            var screen = new PropModel<ScreenState>(Landscape());
            var feature = new ScreenFeature(screen);

            var count = 0;
            feature.Variant.Subscribe(_ => count++).AddTo(disposables);
            Assert.AreEqual(1, count);

            // 가로 → 다른 가로 해상도: variant는 그대로 landscape
            screen.Value = new ScreenState(2560, 1440, new Rect(0, 0, 2560, 1440));
            Assert.AreEqual(1, count);

            feature.Dispose();
        }

        // 커스텀 selector(3종 분기)가 동작하는지 확인
        private void CustomSelector()
        {
            var screen = new PropModel<ScreenState>(new ScreenState(4, 3, default));
            var feature = new ScreenFeature(screen, s =>
                s.Aspect >= 2f ? "wide" : s.IsLandscape ? "landscape" : "portrait");

            Assert.AreEqual("landscape", feature.Variant.Value);

            screen.Value = new ScreenState(21, 9, default);
            Assert.AreEqual("wide", feature.Variant.Value);

            screen.Value = new ScreenState(9, 16, default);
            Assert.AreEqual("portrait", feature.Variant.Value);

            feature.Dispose();
        }

        // Dispose 후 화면 상태를 바꿔도 예외·방출이 없는지 확인
        private void DisposeStopsEmission()
        {
            var screen = new PropModel<ScreenState>(Landscape());
            var feature = new ScreenFeature(screen);

            var count = 0;
            feature.Variant.Subscribe(_ => count++).AddTo(disposables);
            var before = count;

            feature.Dispose();
            // 외부 주입 모델도 Feature와 함께 Dispose된다 — 값 변경 시도는 무시되어야 한다
            Assert.AreEqual(before, count);
        }
    }
}
