using System;
using System.Collections.Generic;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// UI 구조를 정의하고 조립하는 빌더 겸 재사용 가능한 템플릿.
    /// 한 번 정의한 Blueprint는 상태를 보존한 채 여러 번 Open() 할 수 있다.
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
    /// </summary>
    public class ComponentBlueprint
    {
        // ── 내부 자료 ──────────────────────────────────────────────────────────

        private readonly string _prefabName;
        private readonly ComponentBlueprint _baseBlueprint;
        private Func<object> _rootModelFactory;

        private readonly List<PatchInstruction> _patches = new();
        private PatchInstruction _pendingPatch;
        private PatchInstruction _lastFlushedPatch;
        private LayoutFeature _rootLayout;

        internal string PrefabName => _prefabName;
        internal LayoutFeature RootLayout => _rootLayout;
        internal Func<object> RootModelFactory => _rootModelFactory;
        internal IReadOnlyList<PatchInstruction> PatchEntries => _patches;

        internal class PatchInstruction
        {
            public readonly string Path;
            public readonly string PrefabName;
            public readonly ComponentBlueprint Blueprint;
            public Func<object> ModelFactory;
            public LayoutFeature Layout;

            public PatchInstruction(string path, string prefabName)
            {
                Path = path;
                PrefabName = prefabName;
            }

            public PatchInstruction(string path, ComponentBlueprint blueprint)
            {
                Path = path;
                PrefabName = blueprint._prefabName;
                Blueprint = blueprint;
            }
        }

        // ── 생성 ───────────────────────────────────────────────────────────────

        private ComponentBlueprint(string prefabName)
        {
            _prefabName = prefabName;
        }

        private ComponentBlueprint(ComponentBlueprint template)
        {
            _baseBlueprint = template;
            _prefabName = template._prefabName;
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
        public ComponentBlueprint WithModel(Func<object> factory)
        {
            if (_pendingPatch != null)
            {
                _pendingPatch.ModelFactory = factory;
                _patches.Add(_pendingPatch);
                _lastFlushedPatch = _pendingPatch;
                _pendingPatch = null;
            }
            else
            {
                _rootModelFactory = factory;
                _lastFlushedPatch = null;
            }
            return this;
        }

        // ── 패치 ───────────────────────────────────────────────────────────────

        /// <summary>경로에 프리팹을 패치한다.</summary>
        public ComponentBlueprint Patch(string path, string prefabName)
        {
            FlushPendingPatch();
            _pendingPatch = new PatchInstruction(path, prefabName);
            _lastFlushedPatch = null;
            return this;
        }

        /// <summary>경로에 Blueprint 구조를 패치한다. 하위 패치가 자동 전개된다.</summary>
        public ComponentBlueprint Patch(string path, ComponentBlueprint blueprint)
        {
            FlushPendingPatch();
            _pendingPatch = new PatchInstruction(path, blueprint);
            _lastFlushedPatch = null;
            return this;
        }

        private void FlushPendingPatch()
        {
            if (_pendingPatch == null) return;
            _patches.Add(_pendingPatch);
            _pendingPatch = null;
        }

        // ── 레이아웃 ───────────────────────────────────────────────────────────

        /// <summary>외부 여백을 지정한다.</summary>
        public ComponentBlueprint Margin(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.MarginTop = top; f.MarginRight = right; f.MarginBottom = bottom; f.MarginLeft = left;
            f.HasMargin = true;
            return this;
        }

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

        private LayoutFeature GetOrCreateCurrentLayout()
        {
            if (_pendingPatch != null)
                return _pendingPatch.Layout ??= new LayoutFeature();
            if (_lastFlushedPatch != null)
                return _lastFlushedPatch.Layout ??= new LayoutFeature();
            return _rootLayout ??= new LayoutFeature();
        }

        // ── 실행 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 인스턴스를 생성하고 모든 패치를 적용한다.
        /// Blueprint 상태는 변경되지 않으므로 여러 번 호출해도 안전하다.
        /// </summary>
        public SindyComponent Open(int layer = 0)
        {
            FlushPendingPatch();

            var prefab = ComponentManager.GetPrefab<SindyComponent>(_prefabName);
            if (prefab == null)
                throw new InvalidOperationException($"ComponentBlueprint: prefab '{_prefabName}' not found.");

            var rootModel = _rootModelFactory?.Invoke()
                            ?? _baseBlueprint?.RootModelFactory?.Invoke();

            if (rootModel is ViewModel viewModel)
            {
                var rootLayoutTemplate = _rootLayout ?? _baseBlueprint?.RootLayout;
                if (rootLayoutTemplate != null)
                    viewModel.With(rootLayoutTemplate.Clone());

                foreach (var patch in EnumerateExpanded())
                {
                    var patchModel = patch.ModelFactory?.Invoke();
                    if (patchModel is IViewModel vm)
                    {
                        if (patch.Layout != null && patchModel is ViewModel patchVM)
                            patchVM.With(patch.Layout.Clone());
                        viewModel.AddChild(patch.Path, vm, disposeWithParent: true);
                    }
                }
            }

            var preset = new ComponentPreset(prefab, rootModel, layer);
            ComponentManager.Open(preset);
            return preset.Component;
        }

        // ── Blueprint 전개 ─────────────────────────────────────────────────────

        private IEnumerable<PatchInstruction> EnumerateExpanded()
        {
            if (_baseBlueprint != null)
            {
                _baseBlueprint.FlushPendingPatch();
                foreach (var pi in ExpandBlueprint(null, _baseBlueprint))
                    yield return pi;
            }

            foreach (var patch in _patches)
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
            foreach (var entry in blueprint._patches)
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
