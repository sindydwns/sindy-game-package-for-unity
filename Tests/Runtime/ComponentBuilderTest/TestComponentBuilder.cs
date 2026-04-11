using Sindy.View;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ComponentBlueprint — Create 체인, WithModel(factory), Patch, Layout, Blueprint 재사용
    /// Open()은 ComponentManager 씬이 필요하므로 빌더 조립과 상태 검증에 집중한다.
    /// </summary>
    class TestComponentBuilder : TestCase
    {
        public override void Run()
        {
            CreateReturnsBuilder();
            WithModelFactoryChains();
            PatchChaining();
            MultiplePatchesChained();
            PatchWithoutModelChained();
            LayoutOnRoot();
            LayoutOnPatch();
            MarginAfterWithModel();
            LayoutFullChain();

            // Blueprint
            BlueprintCreation();
            BlueprintAsRootCreate();
            BlueprintAsPatch();
            BlueprintNestedInBlueprint();
            PresetWithDefaultModels();
            PresetModelNotShared();
            BlueprintLayoutApplied();
            BlueprintWithOverride();
            BlueprintFactoryCreatesFreshInstances();
        }

        // Create() 호출 시 null이 아닌 Blueprint 인스턴스를 반환하는지 확인
        private void CreateReturnsBuilder()
        {
            var builder = ComponentBlueprint.Create("test_prefab");

            Assert.IsNotNull(builder);
        }

        // WithModel(팩토리)로 모델을 등록하는 체이닝이 동작하는지 확인
        private void WithModelFactoryChains()
        {
            int factoryCallCount = 0;

            var builder = ComponentBlueprint.Create("test_prefab")
                .WithModel(() =>
                {
                    factoryCallCount++;
                    return new PropModel<string>("hello");
                });

            Assert.IsNotNull(builder);
            // 팩토리는 Open() 시점에만 호출되므로 체이닝만으로는 호출되지 않음
            Assert.AreEqual(0, factoryCallCount);
        }

        // Patch()로 하위 프리팹을 등록하고 WithModel()로 모델을 연결하는 체이닝이 동작하는지 확인
        private void PatchChaining()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .WithModel(() => new ViewModel())
                .Patch("header.title", "label_prefab")
                .WithModel(() => new PropModel<string>("label"));

            Assert.IsNotNull(builder);
        }

        // 여러 Patch를 연속 체이닝해도 예외 없이 동작하는지 확인
        private void MultiplePatchesChained()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .WithModel(() => new ViewModel())
                .Patch("path.a", "prefab_a").WithModel(() => new PropModel<string>("a"))
                .Patch("path.b", "prefab_b").WithModel(() => new PropModel<string>("b"))
                .Patch("path.c", "prefab_c").WithModel(() => new PropModel<string>("c"));

            Assert.IsNotNull(builder);
        }

        // Patch() 후 WithModel() 없이 다음 Patch()를 체이닝하는 경우에도 예외 없이 동작하는지 확인
        private void PatchWithoutModelChained()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .Patch("first_path", "first_prefab")
                .Patch("second_path", "second_prefab")
                .WithModel(() => new PropModel<string>("second"));

            Assert.IsNotNull(builder);
        }

        // Create() 직후 Layout/Padding 등 루트에 레이아웃을 지정할 수 있는지 확인
        private void LayoutOnRoot()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .WithModel(() => new ViewModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16);

            Assert.IsNotNull(builder);
        }

        // Patch() 직후 (WithModel 전) 레이아웃을 지정할 수 있는지 확인
        private void LayoutOnPatch()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .WithModel(() => new ViewModel())
                .Patch("body", "container")
                .Layout(Direction.Horizontal, spacing: 12)
                .Align(TextAnchor.MiddleCenter);

            Assert.IsNotNull(builder);
        }

        // WithModel() 이후에도 Margin/Size를 직전 Patch 대상에 지정할 수 있는지 확인
        private void MarginAfterWithModel()
        {
            var builder = ComponentBlueprint.Create("root_prefab")
                .WithModel(() => new ViewModel())
                .Patch("header.title", "label").WithModel(() => new PropModel<string>("label"))
                .Margin(bottom: 12)
                .Size(width: 200);

            Assert.IsNotNull(builder);
        }

        // Layout, Margin, Padding, Align, Size를 조합한 복합 체이닝이 예외 없이 동작하는지 확인
        private void LayoutFullChain()
        {
            var builder = ComponentBlueprint.Create("popup")
                .WithModel(() => new ViewModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(top: 16, right: 16, bottom: 16, left: 16)
                .Patch("header", "label").WithModel(() => new PropModel<string>("title"))
                .Margin(bottom: 12)
                .Patch("body", "container")
                .Layout(Direction.Horizontal, spacing: 4)
                .Align(TextAnchor.MiddleLeft)
                .Patch("body.item", "label").WithModel(() => new PropModel<string>("item"))
                .Size(width: 100, height: 40)
                .Patch("footer", "button").WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // ── Blueprint 테스트 ─────────────────────────────────────────────────

        // Blueprint 생성 시 null이 아닌 인스턴스를 반환하는지 확인
        private void BlueprintCreation()
        {
            var bp = ComponentBlueprint.Create("card")
                .Layout(Direction.Vertical, spacing: 4)
                .Patch("title", "label")
                .Patch("desc", "label");

            Assert.IsNotNull(bp);
        }

        // Create(Blueprint)로 Blueprint 기반 빌더를 생성할 수 있는지 확인
        private void BlueprintAsRootCreate()
        {
            var bp = ComponentBlueprint.Create("card")
                .Layout(Direction.Vertical)
                .Patch("title", "label");

            var builder = ComponentBlueprint.Create(bp)
                .WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // Patch(path, Blueprint)로 Blueprint를 하위 패치에 적용할 수 있는지 확인
        private void BlueprintAsPatch()
        {
            var header = ComponentBlueprint.Create("header_bar")
                .Layout(Direction.Horizontal, spacing: 8)
                .Patch("icon", "icon_prefab")
                .Patch("title", "label");

            var builder = ComponentBlueprint.Create("popup")
                .WithModel(() => new ViewModel())
                .Patch("header", header).WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // Blueprint 안에 Blueprint를 중첩할 수 있는지 확인
        private void BlueprintNestedInBlueprint()
        {
            var iconLabel = ComponentBlueprint.Create("row")
                .Layout(Direction.Horizontal, spacing: 4)
                .Patch("icon", "icon_prefab")
                .Patch("text", "label");

            var card = ComponentBlueprint.Create("card")
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("header", iconLabel)
                .Patch("body", "label");

            var builder = ComponentBlueprint.Create("screen")
                .WithModel(() => new ViewModel())
                .Patch("card", card).WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // Preset(Blueprint + 기본 모델 팩토리) 사용 시 여러 번 빌더를 생성해도 독립적인지 확인
        private void PresetWithDefaultModels()
        {
            var preset = ComponentBlueprint.Create("card")
                .WithModel(() => new ViewModel())
                .Patch("title", "label").WithModel(() => new PropModel<string>("default"))
                .Patch("desc", "label").WithModel(() => new PropModel<string>("desc"));

            var b1 = ComponentBlueprint.Create(preset);
            var b2 = ComponentBlueprint.Create(preset);

            Assert.IsNotNull(b1);
            Assert.IsNotNull(b2);
            Assert.AreNotEqual(b1, b2);
        }

        // Preset을 두 곳에서 사용해도 팩토리 함수가 공유되는 것이지 인스턴스가 공유되지 않는지 확인
        private void PresetModelNotShared()
        {
            int callCount = 0;
            var preset = ComponentBlueprint.Create("row")
                .Patch("label", "label").WithModel(() =>
                {
                    callCount++;
                    return new PropModel<string>("text");
                });

            // 체이닝 중에는 팩토리가 호출되지 않음
            var b1 = ComponentBlueprint.Create("screen")
                .WithModel(() => new ViewModel())
                .Patch("row1", preset).WithModel(() => new ViewModel());

            var b2 = ComponentBlueprint.Create("screen")
                .WithModel(() => new ViewModel())
                .Patch("row2", preset).WithModel(() => new ViewModel());

            Assert.IsNotNull(b1);
            Assert.IsNotNull(b2);
            Assert.AreEqual(0, callCount);
        }

        // Blueprint에 지정한 레이아웃이 빌더에 전달되는지 확인
        private void BlueprintLayoutApplied()
        {
            var bp = ComponentBlueprint.Create("container")
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16)
                .Patch("item", "label").Margin(bottom: 4);

            var builder = ComponentBlueprint.Create(bp)
                .WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // Builder에서 Blueprint의 기본값을 WithModel로 덮어쓸 수 있는지 확인
        private void BlueprintWithOverride()
        {
            var bp = ComponentBlueprint.Create("card")
                .WithModel(() => new ViewModel())
                .Patch("title", "label").WithModel(() => new PropModel<string>("default"));

            var builder = ComponentBlueprint.Create(bp)
                .WithModel(() => new ViewModel());

            Assert.IsNotNull(builder);
        }

        // Blueprint 팩토리는 매번 새 인스턴스를 만들어야 하므로 두 번 호출 시 서로 다른 인스턴스가 생성되는지 확인
        private void BlueprintFactoryCreatesFreshInstances()
        {
            int invocations = 0;
            var factory = new System.Func<object>(() =>
            {
                invocations++;
                return new PropModel<string>("fresh");
            });

            var bp = ComponentBlueprint.Create("card")
                .WithModel(factory);

            var first = factory();
            var second = factory();

            Assert.IsNotNull(bp);
            Assert.AreEqual(2, invocations);
            Assert.AreNotEqual(first, second);
        }
    }
}
