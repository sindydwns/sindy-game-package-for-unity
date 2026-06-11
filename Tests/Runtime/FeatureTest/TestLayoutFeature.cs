using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace Sindy.Test
{
    /// <summary>
    /// LayoutFeature — 공개 fluent API 구성, Apply 결과,
    /// 재적용 시 LayoutGroup 중복 추가 없음(재바인딩 버그 회귀 방지)
    /// </summary>
    class TestLayoutFeature : TestCase
    {
        private GameObject go;

        public override void Run()
        {
            FluentApplyCreatesGroup();
            ReApplyDoesNotDuplicate();
            SizeCreatesLayoutElementOnce();
        }

        private RectTransform NewRect()
        {
            Cleanup();
            go = new GameObject("layout_target", typeof(RectTransform));
            return (RectTransform)go.transform;
        }

        // 공개 API로 구성한 LayoutFeature가 VerticalLayoutGroup을 생성하는지 확인
        private void FluentApplyCreatesGroup()
        {
            var rect = NewRect();
            var feature = new LayoutFeature()
                .Layout(Direction.Vertical, spacing: 14)
                .Padding(top: 12, right: 32, bottom: 16, left: 32)
                .Align(TextAnchor.UpperLeft);

            feature.Apply(rect);

            var group = go.GetComponent<VerticalLayoutGroup>();
            Assert.IsNotNull(group);
            Assert.AreEqual(14f, group.spacing);
            Assert.AreEqual(32, group.padding.left);
            Assert.AreEqual(12, group.padding.top);
            Assert.AreEqual(TextAnchor.UpperLeft, group.childAlignment);
            feature.Dispose();
        }

        // 같은 대상에 다시 Apply해도 LayoutGroup이 중복 추가되지 않는지 확인 (회귀 방지)
        private void ReApplyDoesNotDuplicate()
        {
            var rect = NewRect();
            var feature = new LayoutFeature().Layout(Direction.Vertical, 8);

            feature.Apply(rect);
            feature.Apply(rect);

            Assert.AreEqual(1, go.GetComponents<VerticalLayoutGroup>().Length);
            feature.Dispose();
        }

        // Size 재적용 시 LayoutElement도 1개만 유지되는지 확인
        private void SizeCreatesLayoutElementOnce()
        {
            var rect = NewRect();
            var feature = new LayoutFeature().Size(width: 100, height: 50);

            feature.Apply(rect);
            feature.Apply(rect);

            var elements = go.GetComponents<LayoutElement>();
            Assert.AreEqual(1, elements.Length);
            Assert.AreEqual(100f, elements[0].preferredWidth);
            Assert.AreEqual(50f, elements[0].preferredHeight);
            feature.Dispose();
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
