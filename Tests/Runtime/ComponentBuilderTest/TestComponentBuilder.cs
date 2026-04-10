using Sindy.View;
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
        }

        private void BuildReturnsBuilder()
        {
            var builder = ComponentBuilder.Build("test_prefab");

            Assert.IsNotNull(builder);
        }

        private void WithModelInstanceSetsModel()
        {
            var model = new PropModel<string>("hello");

            var builder = ComponentBuilder.Build("test_prefab")
                .WithModel(model);

            Assert.IsNotNull(builder);
            Assert.IsFalse(model.IsDisposed);

            builder.Cancel();
        }

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

        private void OnLayerChaining()
        {
            var builder = ComponentBuilder.Build("root_prefab")
                .WithModel(new ViewModel())
                .OnLayer(2);

            Assert.IsNotNull(builder);

            builder.Cancel();
        }

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
    }
}
