using Sindy.View;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ComponentBuilder — Build 체인, WithModel(instance/factory), Patch, Cancel/Dispose
    /// Open()은 ComponentManager 씬이 필요하므로 Cancel 중심으로 검증
    /// </summary>
    class TestComponentBuilder : TestCase
    {
        public override void Run()
        {
            BuildReturnsBuilder();
            WithModelInstanceSetsModel();
            WithModelFactorySetsModel();
            PatchChaining();
            CancelDisposesOwnedModels();
            CancelDoesNotDisposeFactoryModels();
            CancelDisposesMixedModels();
            PatchWithoutModelFlushed();
            MultiplePatchesChained();
            OnLayerChaining();
            CancelClearsState();
            LayoutOnRoot();
            LayoutOnPatch();
            MarginAfterWithModel();
            LayoutFullChain();
            CancelClearsLayout();
        }

        // Build() 호출 시 null이 아닌 빌더 인스턴스를 반환하는지 확인
        private void BuildReturnsBuilder()
        {
            var builder = ComponentBuilder.Build("test_prefab");

            Assert.IsNotNull(builder);
        }

        // WithModel(인스턴스)로 모델을 설정하면 빌더에 등록되고 모델이 Dispose되지 않은 상태인지 확인
        private void WithModelInstanceSetsModel()
        {
            var model = new PropModel<string>("hello");

            var builder = ComponentBuilder.Build("test_prefab")
                .WithModel(model);

            Assert.IsNotNull(builder);
            Assert.IsFalse(model.IsDisposed);

            builder.Cancel();
        }

        // WithModel(팩토리)로 등록 시 Cancel하면 팩토리가 호출되지 않는지 확인
        private void WithModelFactorySetsModel()
        {
            bool factoryCalled = false;

            var builder = ComponentBuilder.Build("test_prefab")
                .WithModel(() =>
                {
                    factoryCalled = true;
                    return new PropModel<string>("hello");
                });

            // Factory는 Open() 시점에 호출되므로 Cancel 시 호출되지 않음
            builder.Cancel();
            Assert.IsFalse(factoryCalled);
        }

        // Patch()로 하위 프리팹을 등록하고 WithModel()로 모델을 연결하는 체이닝이 동작하는지 확인
        private void PatchChaining()
        {
            var rootModel = new ViewModel();
            var patchModel = new PropModel<string>("label");

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(rootModel)
                .Patch("header.title", "label_prefab")
                .WithModel(patchModel);

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

        // Cancel 시 WithModel(인스턴스)로 등록된 모든 모델이 Dispose되는지 확인
        private void CancelDisposesOwnedModels()
        {
            var rootModel = new PropModel<string>("root");
            var patchModel = new PropModel<string>("patch");

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(rootModel)
                .Patch("child", "child_prefab")
                .WithModel(patchModel);

            builder.Cancel();

            // WithModel(instance)로 등록된 모델은 Cancel 시 Dispose됨
            Assert.IsTrue(rootModel.IsDisposed);
            Assert.IsTrue(patchModel.IsDisposed);
        }

        // Cancel 시 팩토리 모델은 아직 생성되지 않았으므로 팩토리가 호출되지 않는지 확인
        private void CancelDoesNotDisposeFactoryModels()
        {
            bool factoryCalled = false;

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(() =>
                {
                    factoryCalled = true;
                    return new PropModel<string>("lazy");
                });

            builder.Cancel();

            // Factory 모델은 아직 생성되지 않았으므로 Dispose 대상 아님
            Assert.IsFalse(factoryCalled);
        }

        // 인스턴스 모델과 팩토리 모델이 혼합된 경우 Cancel 시 인스턴스만 Dispose되는지 확인
        private void CancelDisposesMixedModels()
        {
            var instanceModel = new PropModel<string>("instance");
            bool factoryCalled = false;

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(instanceModel)
                .Patch("lazy_child", "child_prefab")
                .WithModel(() =>
                {
                    factoryCalled = true;
                    return new PropModel<string>("lazy");
                });

            builder.Cancel();

            // 인스턴스 모델만 Dispose, 팩토리는 호출되지 않음
            Assert.IsTrue(instanceModel.IsDisposed);
            Assert.IsFalse(factoryCalled);
        }

        // Patch() 후 WithModel() 없이 다음 Patch()를 호출하면 이전 Patch가 모델 없이 flush되는지 확인
        private void PatchWithoutModelFlushed()
        {
            // Patch() 후 WithModel() 없이 다음 Patch()를 호출하면
            // 이전 Patch가 모델 없이 flush됨
            var model = new PropModel<string>("second");

            var builder = ComponentBuilder.Build("root_prefab")
                .Patch("first_path", "first_prefab")
                .Patch("second_path", "second_prefab")
                .WithModel(model);

            // Cancel 시 예외 없이 처리됨
            builder.Cancel();

            Assert.IsTrue(model.IsDisposed);
        }

        // 여러 Patch를 연속 체이닝하고 Cancel 시 모든 모델이 Dispose되는지 확인
        private void MultiplePatchesChained()
        {
            var m1 = new PropModel<string>("a");
            var m2 = new PropModel<string>("b");
            var m3 = new PropModel<string>("c");

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .Patch("path.a", "prefab_a").WithModel(m1)
                .Patch("path.b", "prefab_b").WithModel(m2)
                .Patch("path.c", "prefab_c").WithModel(m3);

            builder.Cancel();

            Assert.IsTrue(m1.IsDisposed);
            Assert.IsTrue(m2.IsDisposed);
            Assert.IsTrue(m3.IsDisposed);
        }

        // OnLayer()를 체이닝하여 레이어를 설정할 수 있는지 확인
        private void OnLayerChaining()
        {
            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .OnLayer(2);

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

        // Cancel 후 두 번째 Cancel 호출 시 이미 정리된 상태에서 예외 없이 통과하는지 확인
        private void CancelClearsState()
        {
            var model = new PropModel<string>("test");

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(model)
                .Patch("child", "child_prefab")
                .WithModel(new PropModel<string>("child"));

            builder.Cancel();

            // 두 번째 Cancel은 이미 정리된 상태이므로 예외 없이 통과
            builder.Cancel();
        }

        // Build() 직후 Layout/Padding 등 루트에 레이아웃을 지정할 수 있는지 확인
        private void LayoutOnRoot()
        {
            var model = new ViewModel();

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(model)
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16);

            Assert.IsNotNull(builder);
            Assert.IsNotNull(model.Feature<LayoutFeature>());

            builder.Cancel();
        }

        // Patch() 직후 (WithModel 전) 레이아웃을 지정할 수 있는지 확인
        private void LayoutOnPatch()
        {
            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .Patch("body", "container")
                .Layout(Direction.Horizontal, spacing: 12)
                .Align(TextAnchor.MiddleCenter);

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

        // WithModel() 이후에도 Margin/Size를 직전 Patch 대상에 지정할 수 있는지 확인
        private void MarginAfterWithModel()
        {
            var patchModel = new PropModel<string>("label");

            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .Patch("header.title", "label").WithModel(patchModel)
                .Margin(bottom: 12)
                .Size(width: 200);

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

        // Layout, Margin, Padding, Align, Size를 조합한 복합 체이닝이 예외 없이 동작하는지 확인
        private void LayoutFullChain()
        {
            var builder = ComponentBuilder.Build("popup")
                .WithModel(new ViewModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(top: 16, right: 16, bottom: 16, left: 16)
                .Patch("header", "label").WithModel(new PropModel<string>("title"))
                .Margin(bottom: 12)
                .Patch("body", "container")
                .Layout(Direction.Horizontal, spacing: 4)
                .Align(TextAnchor.MiddleLeft)
                .Patch("body.item", "label").WithModel(new PropModel<string>("item"))
                .Size(width: 100, height: 40)
                .Patch("footer", "button").WithModel(new ViewModel());

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

        // Cancel 시 레이아웃 상태도 정리되어 두 번째 Cancel에서 예외가 없는지 확인
        private void CancelClearsLayout()
        {
            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .Layout(Direction.Vertical)
                .Patch("child", "child_prefab")
                .WithModel(new PropModel<string>("child"))
                .Margin(top: 10);

            builder.Cancel();
            builder.Cancel();
        }
    }
}
