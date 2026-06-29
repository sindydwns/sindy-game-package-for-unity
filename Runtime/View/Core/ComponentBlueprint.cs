using System;
using System.Collections.Generic;
using System.Linq;
using Sindy.View.FeatureViews;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// 사전 제작된 부품 프리팹들을 조합해 UI를 만들어내는 설계도(데이터) 겸 재사용 가능한 템플릿.
    /// 선언 시점에는 아무것도 생성되지 않으며, Open() 시점에 루트 프리팹이 인스턴스화되고
    /// 각 Patch가 가리키는 프리팹이 해당 경로의 자식으로 인스턴스화·바인딩된다.
    ///
    /// 재사용 템플릿:
    ///   static readonly ComponentBlueprint Card = ComponentBlueprint
    ///       .Create("card")
    ///           .Layout(Direction.Vertical, spacing: 4)
    ///       .Patch("icon", "icon_prefab")
    ///       .Patch("title", "label");
    ///
    /// 즉시 실행:
    ///   ComponentBlueprint
    ///       .Create("popup").WithModel(() => new PopupModel())
    ///       .Patch("header", Card).WithModel(() => headerModel)
    ///       .Open(layer: 1);
    ///
    /// 설계 원칙:
    ///   - 모델은 항상 팩토리로 주입한다. 인스턴스 공유를 막기 위해.
    ///   - Open()은 Blueprint 상태를 변경하지 않는다. 여러 번 호출해도 동일 결과.
    ///   - 1회용 개념이 없다. Cancel()이 없고, 버리면 그만이다.
    ///
    /// 조립 규칙 (Open):
    ///   - 패치 경로에 해당하는 키가 루트 프리팹(SindyComponent)에 이미 있으면 인스턴스화를
    ///     생략하고 모델만 주입한다 — 틀은 프리팹에, 가변 부품은 코드에 두는 하이브리드 허용.
    ///   - 중간 경로(컨테이너)가 없으면 RectTransform+SindyComponent 빈 컨테이너를 자동 생성한다.
    ///   - 같은 경로를 여러 번 패치하면 마지막 선언이 우선하되, 지정하지 않은
    ///     모델 팩토리/레이아웃은 이전 선언에서 승계한다 (파생 Blueprint의 부분 재정의).
    ///   - 형제 순서 = 같은 깊이에서의 패치 선언 순서.
    /// </summary>
    public class ComponentBlueprint
    {
        // ── 내부 자료 ──────────────────────────────────────────────────────────

        private readonly string prefabName;
        private readonly ComponentBlueprint baseBlueprint;
        private Func<IViewModel> rootModelFactory;

        private readonly List<PatchInstruction> patches = new();
        private PatchInstruction pendingPatch;
        private PatchInstruction lastFlushedPatch;
        private LayoutFeature rootLayout;

        internal string PrefabName => prefabName;
        internal LayoutFeature RootLayout => rootLayout;
        internal Func<IViewModel> RootModelFactory => rootModelFactory;
        internal IReadOnlyList<PatchInstruction> PatchEntries => patches;

        internal class PatchInstruction
        {
            public readonly string Path;
            public readonly string PrefabName;
            public readonly ComponentBlueprint Blueprint;
            public Func<IViewModel> ModelFactory;
            public LayoutFeature Layout;

            public PatchInstruction(string path, string prefabName)
            {
                Path = path;
                PrefabName = prefabName;
            }

            public PatchInstruction(string path, ComponentBlueprint blueprint)
            {
                Path = path;
                PrefabName = blueprint.prefabName;
                Blueprint = blueprint;
            }
        }

        // ── 생성 ───────────────────────────────────────────────────────────────

        private ComponentBlueprint(string prefabName)
        {
            this.prefabName = prefabName;
        }

        private ComponentBlueprint(ComponentBlueprint template)
        {
            baseBlueprint = template;
            prefabName = template.prefabName;
        }

        /// <summary>프리팹 이름으로 새 Blueprint를 생성한다.</summary>
        public static ComponentBlueprint Create(string prefabName) => new(prefabName);

        /// <summary>기존 Blueprint를 기반으로 파생 Blueprint를 생성한다. 템플릿의 구조가 자동 전개된다.</summary>
        public static ComponentBlueprint Create(ComponentBlueprint template) => new(template);

        // ── 모델 지정 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 직전 Create() 또는 Patch()에 팩토리로 모델을 지정한다.
        /// Open() 시점에 팩토리가 실행되어 매번 새 인스턴스가 생성된다.
        /// </summary>
        public ComponentBlueprint WithModel(Func<IViewModel> factory)
        {
            if (pendingPatch != null)
            {
                pendingPatch.ModelFactory = factory;
                patches.Add(pendingPatch);
                lastFlushedPatch = pendingPatch;
                pendingPatch = null;
            }
            else
            {
                rootModelFactory = factory;
                lastFlushedPatch = null;
            }
            return this;
        }

        // ── 패치 ───────────────────────────────────────────────────────────────

        /// <summary>경로에 프리팹을 패치한다.</summary>
        public ComponentBlueprint Patch(string path, string prefabName)
        {
            FlushPendingPatch();
            pendingPatch = new PatchInstruction(path, prefabName);
            lastFlushedPatch = null;
            return this;
        }

        /// <summary>경로에 Blueprint 구조를 패치한다. 하위 패치가 자동 전개된다.</summary>
        public ComponentBlueprint Patch(string path, ComponentBlueprint blueprint)
        {
            FlushPendingPatch();
            pendingPatch = new PatchInstruction(path, blueprint);
            lastFlushedPatch = null;
            return this;
        }

        private void FlushPendingPatch()
        {
            if (pendingPatch == null) return;
            patches.Add(pendingPatch);
            pendingPatch = null;
        }

        // ── 레이아웃 ───────────────────────────────────────────────────────────

        /// <summary>자식 배치 방향과 간격을 지정한다.</summary>
        public ComponentBlueprint Layout(Direction direction, float spacing = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.LayoutDirection = direction;
            f.Spacing = spacing;
            return this;
        }

        /// <summary>내부 여백을 지정한다 (사방 동일).</summary>
        public ComponentBlueprint Padding(float all)
            => Padding(all, all, all, all);

        /// <summary>내부 여백을 지정한다.</summary>
        public ComponentBlueprint Padding(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.PaddingTop = top; f.PaddingRight = right; f.PaddingBottom = bottom; f.PaddingLeft = left;
            f.HasPadding = true;
            return this;
        }

        /// <summary>자식 정렬 기준을 지정한다.</summary>
        public ComponentBlueprint Align(TextAnchor anchor)
        {
            GetOrCreateCurrentLayout().Alignment = anchor;
            return this;
        }

        /// <summary>선호 크기를 지정한다. -1이면 미지정.</summary>
        public ComponentBlueprint Size(float width = -1, float height = -1)
        {
            var f = GetOrCreateCurrentLayout();
            f.PreferredWidth = width;
            f.PreferredHeight = height;
            return this;
        }

        /// <summary>유연 크기 가중치를 지정한다. 남는 공간을 형제와 비율로 나눠 갖는다. -1이면 미지정.</summary>
        public ComponentBlueprint Flexible(float width = -1, float height = -1)
        {
            var f = GetOrCreateCurrentLayout();
            f.FlexibleWidth = width;
            f.FlexibleHeight = height;
            return this;
        }

        private LayoutFeature GetOrCreateCurrentLayout()
        {
            if (pendingPatch != null)
                return pendingPatch.Layout ??= new LayoutFeature();
            if (lastFlushedPatch != null)
                return lastFlushedPatch.Layout ??= new LayoutFeature();
            return rootLayout ??= new LayoutFeature();
        }

        // ── 실행 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 루트 프리팹을 인스턴스화하고 모든 패치를 적용한다 — 모델 트리 구성 → 루트 바인딩 →
        /// 패치 프리팹 인스턴스화·부착·바인딩 순. 생성된 루트 인스턴스를 반환한다.
        /// Blueprint 상태는 변경되지 않으므로 여러 번 호출해도 안전하다.
        /// </summary>
        public SindyComponent Open(int layer = 0)
        {
            FlushPendingPatch();

            var prefab = ComponentManager.GetPrefab<SindyComponent>(prefabName);
            if (prefab == null)
                throw new InvalidOperationException($"ComponentBlueprint: prefab '{prefabName}' not found.");

            var patches = CollectFinalPatches();
            var rootModel = BuildModelTree(patches);

            var preset = new ComponentPreset(prefab, rootModel, layer);
            var instance = ComponentManager.Open(preset);

            if (rootModel is ViewModel rootVM && patches.Count > 0)
                AssembleViews(instance, rootVM, patches);

            return instance;
        }

        /// <summary>
        /// 설계도가 정의한 모양 그대로 모델 트리만 생성한다 (뷰 생성 없음).
        /// 팩토리가 실행되어 매번 새 인스턴스가 만들어지고, 설계도의 Layout/Padding/Size가
        /// LayoutFeature로 부착된 완전한 트리를 반환한다.
        ///
        /// 용도: 같은 설계도로 이미 조립된 인스턴스에 새 모델을 재주입(풀링 등)할 때 —
        /// 디자인을 손으로 복제하지 않고 설계도가 모델 모양을 책임진다.
        /// 구조가 다른 인스턴스에 주입하면 일치하지 않는 키는 무시된다(재바인딩은 구조를 바꾸지 못함).
        /// Open()도 내부적으로 이 메서드를 사용하므로 두 경로는 항상 같은 모양을 만든다.
        /// </summary>
        public IViewModel BuildModelTree()
        {
            FlushPendingPatch();
            return BuildModelTree(CollectFinalPatches());
        }

        /// <summary>
        /// 다음 프레임에 기존 인스턴스를 파괴하고 설계도를 재실행한다(재-Open).
        /// 버튼 OnClick 방출 도중 호출해도 안전하다 — 파괴는 방출 스택을 벗어난 뒤
        /// (<see cref="FrameDispatcher"/>) 일어나므로, 방출 중인 모델 트리를 자기 자신이 파괴하는
        /// 재진입 오류가 발생하지 않는다.
        ///
        /// <paramref name="disposeOld"/>가 true면(기본) 파괴 직전 바인딩돼 있던 모델을 Dispose한다.
        /// 모델을 공유·재사용한다면 false로 두고 호출부가 수명을 직접 관리한다.
        /// 새로 열린 인스턴스는 <paramref name="onOpened"/> 콜백으로 전달된다.
        /// </summary>
        /// <param name="instance">파괴할 기존 인스턴스.</param>
        /// <param name="disposeOld">이전 모델을 Dispose할지 여부. 기본 true.</param>
        /// <param name="onOpened">재-Open 완료 후 새 인스턴스를 받아 후처리(선택).</param>
        /// <param name="layer">새 인스턴스를 열 레이어.</param>
        public void ReopenNextFrame(SindyComponent instance, bool disposeOld = true,
            Action<SindyComponent> onOpened = null, int layer = 0)
        {
            var old = instance != null ? instance.CurrentModel : null;
            FrameDispatcher.NextFrame(() =>
            {
                if (instance != null)
                {
                    instance.Bind(null);
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
                if (disposeOld)
                {
                    (old as IDisposable)?.Dispose();
                }
                onOpened?.Invoke(Open(layer));
            });
        }

        private IViewModel BuildModelTree(List<PatchInstruction> patches)
        {
            var rootModel = rootModelFactory?.Invoke()
                            ?? baseBlueprint?.RootModelFactory?.Invoke();

            if (rootModel is ViewModel viewModel)
            {
                var rootLayoutTemplate = rootLayout ?? baseBlueprint?.RootLayout;
                if (rootLayoutTemplate != null)
                    ApplyBlueprintLayout(viewModel, rootLayoutTemplate, "(root)", prefabName);

                foreach (var patch in patches)
                {
                    // 모델 팩토리가 없는 패치(구조 전용 부품)는 빈 ViewModel로 자리를 만든다 —
                    // 뷰 조립과 Dispose 체인이 모델 트리와 1:1로 유지되도록.
                    var patchModel = patch.ModelFactory?.Invoke() ?? new ViewModel();
                    if (patch.Layout != null)
                    {
                        if (patchModel is ViewModel patchVM)
                            ApplyBlueprintLayout(patchVM, patch.Layout, patch.Path, prefabName);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        else
                            Debug.LogWarning(
                                $"ComponentBlueprint('{prefabName}'): 패치 '{patch.Path}'의 모델이 ViewModel이 아니어서 " +
                                $"Layout/Padding/Size 지정이 무시됩니다. ({patchModel.GetType().Name})");
#endif
                    }
                    viewModel.AddChild(patch.Path, patchModel, disposeWithParent: true);
                }
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            else if (patches.Count > 0)
            {
                Debug.LogWarning(
                    $"ComponentBlueprint('{prefabName}'): 루트 모델이 ViewModel이 아니어서 " +
                    $"패치 {patches.Count}건이 무시됩니다. WithModel에 ViewModel을 지정하세요.");
            }
#endif
            return rootModel;
        }

        /// <summary>
        /// Blueprint의 LayoutFeature 클론을 모델에 부착한다.
        /// 모델 팩토리가 이미 LayoutFeature를 넣어둔 경우 Blueprint가 덮어쓰므로 경고한다 —
        /// 디자인은 Blueprint 체인에, 기능은 모델에 두는 분리 원칙 위반의 가시화.
        /// </summary>
        private static void ApplyBlueprintLayout(ViewModel target, LayoutFeature template, string path, string prefabName)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (target.Feature<LayoutFeature>() != null)
                Debug.LogWarning(
                    $"ComponentBlueprint('{prefabName}'): '{path}' 모델에 이미 LayoutFeature가 있어 " +
                    $"Blueprint의 레이아웃이 이를 덮어씁니다. 레이아웃 선언을 한 곳으로 모으세요.");
#endif
            target.With(template.Clone());
        }

        // ── 뷰 조립 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 전개된 패치를 경로 dedupe(마지막 선언 우선, 미지정 항목은 승계)하고
        /// 부모가 자식보다 먼저 조립되도록 깊이 기준으로 안정 정렬한다.
        /// 원본 PatchInstruction은 변경하지 않는다 (Open 불변성).
        /// </summary>
        private List<PatchInstruction> CollectFinalPatches()
        {
            var indexByPath = new Dictionary<string, int>();
            var result = new List<PatchInstruction>();

            foreach (var raw in EnumerateExpanded())
            {
                // Blueprint를 패치로 사용한 경우, 체인에서 레이아웃을 따로 지정하지 않았다면
                // 해당 Blueprint의 루트 레이아웃이 패치 노드의 레이아웃이 된다.
                var patch = raw;
                if (patch.Blueprint != null && patch.Layout == null && patch.Blueprint.RootLayout != null)
                {
                    patch = new PatchInstruction(raw.Path, raw.Blueprint)
                    {
                        ModelFactory = raw.ModelFactory,
                        Layout = raw.Blueprint.RootLayout,
                    };
                }

                if (indexByPath.TryGetValue(patch.Path, out var index))
                {
                    var prev = result[index];
                    var merged = patch.Blueprint != null
                        ? new PatchInstruction(patch.Path, patch.Blueprint)
                        : new PatchInstruction(patch.Path, patch.PrefabName);
                    merged.ModelFactory = patch.ModelFactory ?? prev.ModelFactory;
                    merged.Layout = patch.Layout ?? prev.Layout;
                    result[index] = merged;
                }
                else
                {
                    indexByPath.Add(patch.Path, result.Count);
                    result.Add(patch);
                }
            }

            return result.OrderBy(p => p.Path.Count(c => c == '.')).ToList();
        }

        /// <summary>
        /// 루트 인스턴스에 패치 프리팹들을 인스턴스화·부착·바인딩한다.
        /// 키가 이미 존재하면 인스턴스화를 생략하고 모델만 주입한다.
        /// </summary>
        private static void AssembleViews(SindyComponent rootInstance, ViewModel rootModel, List<PatchInstruction> patches)
        {
            foreach (var patch in patches)
            {
                var tokens = patch.Path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var parent = rootInstance;
                IViewModel parentModel = rootModel;

                for (var i = 0; i < tokens.Length - 1; i++)
                    (parent, parentModel) = EnsureContainer(parent, parentModel, tokens[i]);

                AttachLeaf(parent, parentModel, tokens[^1], patch);
            }
        }

        /// <summary>중간 경로 노드를 찾고, 없으면 빈 컨테이너(RectTransform+SindyComponent)를 자동 생성한다.</summary>
        private static (SindyComponent, IViewModel) EnsureContainer(SindyComponent parent, IViewModel parentModel, string token)
        {
            var childModel = parentModel?.GetChild<IViewModel>(token);

            if (parent.TryGetView(token, out var existing))
                return (existing, childModel);

            var go = new GameObject(token, typeof(RectTransform));
            go.transform.SetParent(parent.transform, false);
            var container = go.AddComponent<SindyComponent>();
            if (childModel?.Feature<LayoutFeature>() != null)
                go.AddComponent<LayoutFeatureView>();

            parent.AddView(token, container);
            if (childModel != null)
                container.Bind(childModel).SetParent(parent);
            return (container, childModel);
        }

        /// <summary>말단 패치를 부착한다. 키가 이미 존재하면 모델 주입만, 없으면 프리팹을 인스턴스화한다.</summary>
        private static void AttachLeaf(SindyComponent parent, IViewModel parentModel, string token, PatchInstruction patch)
        {
            var childModel = parentModel?.GetChild<IViewModel>(token);

            if (parent.TryGetView(token, out var existing))
            {
                // 하이브리드 규칙: 프리팹에 이미 배치된 키 — 인스턴스화 생략, 모델만 주입
                if (childModel != null)
                    existing.Bind(childModel).SetParent(parent);
                return;
            }

            var prefab = ComponentManager.GetPrefab<SindyComponent>(patch.PrefabName);
            if (prefab == null)
                throw new InvalidOperationException(
                    $"ComponentBlueprint: patch prefab '{patch.PrefabName}' not found. (path: {patch.Path})");

            var child = UnityEngine.Object.Instantiate(prefab, parent.transform, false);
            child.name = prefab.name;
            if (patch.Layout != null && child.GetComponent<LayoutFeatureView>() == null)
                child.gameObject.AddComponent<LayoutFeatureView>();

            parent.AddView(token, child);
            if (childModel != null)
                child.Bind(childModel).SetParent(parent);
        }

        // ── Blueprint 전개 ─────────────────────────────────────────────────────

        private IEnumerable<PatchInstruction> EnumerateExpanded()
        {
            if (baseBlueprint != null)
            {
                baseBlueprint.FlushPendingPatch();
                foreach (var pi in ExpandBlueprint(null, baseBlueprint))
                    yield return pi;
            }

            foreach (var patch in patches)
            {
                yield return patch;
                if (patch.Blueprint != null)
                {
                    patch.Blueprint.FlushPendingPatch();
                    foreach (var pi in ExpandBlueprint(patch.Path, patch.Blueprint))
                        yield return pi;
                }
            }
        }

        private static IEnumerable<PatchInstruction> ExpandBlueprint(string parentPath, ComponentBlueprint blueprint)
        {
            foreach (var entry in blueprint.patches)
            {
                var fullPath = parentPath != null ? $"{parentPath}.{entry.Path}" : entry.Path;

                var pi = entry.Blueprint != null
                    ? new PatchInstruction(fullPath, entry.Blueprint)
                    : new PatchInstruction(fullPath, entry.PrefabName);
                pi.Layout = entry.Layout;
                pi.ModelFactory = entry.ModelFactory;
                yield return pi;

                if (entry.Blueprint != null)
                {
                    entry.Blueprint.FlushPendingPatch();
                    foreach (var child in ExpandBlueprint(fullPath, entry.Blueprint))
                        yield return child;
                }
            }
        }
    }
}
