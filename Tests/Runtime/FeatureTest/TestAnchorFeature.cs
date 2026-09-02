using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Sindy.Test
{
    /// <summary>
    /// AnchorFeature — 프리셋/정규화 사각형/인셋의 Apply 결과, 미지정 크기 유지, 부모 LayoutGroup 감지.
    /// </summary>
    class TestAnchorFeature : TestCase
    {
        private GameObject go;

        public override void Run()
        {
            CenterPresetSetsAnchorsAndSize();
            BottomSheetWithInset();
            TopStretchInsetPushesDown();
            StretchInsetShrinksBothAxes();
            NormalizedRectStretchesWithCenterPivot();
            UnspecifiedSizeKeepsPrefabSize();
            InsetOnlyKeepsPrefabAnchors();
            CloneIsIndependent();
            ParentLayoutGroupIsDetected();
        }

        private RectTransform NewRect(Vector2? size = null)
        {
            Cleanup();
            go = new GameObject("anchor_target", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = Vector2.zero; // uGUI 기본 풀스트레치가 아닌 상태에서 시작
            rect.sizeDelta = size ?? new Vector2(100, 50);
            return rect;
        }

        // Center + width/height: 점 고정 앵커, 크기 지정, 위치 0
        private void CenterPresetSetsAnchorsAndSize()
        {
            var rect = NewRect();
            var f = new AnchorFeature().Anchor(AnchorPreset.Center, 600, 400);
            f.Apply(rect);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMax);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot);
            Assert.AreEqual(new Vector2(600, 400), rect.sizeDelta);
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
            f.Dispose();
        }

        // 바텀시트: 가로 늘림(좌우 인셋만큼 줄어듦) + 세로 고정 높이, 바닥에서 인셋만큼 띄움
        private void BottomSheetWithInset()
        {
            var rect = NewRect();
            var f = new AnchorFeature().Anchor(AnchorPreset.BottomStretch, height: 300).Inset(left: 16, right: 16, bottom: 20);
            f.Apply(rect);
            Assert.AreEqual(new Vector2(0, 0), rect.anchorMin);
            Assert.AreEqual(new Vector2(1, 0), rect.anchorMax);
            Assert.AreEqual(new Vector2(0.5f, 0), rect.pivot);
            Assert.AreEqual(new Vector2(-32, 300), rect.sizeDelta);
            Assert.AreEqual(new Vector2(0, 20), rect.anchoredPosition);
            f.Dispose();
        }

        // 탑바: 피벗 y=1 → 상단 인셋은 음의 y 이동
        private void TopStretchInsetPushesDown()
        {
            var rect = NewRect();
            var f = new AnchorFeature().Anchor(AnchorPreset.TopStretch, height: 80).Inset(top: 10);
            f.Apply(rect);
            Assert.AreEqual(new Vector2(0.5f, 1), rect.pivot);
            Assert.AreEqual(new Vector2(0, 80), rect.sizeDelta);
            Assert.AreEqual(new Vector2(0, -10), rect.anchoredPosition);
            f.Dispose();
        }

        // 전체 페이지 + 비대칭 인셋: 크기는 양쪽 합만큼 줄고, 중심은 차이의 절반만큼 이동
        private void StretchInsetShrinksBothAxes()
        {
            var rect = NewRect();
            var f = new AnchorFeature().Anchor(AnchorPreset.Stretch).Inset(top: 10, right: 0, bottom: 30, left: 20);
            f.Apply(rect);
            Assert.AreEqual(new Vector2(-20, -40), rect.sizeDelta);
            Assert.AreEqual(new Vector2(10, 10), rect.anchoredPosition);
            f.Dispose();
        }

        // 정규화 사각형: 두 축 늘림, 피벗은 사각형 중심, 오프셋 0
        private void NormalizedRectStretchesWithCenterPivot()
        {
            var rect = NewRect();
            var f = new AnchorFeature().Anchor(new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f));
            f.Apply(rect);
            Assert.AreEqual(new Vector2(0.06f, 0.22f), rect.anchorMin);
            Assert.AreEqual(new Vector2(0.94f, 0.78f), rect.anchorMax);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot);
            Assert.AreEqual(Vector2.zero, rect.sizeDelta);
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
            f.Dispose();
        }

        // width/height 미지정(-1)이면 점 고정 축은 프리팹 크기를 유지한다
        private void UnspecifiedSizeKeepsPrefabSize()
        {
            var rect = NewRect(new Vector2(320, 180));
            var f = new AnchorFeature().Anchor(AnchorPreset.TopRight);
            f.Apply(rect);
            Assert.AreEqual(new Vector2(320, 180), rect.sizeDelta);
            Assert.AreEqual(new Vector2(1, 1), rect.pivot);
            f.Dispose();
        }

        // Inset만 선언하면 프리팹의 앵커·피벗은 그대로 두고 여백만 반영한다
        private void InsetOnlyKeepsPrefabAnchors()
        {
            var rect = NewRect();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
            var f = new AnchorFeature().Inset(8);
            f.Apply(rect);
            Assert.AreEqual(Vector2.zero, rect.anchorMin);
            Assert.AreEqual(Vector2.one, rect.anchorMax);
            Assert.AreEqual(new Vector2(-16, -16), rect.sizeDelta);
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition);
            f.Dispose();
        }

        // Clone은 값 전체를 복사한 독립 인스턴스다 (Blueprint가 Open마다 클론을 모델에 부착)
        private void CloneIsIndependent()
        {
            var original = new AnchorFeature().Anchor(AnchorPreset.BottomStretch, height: 200).Inset(bottom: 12);
            var clone = original.Clone();
            Assert.AreNotEqual(original, clone);
            Assert.AreEqual(original.AnchorMax, clone.AnchorMax);
            Assert.AreEqual(original.Height, clone.Height);
            Assert.AreEqual(original.InsetBottom, clone.InsetBottom);
            Assert.IsTrue(clone.HasAnchor);
            Assert.IsTrue(clone.HasInset);
            original.Dispose();
            clone.Dispose();
        }

        // 부모에 LayoutGroup이 있으면 앵커가 덮어써진다는 사실을 감지한다 (Dev 경고 근거)
        private void ParentLayoutGroupIsDetected()
        {
            var rect = NewRect();
            Assert.IsFalse(AnchorFeature.IsOverriddenByParentLayout(rect));

            var parent = new GameObject("parent", typeof(RectTransform), typeof(VerticalLayoutGroup));
            rect.SetParent(parent.transform, false);
            Assert.IsTrue(AnchorFeature.IsOverriddenByParentLayout(rect));

            parent.GetComponent<VerticalLayoutGroup>().enabled = false;
            Assert.IsFalse(AnchorFeature.IsOverriddenByParentLayout(rect));

            rect.SetParent(null, false);
            Object.DestroyImmediate(parent);
        }

        protected override void Cleanup()
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
                go = null;
            }
        }
    }
}
