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
    /// 조립 규칙 (Open) — 호출 형태가 의도를 결정한다:
    ///   - Patch(path, prefab) / Patch(path, blueprint): 그 키에 새 인스턴스를 생성한다.
    ///     키가 이미 존재하면 예외(충돌).
    ///   - Patch(path): 그 키의 기존 인스턴스를 재사용해 모델만 주입한다. 키가 없으면 예외.
    ///   - 중간 경로 토큰은 등록된 키로 내려가기만 한다. 없으면 예외 — 자동 생성하지 않는다.
    ///   - "등록된 키" = 프리팹에 미리 배치된 키 또는 같은 Open에서 앞선 Patch가 생성한 키.
    ///   - 같은 경로를 여러 번 패치하면 마지막 선언이 우선하되, 지정하지 않은
    ///     모델 팩토리/레이아웃은 이전 선언에서 승계한다 (파생 Blueprint의 부분 재정의).
    ///   - 형제 순서 = 같은 깊이에서의 패치 선언 순서.
    ///   - PatchEach(path, items, ...): 컬렉션을 컨테이너 키 아래 자식으로 펼쳐 연속 추가한다.
    ///
    /// 배치와 수명:
    ///   - Layout/Padding/Align/Size/Flexible는 자식 배치(LayoutGroup), Anchor/Inset은 노드 자신의 배치(RectTransform 앵커).
    ///     루트에 .Anchor(AnchorPreset.Center, 600, 400)처럼 선언하면 화면 어디에 어떤 크기로 놓일지가 설계도에 남는다.
    ///   - 닫기는 instance.Close() / CloseNextFrame() — 구독 해제 → Bind(null) → 모델 Dispose → Destroy 순서를 고정한다.
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
        private AnchorFeature rootAnchor;

        internal string PrefabName => prefabName;
        internal LayoutFeature RootLayout => rootLayout;
        internal AnchorFeature RootAnchor => rootAnchor;
        internal Func<IViewModel> RootModelFactory => rootModelFactory;
        internal IReadOnlyList<PatchInstruction> PatchEntries => patches;

        internal class PatchInstruction
        {
            public readonly string Path;
            public readonly string PrefabName;
            public readonly ComponentBlueprint Blueprint;
            public Func<IViewModel> ModelFactory;
            public LayoutFeature Layout;
            public AnchorFeature Anchor;

            /// <summary>
            /// 이 패치 모델을 부모 모델의 Dispose 체인에 연결할지 여부. 기본 true.
            /// false면 부모 모델을 Dispose해도 이 모델은 해제되지 않으므로 호출자가 직접 수명을 관리해야 한다.
            /// </summary>
            public bool DisposeWithParent = true;

            /// <summary>재사용 패치 — 프리팹/블루프린트 없이 기존 뷰에 모델만 주입한다.</summary>
            public PatchInstruction(string path)
            {
                Path = path;
            }

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
        ///
        /// <paramref name="disposeWithParent"/>가 true면(기본) 이 패치 모델은 부모(루트) 모델의
        /// Dispose 체인에 연결되어 부모를 Dispose할 때 함께 해제된다. false면 부모를 Dispose해도
        /// 이 모델은 살아남으므로 호출자가 직접 수명을 관리해야 한다(예: 다른 뷰와 공유하는 모델).
        /// 루트 모델 지정 시에는 부모가 없으므로 이 인자는 효과가 없다.
        /// </summary>
        public ComponentBlueprint WithModel(Func<IViewModel> factory, bool disposeWithParent = true)
        {
            if (pendingPatch != null)
            {
                pendingPatch.ModelFactory = factory;
                pendingPatch.DisposeWithParent = disposeWithParent;
                patches.Add(pendingPatch);
                lastFlushedPatch = pendingPatch;
                pendingPatch = null;
            }
            else
            {
                rootModelFactory = factory;
                lastFlushedPatch = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (!disposeWithParent)
                    Debug.LogWarning(
                        $"ComponentBlueprint('{prefabName}'): 루트 모델에는 부모가 없어 " +
                        $"disposeWithParent가 의미 없습니다(무시됨). 패치 모델에만 지정하세요.");
#endif
            }
            return this;
        }

        public ComponentBlueprint WithModel(bool disposeWithParent = true)
            => WithModel(() => new ViewModel(), disposeWithParent);

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

        /// <summary>
        /// 경로의 기존 인스턴스를 재사용한다 — 프리팹을 새로 만들지 않고 모델만 주입한다.
        /// 프리팹에 미리 배치됐거나 앞선 Patch가 생성한 키에 쓴다. 키가 없으면 Open 시 예외.
        /// </summary>
        public ComponentBlueprint Patch(string path)
        {
            FlushPendingPatch();
            pendingPatch = new PatchInstruction(path);
            lastFlushedPatch = null;
            return this;
        }

        // ── 연속 추가 (PatchEach) ───────────────────────────────────────────────

        /// <summary>컬렉션을 단일 프리팹으로 펼쳐 컨테이너 키 아래 자식으로 추가한다(선언 순서 유지).</summary>
        public ComponentBlueprint PatchEach<T>(string path, IEnumerable<T> items, string prefabName, Func<T, IViewModel> model)
            => PatchEach(path, items, _ => prefabName, model);

        /// <summary>컬렉션을 아이템별 프리팹으로 펼쳐 컨테이너 키 아래 자식으로 추가한다(이질 유닛, 선언 순서 유지).</summary>
        public ComponentBlueprint PatchEach<T>(string path, IEnumerable<T> items, Func<T, string> prefabSelector, Func<T, IViewModel> model)
        {
            FlushPendingPatch();
            var i = 0;
            foreach (var item in items)
            {
                var captured = item;
                patches.Add(new PatchInstruction($"{path}.{i}", prefabSelector(captured))
                {
                    ModelFactory = () => model(captured),
                });
                i++;
            }
            lastFlushedPatch = null;
            return this;
        }

        /// <summary>컬렉션을 아이템별 서브 블루프린트로 펼쳐 컨테이너 키 아래 자식으로 추가한다(선언 순서 유지).</summary>
        public ComponentBlueprint PatchEach<T>(string path, IEnumerable<T> items, Func<T, ComponentBlueprint> blueprintSelector, Func<T, IViewModel> model)
        {
            FlushPendingPatch();
            var i = 0;
            foreach (var item in items)
            {
                var captured = item;
                patches.Add(new PatchInstruction($"{path}.{i}", blueprintSelector(captured))
                {
                    ModelFactory = () => model(captured),
                });
                i++;
            }
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

        // ── 앵커 (노드 자신의 배치) ────────────────────────────────────────────

        /// <summary>
        /// 노드 자신이 부모 안 어디에 놓일지를 프리셋으로 지정한다 — 주로 루트에 쓴다
        /// (중앙 다이얼로그 = Center, 바텀시트 = BottomStretch, 전체 페이지 = Stretch).
        /// 점 고정 축의 크기는 <paramref name="width"/>/<paramref name="height"/>로 주고, -1이면 프리팹 크기를 유지한다.
        /// 부모에 LayoutGroup이 있는 노드에는 무효다(LayoutGroup이 덮어씀) — 그 경우 Size/Flexible을 쓴다.
        /// </summary>
        public ComponentBlueprint Anchor(AnchorPreset preset, float width = -1, float height = -1)
        {
            GetOrCreateCurrentAnchor().Anchor(preset, width, height);
            return this;
        }

        /// <summary>정규화 좌표(0~1)로 앵커 사각형을 직접 지정한다 — 예: Anchor(new Vector2(0.06f, 0.22f), new Vector2(0.94f, 0.78f)).</summary>
        public ComponentBlueprint Anchor(Vector2 anchorMin, Vector2 anchorMax)
        {
            GetOrCreateCurrentAnchor().Anchor(anchorMin, anchorMax);
            return this;
        }

        /// <summary>가장자리 여백을 지정한다 (사방 동일). 늘림 축은 양 끝을 줄이고, 점 고정 축은 붙은 변에서 안쪽으로 민다.</summary>
        public ComponentBlueprint Inset(float all)
            => Inset(all, all, all, all);

        /// <summary>가장자리 여백을 지정한다. 늘림 축은 양 끝을 줄이고, 점 고정 축은 붙은 변에서 안쪽으로 민다.</summary>
        public ComponentBlueprint Inset(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            GetOrCreateCurrentAnchor().Inset(top, right, bottom, left);
            return this;
        }

        private AnchorFeature GetOrCreateCurrentAnchor()
        {
            if (pendingPatch != null)
                return pendingPatch.Anchor ??= new AnchorFeature();
            if (lastFlushedPatch != null)
                return lastFlushedPatch.Anchor ??= new AnchorFeature();
            return rootAnchor ??= new AnchorFeature();
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

            EnsureLayoutView(instance, rootLayout ?? baseBlueprint?.RootLayout);
            EnsureAnchorView(instance, rootAnchor ?? baseBlueprint?.RootAnchor);

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
                // 닫기 순서(구독 해제 → Bind(null) → 모델 Dispose → Destroy)는 Close가 고정한다.
                if (instance != null)
                    instance.Close(disposeOld);
                else if (disposeOld)
                    (old as IDisposable)?.Dispose();
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
                    ApplyBlueprintFeature(viewModel, rootLayoutTemplate.Clone(), "(root)", prefabName);
                var rootAnchorTemplate = rootAnchor ?? baseBlueprint?.RootAnchor;
                if (rootAnchorTemplate != null)
                    ApplyBlueprintFeature(viewModel, rootAnchorTemplate.Clone(), "(root)", prefabName);

                foreach (var patch in patches)
                {
                    // 모델 팩토리가 없는 패치(구조 전용 부품)는 빈 ViewModel로 자리를 만든다 —
                    // 뷰 조립과 Dispose 체인이 모델 트리와 1:1로 유지되도록.
                    var patchModel = patch.ModelFactory?.Invoke() ?? new ViewModel();
                    if (patch.Layout != null || patch.Anchor != null)
                    {
                        if (patchModel is ViewModel patchVM)
                        {
                            if (patch.Layout != null)
                                ApplyBlueprintFeature(patchVM, patch.Layout.Clone(), patch.Path, prefabName);
                            if (patch.Anchor != null)
                                ApplyBlueprintFeature(patchVM, patch.Anchor.Clone(), patch.Path, prefabName);
                        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                        else
                            Debug.LogWarning(
                                $"ComponentBlueprint('{prefabName}'): 패치 '{patch.Path}'의 모델이 ViewModel이 아니어서 " +
                                $"Layout/Padding/Size/Anchor 지정이 무시됩니다. ({patchModel.GetType().Name})");
#endif
                    }
                    viewModel.AddChild(patch.Path, patchModel, patch.DisposeWithParent);
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
        /// Blueprint의 디자인 Feature(Layout/Anchor) 클론을 모델에 부착한다.
        /// 모델 팩토리가 이미 같은 Feature를 넣어둔 경우 Blueprint가 덮어쓰므로 경고한다 —
        /// 디자인은 Blueprint 체인에, 기능은 모델에 두는 분리 원칙 위반의 가시화.
        /// </summary>
        private static void ApplyBlueprintFeature<TFeature>(ViewModel target, TFeature clone, string path, string prefabName)
            where TFeature : ModelFeature
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (target.Feature<TFeature>() != null)
                Debug.LogWarning(
                    $"ComponentBlueprint('{prefabName}'): '{path}' 모델에 이미 {typeof(TFeature).Name}가 있어 " +
                    $"Blueprint의 선언이 이를 덮어씁니다. 디자인 선언을 한 곳으로 모으세요.");
#endif
            target.With(clone);
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
                var inheritLayout = patch.Blueprint != null && patch.Layout == null && patch.Blueprint.RootLayout != null;
                var inheritAnchor = patch.Blueprint != null && patch.Anchor == null && patch.Blueprint.RootAnchor != null;
                if (inheritLayout || inheritAnchor)
                {
                    patch = new PatchInstruction(raw.Path, raw.Blueprint)
                    {
                        ModelFactory = raw.ModelFactory,
                        Layout = inheritLayout ? raw.Blueprint.RootLayout : raw.Layout,
                        Anchor = inheritAnchor ? raw.Blueprint.RootAnchor : raw.Anchor,
                        DisposeWithParent = raw.DisposeWithParent,
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
                    merged.Anchor = patch.Anchor ?? prev.Anchor;
                    // DisposeWithParent는 모델 팩토리에 딸린 속성이므로 '채택된 팩토리'를 따라가야 한다.
                    //
                    // 위에서 ModelFactory는 null이면 이전 것을 승계한다(?? prev). 하지만 DisposeWithParent는
                    // bool(기본 true)이라 "지정 안 함"과 "true로 지정함"을 구분할 수 없다. 그래서 새 패치가
                    // 모델을 실제로 줬는지(ModelFactory != null)를 신호로 삼아 플래그가 팩토리와 함께 움직이게 한다.
                    //   - 새 모델을 줬으면        → 그 새 모델의 플래그(patch)를 쓴다.
                    //   - 모델을 안 주고 레이아웃 등만 바꿨으면 → 승계한 팩토리의 플래그(prev)를 그대로 쓴다.
                    //
                    // 이 가드가 없으면, 모델을 건드리지 않은 파생 패치의 기본값 true가 승계된 팩토리의
                    // 의도(예: 공유 모델이라 false)를 덮어써 원치 않는 연쇄 Dispose가 발생한다.
                    // 예) Base: Patch("item","cell").WithModel(()=>shared, disposeWithParent:false)
                    //     Derived(Base 파생): Patch("item","cell").Layout(...)   // WithModel 호출 없음
                    //     → patch.ModelFactory == null 이므로 prev의 false를 유지한다.
                    merged.DisposeWithParent = patch.ModelFactory != null
                        ? patch.DisposeWithParent
                        : prev.DisposeWithParent;
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
        /// 루트 인스턴스에 패치들을 적용한다 — 중간 경로는 등록된 키로 내려가고(없으면 예외),
        /// 말단은 호출 형태에 따라 생성(Patch(path, prefab)) 또는 재사용(Patch(path))한다.
        /// </summary>
        private static void AssembleViews(SindyComponent rootInstance, ViewModel rootModel, List<PatchInstruction> patches)
        {
            foreach (var patch in patches)
            {
                var tokens = patch.Path.Split('.', StringSplitOptions.RemoveEmptyEntries);
                var parent = rootInstance;
                IViewModel parentModel = rootModel;

                for (var i = 0; i < tokens.Length - 1; i++)
                    (parent, parentModel) = ResolveContainer(parent, parentModel, tokens[i], patch.Path);

                AttachLeaf(parent, parentModel, tokens[^1], patch);
            }
        }

        /// <summary>
        /// 중간 경로 노드를 등록된 키에서 찾아 내려간다. 없으면 예외 — 자동 생성하지 않는다.
        /// (이 경로를 먼저 Patch로 생성해야 한다.)
        /// </summary>
        private static (SindyComponent, IViewModel) ResolveContainer(
            SindyComponent parent, IViewModel parentModel, string token, string fullPath)
        {
            if (parent.TryGetView(token, out var existing))
                return (existing, parentModel?.GetChild<IViewModel>(token));

            throw new InvalidOperationException(
                $"ComponentBlueprint: 중간 경로 '{token}'가 등록되어 있지 않습니다. " +
                $"이 경로를 먼저 Patch로 생성하세요. (path: {fullPath})");
        }

        /// <summary>
        /// 말단 패치를 부착한다 — 호출 형태가 동작을 결정한다.
        /// 프리팹/블루프린트가 있으면 새로 인스턴스화하고(키 충돌 시 예외),
        /// 없으면(Patch(path)) 기존 인스턴스를 재사용해 모델만 주입한다(키 없으면 예외).
        /// </summary>
        private static void AttachLeaf(SindyComponent parent, IViewModel parentModel, string token, PatchInstruction patch)
        {
            var childModel = parentModel?.GetChild<IViewModel>(token);
            var creates = !string.IsNullOrEmpty(patch.PrefabName) || patch.Blueprint != null;

            if (!creates)
            {
                // 재사용: Patch(path) — 기존 뷰에 모델만 주입
                if (!parent.TryGetView(token, out var existing))
                    throw new InvalidOperationException(
                        $"ComponentBlueprint: 재사용할 뷰 '{token}'가 없습니다. " +
                        $"새로 생성하려면 Patch(\"{patch.Path}\", prefab)을 사용하세요. (path: {patch.Path})");
                EnsureLayoutView(existing, patch.Layout);
                EnsureAnchorView(existing, patch.Anchor);
                if (childModel != null)
                    existing.Bind(childModel).SetParent(parent);
                return;
            }

            // 생성: Patch(path, prefab|blueprint) — 키가 이미 있으면 충돌 예외
            if (parent.TryGetView(token, out _))
                throw new InvalidOperationException(
                    $"ComponentBlueprint: 키 '{token}'가 이미 존재합니다. " +
                    $"모델만 주입하려면 인자 없이 Patch(\"{patch.Path}\")를 사용하세요. (path: {patch.Path})");

            var prefab = ComponentManager.GetPrefab<SindyComponent>(patch.PrefabName);
            if (prefab == null)
                throw new InvalidOperationException(
                    $"ComponentBlueprint: patch prefab '{patch.PrefabName}' not found. (path: {patch.Path})");

            var child = UnityEngine.Object.Instantiate(prefab, parent.transform, false);
            child.name = $"{token} ({prefab.name})";
            EnsureLayoutView(child, patch.Layout);
            EnsureAnchorView(child, patch.Anchor);

            parent.AddView(token, child);
            if (childModel != null)
                child.Bind(childModel).SetParent(parent);
        }

        /// <summary>
        /// 패치가 레이아웃을 선언했으면 대상 뷰에 LayoutFeatureView를 보장한다(없으면 부착).
        /// 생성·재사용·루트 어느 경로든 동일하게 Layout 선언이 화면에 반영되도록 한다.
        /// </summary>
        private static void EnsureLayoutView(SindyComponent view, LayoutFeature layout)
        {
            if (layout != null && view.GetComponent<LayoutFeatureView>() == null)
                view.gameObject.AddComponent<LayoutFeatureView>();
        }

        /// <summary>패치가 앵커를 선언했으면 대상 뷰에 AnchorFeatureView를 보장한다(없으면 부착).</summary>
        private static void EnsureAnchorView(SindyComponent view, AnchorFeature anchor)
        {
            if (anchor != null && view.GetComponent<AnchorFeatureView>() == null)
                view.gameObject.AddComponent<AnchorFeatureView>();
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
                pi.Anchor = entry.Anchor;
                pi.ModelFactory = entry.ModelFactory;
                pi.DisposeWithParent = entry.DisposeWithParent;
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
