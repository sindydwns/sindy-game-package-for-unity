using System;
using System.Collections.Generic;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// ComponentManager에 등록된 프리팹을 조립하여 UI를 여는 Builder.
    /// Build() → WithModel() → Patch() → WithModel() → OnLayer() → Open() 순서로 사용한다.
    /// OnLayer()는 레이어만 지정하고, Open() 호출 시 인스턴스가 생성된다.
    /// Open() 없이 Builder를 버릴 경우 Cancel()로 소유 모델을 정리한다.
    /// </summary>
    public class ComponentBuilder
    {
        // ── 내부 자료 ──────────────────────────────────────────────────────────

        private readonly string _rootPrefabName;
        private Func<object> _rootModelFactory;
        private bool _rootDisposeWithComponent;
        private int _layer;

        private readonly List<PatchInstruction> _patches = new();
        private PatchInstruction _pendingPatch; // Patch() 직후, WithModel() 전까지 살아있는 컨텍스트
        private PatchInstruction _lastFlushedPatch; // WithModel() 직후, 다음 Patch() 전까지 레이아웃 대상
        private LayoutFeature _rootLayout;

        private class PatchInstruction
        {
            public readonly string Path;
            public readonly string PrefabName;
            public Func<object> ModelFactory;
            public bool DisposeWithComponent;
            public LayoutFeature Layout;

            public PatchInstruction(string path, string prefabName)
            {
                Path = path;
                PrefabName = prefabName;
            }
        }

        // ── 생성 ───────────────────────────────────────────────────────────────

        private ComponentBuilder(string rootPrefabName)
        {
            _rootPrefabName = rootPrefabName;
        }

        /// <summary>루트 컴포넌트의 프리팹 이름을 지정하고 Builder를 시작한다.</summary>
        public static ComponentBuilder Build(string prefabName) => new ComponentBuilder(prefabName);

        // ── 모델 지정 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 직전 Build() 또는 Patch()에 모델 인스턴스를 지정한다.
        /// disposeWithComponent = true면 Cancel() 시 Dispose 대상이 된다.
        /// </summary>
        public ComponentBuilder WithModel(object model, bool disposeWithComponent = true)
        {
            // 인스턴스를 직접 캐시하는 클로저 — Cancel()이 같은 인스턴스를 Dispose할 수 있다.
            SetModel(() => model, disposeWithComponent);
            return this;
        }

        /// <summary>
        /// 직전 Build() 또는 Patch()에 팩토리로 모델을 지정한다.
        /// Open() 시점에 팩토리가 실행된다. Cancel()은 아직 생성된 인스턴스가 없으므로 아무것도 하지 않는다.
        /// </summary>
        public ComponentBuilder WithModel(Func<object> factory)
        {
            SetModel(factory, disposeWithComponent: false);
            return this;
        }

        private void SetModel(Func<object> factory, bool disposeWithComponent)
        {
            if (_pendingPatch != null)
            {
                _pendingPatch.ModelFactory = factory;
                _pendingPatch.DisposeWithComponent = disposeWithComponent;
                _patches.Add(_pendingPatch);
                _lastFlushedPatch = _pendingPatch;
                _pendingPatch = null;
            }
            else
            {
                _rootModelFactory = factory;
                _rootDisposeWithComponent = disposeWithComponent;
                _lastFlushedPatch = null;
            }
        }

        // ── 패치 ───────────────────────────────────────────────────────────────

        /// <summary>
        /// 루트 ViewModel의 <paramref name="path"/> 경로에 <paramref name="prefabName"/> 프리팹과 함께
        /// 모델을 주입할 패치를 예약한다. 이후 WithModel()로 모델을 지정한다.
        /// </summary>
        public ComponentBuilder Patch(string path, string prefabName)
        {
            FlushPendingPatch();
            _pendingPatch = new PatchInstruction(path, prefabName);
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

        /// <summary>직전 Build/Patch 대상의 외부 여백을 지정한다.</summary>
        public ComponentBuilder Margin(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.MarginTop = top; f.MarginRight = right; f.MarginBottom = bottom; f.MarginLeft = left;
            f.HasMargin = true;
            return this;
        }

        /// <summary>직전 Build/Patch 대상을 컨테이너로 설정하고 자식 배치 방향과 간격을 지정한다.</summary>
        public ComponentBuilder Layout(Direction direction, float spacing = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.LayoutDirection = direction;
            f.Spacing = spacing;
            return this;
        }

        /// <summary>직전 Build/Patch 대상의 내부 여백을 지정한다. 단일 값이면 사방 동일 적용.</summary>
        public ComponentBuilder Padding(float all)
            => Padding(all, all, all, all);

        /// <summary>직전 Build/Patch 대상의 내부 여백을 지정한다.</summary>
        public ComponentBuilder Padding(float top = 0, float right = 0, float bottom = 0, float left = 0)
        {
            var f = GetOrCreateCurrentLayout();
            f.PaddingTop = top; f.PaddingRight = right; f.PaddingBottom = bottom; f.PaddingLeft = left;
            f.HasPadding = true;
            return this;
        }

        /// <summary>직전 Build/Patch 대상의 자식 정렬 기준을 지정한다.</summary>
        public ComponentBuilder Align(TextAnchor anchor)
        {
            GetOrCreateCurrentLayout().Alignment = anchor;
            return this;
        }

        /// <summary>직전 Build/Patch 대상의 선호 크기를 지정한다. -1이면 미지정.</summary>
        public ComponentBuilder Size(float width = -1, float height = -1)
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

        // ── 레이어 지정 ────────────────────────────────────────────────────────

        /// <summary>
        /// UI 레이어를 지정한다. 인스턴스는 생성되지 않는다.
        /// 이후 Open()을 호출해야 실제로 열린다.
        /// </summary>
        public ComponentBuilder OnLayer(int layer)
        {
            _layer = layer;
            return this;
        }

        // ── 실행 / 취소 ────────────────────────────────────────────────────────

        /// <summary>
        /// 설정된 레이어에서 인스턴스를 생성하고 모든 패치를 적용한다.
        /// </summary>
        public SindyComponent Open()
        {
            FlushPendingPatch();

            var prefab = ComponentManager.GetPrefab<SindyComponent>(_rootPrefabName);
            if (prefab == null)
                throw new InvalidOperationException($"ComponentBuilder: prefab '{_rootPrefabName}' not found.");

            var rootModel = _rootModelFactory?.Invoke();

            if (rootModel is ViewModel viewModel)
            {
                if (_rootLayout != null)
                    viewModel.With(_rootLayout);

                foreach (var patch in _patches)
                {
                    var patchModel = patch.ModelFactory?.Invoke();
                    if (patchModel is IViewModel vm)
                    {
                        if (patch.Layout != null && patchModel is ViewModel patchVM)
                            patchVM.With(patch.Layout);
                        viewModel.AddChild(patch.Path, vm, patch.DisposeWithComponent);
                    }
                }
            }

            var preset = new ComponentPreset(prefab, rootModel, _layer);
            ComponentManager.Open(preset);
            return preset.Component;
        }

        /// <summary>
        /// Open() 없이 Builder를 버릴 때 호출한다.
        /// WithModel(instance) 로 등록되고 disposeWithComponent = true 인 모델만 Dispose한다.
        /// WithModel(factory) 로 등록된 모델은 아직 생성되지 않았으므로 Dispose하지 않는다.
        /// </summary>
        public void Cancel()
        {
            FlushPendingPatch();

            DisposeIfOwned(_rootModelFactory, _rootDisposeWithComponent);
            foreach (var patch in _patches)
            {
                DisposeIfOwned(patch.ModelFactory, patch.DisposeWithComponent);
            }

            _rootModelFactory = null;
            _rootLayout = null;
            _lastFlushedPatch = null;
            _patches.Clear();
        }

        private static void DisposeIfOwned(Func<object> factory, bool disposeWithComponent)
        {
            if (!disposeWithComponent || factory == null) return;
            // factory가 캡쳐한 인스턴스를 참조하는 클로저인 경우에만 Dispose.
            // 인스턴스 기반 WithModel()은 () => model 클로저로 저장되어 있으므로
            // Invoke()가 이미 생성된 동일 인스턴스를 반환한다.
            (factory.Invoke() as IDisposable)?.Dispose();
        }
    }
}
