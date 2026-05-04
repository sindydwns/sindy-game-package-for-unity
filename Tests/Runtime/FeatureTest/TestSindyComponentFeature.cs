using Sindy.View;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// SindyComponent.With / Feature API — 컴포넌트 단위 Feature 부착, 조회, 모델 폴백, Dispose 연쇄
    /// </summary>
    class TestSindyComponentFeature : TestCase
    {
        public override void Run()
        {
            AttachAndRetrieve();
            FeatureReturnsNullWhenNotAttached();
            FeatureFallsBackToModel();
            ComponentFeatureOverridesModel();
            OverwritesSameTypeAndDisposesPrevious();
            VisibilityFeatureBindsFromComponent();
            LayoutFeatureBindsFromComponent();
            FeatureDisposedOnDestroy();
            FeatureSurvivesModelChange();
        }

        // 컴포넌트에 With()로 Feature를 부착한 뒤 Feature<T>()로 조회할 수 있는지 확인
        private void AttachAndRetrieve()
        {
            var component = NewComponent();
            component.With(new InteractableFeature(true));

            var feature = component.Feature<InteractableFeature>();

            Assert.IsNotNull(feature);
            Assert.IsTrue(feature.Interactable.Value);

            Object.DestroyImmediate(component.gameObject);
        }

        // Feature를 부착하지 않고 모델도 없을 때 null을 반환하는지 확인
        private void FeatureReturnsNullWhenNotAttached()
        {
            var component = NewComponent();

            var feature = component.Feature<InteractableFeature>();

            Assert.IsNull(feature);

            Object.DestroyImmediate(component.gameObject);
        }

        // 컴포넌트에 Feature가 없으면 모델의 Feature를 폴백 조회하는지 확인
        private void FeatureFallsBackToModel()
        {
            var component = NewComponent();
            var model = new ViewModel();
            model.With(new InteractableFeature(false));
            component.SetModel(model);

            var feature = component.Feature<InteractableFeature>();

            Assert.IsNotNull(feature);
            Assert.IsFalse(feature.Interactable.Value);

            component.SetModel(null);
            model.Dispose();
            Object.DestroyImmediate(component.gameObject);
        }

        // 동일 타입 Feature가 컴포넌트와 모델 양쪽에 있으면 컴포넌트가 우선되는지 확인
        private void ComponentFeatureOverridesModel()
        {
            var component = NewComponent();
            var model = new ViewModel();
            model.With(new InteractableFeature(false));
            component.With(new InteractableFeature(true));
            component.SetModel(model);

            var feature = component.Feature<InteractableFeature>();

            Assert.IsTrue(feature.Interactable.Value);

            component.SetModel(null);
            model.Dispose();
            Object.DestroyImmediate(component.gameObject);
        }

        // 같은 타입 Feature를 다시 부착하면 기존 Feature가 Dispose되고 교체되는지 확인
        private void OverwritesSameTypeAndDisposesPrevious()
        {
            var component = NewComponent();
            var first = new InteractableFeature(true);
            var second = new InteractableFeature(false);

            component.With(first);
            component.With(second);

            Assert.IsTrue(first.IsDisposed);
            Assert.IsFalse(second.IsDisposed);
            Assert.AreSame(second, component.Feature<InteractableFeature>());

            Object.DestroyImmediate(component.gameObject);
        }

        // 컴포넌트에 부착된 VisibilityFeature가 SetModel 시 gameObject.SetActive에 바인딩되는지 확인
        private void VisibilityFeatureBindsFromComponent()
        {
            var component = NewComponent();
            var visibility = new VisibilityFeature(true);
            component.With(visibility);
            component.SetModel(new ViewModel());

            Assert.IsTrue(component.gameObject.activeSelf);

            visibility.Show.Value = false;
            Assert.IsFalse(component.gameObject.activeSelf);

            visibility.Show.Value = true;
            Assert.IsTrue(component.gameObject.activeSelf);

            component.SetModel(null);
            Object.DestroyImmediate(component.gameObject);
        }

        // 컴포넌트에 부착된 LayoutFeature가 SetModel 시 RectTransform에 적용되는지 확인
        private void LayoutFeatureBindsFromComponent()
        {
            var go = new GameObject("TestLayoutFeatureComponent", typeof(RectTransform));
            var component = go.AddComponent<DummySindyComponent>();
            var layout = new LayoutFeature();

            component.With(layout);
            component.SetModel(new ViewModel());

            // LayoutFeature.Apply가 RectTransform 대상으로 호출되어 예외 없이 동작하고,
            // 부착된 Feature가 그대로 조회되는지 확인한다.
            Assert.AreSame(layout, component.Feature<LayoutFeature>());

            component.SetModel(null);
            Object.DestroyImmediate(go);
        }

        // 컴포넌트가 OnDestroy되면 부착된 Feature도 함께 Dispose되는지 확인
        private void FeatureDisposedOnDestroy()
        {
            var component = NewComponent();
            var feature = new InteractableFeature();
            component.With(feature);

            Object.DestroyImmediate(component.gameObject);

            Assert.IsTrue(feature.IsDisposed);
        }

        // 모델을 교체해도 컴포넌트에 부착된 Feature는 유지되는지 확인
        private void FeatureSurvivesModelChange()
        {
            var component = NewComponent();
            var feature = new InteractableFeature(true);
            component.With(feature);

            var model1 = new ViewModel();
            component.SetModel(model1);
            Assert.AreSame(feature, component.Feature<InteractableFeature>());

            component.SetModel(null);
            Assert.IsFalse(feature.IsDisposed);
            Assert.AreSame(feature, component.Feature<InteractableFeature>());

            var model2 = new ViewModel();
            component.SetModel(model2);
            Assert.AreSame(feature, component.Feature<InteractableFeature>());

            component.SetModel(null);
            model1.Dispose();
            model2.Dispose();
            Object.DestroyImmediate(component.gameObject);
        }

        private static DummySindyComponent NewComponent()
        {
            var go = new GameObject("TestSindyComponentFeature", typeof(RectTransform));
            return go.AddComponent<DummySindyComponent>();
        }

        private class DummySindyComponent : SindyComponent { }
    }
}
