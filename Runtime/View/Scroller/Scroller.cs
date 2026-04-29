using System;
using System.Collections;
using System.Collections.Generic;
using Sindy.Easing;
using Sindy.Reactive;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// Sindy 가상화 스크롤러. SRS v1.0의 FR-* 요구사항을 구현한다.
    /// 단일 세로 스크롤(CON-01), 다수 섹션 적층(FR-SEC-01), 그리드 자동 산출(FR-GRID-*),
    /// prefab 단위 풀(FR-POOL-*), ObservableList 5종 이벤트 처리(FR-DATA-02, 8장),
    /// 빈 섹션 처리(FR-EMPTY-*), Easing 기반 스크롤 점프(FR-SCROLL-*).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class Scroller : MonoBehaviour
    {
        [Header("UI Wiring")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform poolRoot;

        [Header("Virtualization")]
        [Tooltip("뷰포트 위/아래로 추가 인스턴스화하는 버퍼 픽셀 수.")]
        [SerializeField] private float overscan = 100f;

        [Header("Defaults (FR-SCROLL-03)")]
        [SerializeField] private ScrollAlignment defaultAlignment = ScrollAlignment.Top;
        [SerializeField] private bool defaultAnimated = false;
        [SerializeField] private float defaultDuration = 0.3f;

        public ScrollAlignment DefaultAlignment { get => defaultAlignment; set => defaultAlignment = value; }
        public bool DefaultAnimated { get => defaultAnimated; set => defaultAnimated = value; }
        public float DefaultDuration { get => defaultDuration; set => defaultDuration = value; }
        public EaseFunction DefaultEase { get; set; } = Ease.OutCubic;

        // ───────── Cell type registry (FR-CELL-*) ─────────

        private readonly CellTypeRegistry registry = new();

        public static void RegisterGlobalCellType<TVM>(SindyComponent prefab) where TVM : class
            => CellTypeRegistry.RegisterGlobal(typeof(TVM), prefab);

        public void RegisterCellType<TVM>(SindyComponent prefab) where TVM : class
            => registry.Register(typeof(TVM), prefab);

        // ───────── Pool (FR-POOL-*) ─────────

        private ViewComponentPool pool;

        public void PrewarmPool(SindyComponent prefab, int count) => EnsurePool().Prewarm(prefab, count);
        public void PrewarmPool<TVM>(SindyComponent prefab, int count) where TVM : class => PrewarmPool(prefab, count);

        private ViewComponentPool EnsurePool()
        {
            if (pool == null)
            {
                if (poolRoot == null) poolRoot = CreateChildRect("__Pool", false);
                pool = new ViewComponentPool(poolRoot, content);
            }
            return pool;
        }

        private RectTransform CreateChildRect(string name, bool active)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            go.SetActive(active);
            return (RectTransform)go.transform;
        }

        // ───────── Sections ─────────

        private readonly List<ISection> sections = new();
        private SectionLayout[] layouts = Array.Empty<SectionLayout>();
        private readonly Dictionary<CellKey, ActiveCell> active = new();
        private readonly HashSet<CellKey> needed = new();

        private float totalContentHeight;
        private float lastContainerWidth = -1f;
        private bool layoutDirty;

        /// <summary>
        /// FR-SEC-05. 섹션 컬렉션을 한 번에 교체한다. 단위 추가/제거 API는 제공하지 않는다.
        /// </summary>
        public void SetSections(IEnumerable<ISection> newSections)
        {
            DetachListeners();
            ReleaseAllActive();

            sections.Clear();
            if (newSections != null)
            {
                foreach (var s in newSections)
                    if (s != null) sections.Add(s);
            }

            layouts = sections.Count == 0 ? Array.Empty<SectionLayout>() : new SectionLayout[sections.Count];

            AttachListeners();
            ResolveAllPrefabs();
            InvalidateLayout();
        }

        private readonly List<Action<ListChange<object>>> sectionHandlers = new();

        private void AttachListeners()
        {
            sectionHandlers.Clear();
            for (var i = 0; i < sections.Count; i++)
            {
                var idx = i;
                Action<ListChange<object>> h = e => OnSectionChanged(idx, e);
                sectionHandlers.Add(h);
                sections[i].OnContentChanged += h;
                sections[i].AttachListener();
            }
        }

        private void DetachListeners()
        {
            for (var i = 0; i < sections.Count && i < sectionHandlers.Count; i++)
            {
                sections[i].OnContentChanged -= sectionHandlers[i];
                sections[i].DetachListener();
            }
            sectionHandlers.Clear();
        }

        // ───────── Unity callbacks ─────────

        protected virtual void Awake()
        {
            if (scrollRect == null) scrollRect = GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                if (viewport == null) viewport = scrollRect.viewport;
                if (content == null) content = scrollRect.content;
            }
        }

        protected virtual void OnEnable()
        {
            if (scrollRect != null) scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        protected virtual void OnDisable()
        {
            if (scrollRect != null) scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
            StopScrollAnimation();
        }

        protected virtual void OnDestroy()
        {
            DetachListeners();
            // 풀 인스턴스는 poolRoot/Content의 자식이므로 GameObject가 파괴되며 함께 정리된다 (FR-POOL-06).
        }

        protected virtual void LateUpdate()
        {
            if (viewport == null || content == null) return;

            var width = viewport.rect.width;
            // FR-GRID-03. 컨테이너 너비가 변경되어 컬럼 수가 달라질 때만 레이아웃을 재계산.
            if (!Mathf.Approximately(width, lastContainerWidth))
            {
                if (WouldColumnCountChange(width)) InvalidateLayout();
                lastContainerWidth = width;
            }

            if (layoutDirty)
            {
                RecomputeLayout(width);
                layoutDirty = false;
            }

            UpdateVisibleCells();
        }

        private void OnScrollChanged(Vector2 _) => UpdateVisibleCells();

        // ───────── Prefab resolution (FR-CELL-03 / 05) ─────────

        private void ResolveAllPrefabs()
        {
            // 모든 섹션의 prefab을 미리 해상도 → 등록 누락은 첫 표시 전에 발견된다 (FR-CELL-04).
            for (var i = 0; i < sections.Count; i++)
            {
                ref var L = ref layouts[i];
                var s = sections[i];
                var opt = s.Option;

                L.ContentPrefab = registry.Resolve(s.ContentVMType, opt.ContentPrefab);
                L.HeaderPrefab = s.Header != null ? registry.Resolve(s.Header.GetType(), opt.HeaderPrefab) : null;
                L.FooterPrefab = s.Footer != null ? registry.Resolve(s.Footer.GetType(), opt.FooterPrefab) : null;
                L.EmptyPrefab = s.EmptyContent != null ? registry.Resolve(s.EmptyContent.GetType(), opt.EmptyContentPrefab) : null;
            }
        }

        // ───────── Layout (FR-EMPTY-01/02/03 + FR-HEIGHT-* + FR-SEC-04) ─────────

        public void InvalidateLayout() => layoutDirty = true;

        private void RecomputeLayout(float containerWidth)
        {
            float yCursor = 0f;

            for (var i = 0; i < sections.Count; i++)
            {
                ref var L = ref layouts[i];
                var s = sections[i];
                var opt = s.Option;

                var hasHeader = s.Header != null && L.HeaderPrefab != null;
                var hasFooter = s.Footer != null && L.FooterPrefab != null;
                var hasEmpty = s.EmptyContent != null && L.EmptyPrefab != null;
                var contentCount = s.ContentCount;

                // FR-EMPTY-01 / 부록 B. 헤더·푸터는 존재하면 항상 표시 (콘텐츠가 0개일 때도).
                L.ShowHeader = hasHeader;
                L.ShowFooter = hasFooter;
                L.ShowEmpty = contentCount == 0 && hasEmpty;

                // FR-EMPTY-02. 콘텐츠 0 + 헤더/푸터/빈 콘텐츠 모두 없음 → 섹션 통째 비표시 (마진 포함).
                L.IsVisible = contentCount > 0 || hasHeader || hasFooter || hasEmpty;

                if (!L.IsVisible)
                {
                    L.TopY = yCursor;
                    L.TopMargin = 0;
                    L.BottomMargin = 0;
                    L.HeaderHeight = 0;
                    L.ContentHeight = 0;
                    L.FooterHeight = 0;
                    L.TotalHeight = 0;
                    L.RowCount = 0;
                    continue;
                }

                // FR-SEC-04. 인접 표시 섹션 사이의 (위쪽 bottomMargin + 아래쪽 topMargin) 자연 가산은
                // yCursor에 각 섹션의 두 마진을 누적하는 것으로 자동 달성된다.
                // 비표시 섹션은 yCursor에 기여하지 않으므로 마진도 사라진다 (FR-EMPTY-02).
                L.TopMargin = opt.TopMargin;
                L.BottomMargin = opt.BottomMargin;

                L.HeaderHeight = L.ShowHeader ? GetPrefabHeight(L.HeaderPrefab) : 0f;
                L.FooterHeight = L.ShowFooter ? GetPrefabHeight(L.FooterPrefab) : 0f;

                if (contentCount == 0)
                {
                    if (L.ShowEmpty)
                    {
                        // FR-EMPTY-03. 빈 콘텐츠 prefab의 RectTransform 높이가 곧 콘텐츠 영역 높이.
                        L.EmptyPrefabSize = GetPrefabSize(L.EmptyPrefab);
                        L.ContentHeight = L.EmptyPrefabSize.y;
                    }
                    else
                    {
                        L.ContentHeight = 0f;
                    }
                    L.RowCount = 0;
                }
                else
                {
                    L.Grid = GridLayoutResolver.Resolve(containerWidth, opt);
                    L.CellHeight = GetPrefabHeight(L.ContentPrefab);
                    L.RowCount = GridLayoutResolver.RowCount(contentCount, L.Grid.Columns);
                    L.ContentHeight = L.RowCount * L.CellHeight + Mathf.Max(0, L.RowCount - 1) * opt.VerticalGap;
                }

                L.TopY = yCursor + L.TopMargin;
                yCursor += L.TopMargin + L.HeaderHeight + L.ContentHeight + L.FooterHeight + L.BottomMargin;
                L.TotalHeight = L.HeaderHeight + L.ContentHeight + L.FooterHeight;
            }

            totalContentHeight = yCursor;
            content.sizeDelta = new Vector2(content.sizeDelta.x, totalContentHeight);
        }

        private bool WouldColumnCountChange(float newWidth)
        {
            for (var i = 0; i < sections.Count; i++)
            {
                if (!layouts[i].IsVisible || sections[i].ContentCount == 0) continue;
                var grid = GridLayoutResolver.Resolve(newWidth, sections[i].Option);
                if (grid.Columns != layouts[i].Grid.Columns) return true;
            }
            return false;
        }

        private static float GetPrefabHeight(SindyComponent prefab)
        {
            if (prefab == null) return 0f;
            var rt = prefab.transform as RectTransform;
            return rt != null ? rt.rect.height : 0f;
        }

        private static Vector2 GetPrefabSize(SindyComponent prefab)
        {
            if (prefab == null) return Vector2.zero;
            var rt = prefab.transform as RectTransform;
            return rt != null ? rt.rect.size : Vector2.zero;
        }

        // ───────── Virtualization (FR-POOL-01/02 + 8장) ─────────

        private float ScrollY => content != null ? content.anchoredPosition.y : 0f;
        private float ViewportHeight => viewport != null ? viewport.rect.height : 0f;

        private void UpdateVisibleCells()
        {
            if (sections.Count == 0) { ReleaseAllActive(); return; }

            var top = ScrollY - overscan;
            var bottom = ScrollY + ViewportHeight + overscan;

            needed.Clear();
            for (var i = 0; i < sections.Count; i++) CollectNeeded(i, top, bottom);
            ReleaseUnneeded();
            AcquireMissing();
        }

        private void CollectNeeded(int sectionIndex, float top, float bottom)
        {
            ref var L = ref layouts[sectionIndex];
            if (!L.IsVisible) return;

            var sectionTop = L.TopY;
            var sectionBottom = L.TopY + L.TotalHeight;
            if (sectionBottom < top || sectionTop > bottom) return;

            if (L.ShowHeader && OverlapsRange(L.HeaderTopY, L.HeaderHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.HeaderSlot));

            if (L.ShowFooter && OverlapsRange(L.FooterTopY, L.FooterHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.FooterSlot));

            if (L.ShowEmpty && OverlapsRange(L.ContentTopY, L.ContentHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.EmptySlot));

            if (L.RowCount > 0)
            {
                var contentBottom = L.ContentTopY + L.ContentHeight;
                if (contentBottom < top || L.ContentTopY > bottom) return;

                var rowStride = L.CellHeight + sections[sectionIndex].Option.VerticalGap;
                var firstRow = Mathf.Max(0, Mathf.FloorToInt((top - L.ContentTopY) / Mathf.Max(0.0001f, rowStride)));
                var lastRow = Mathf.Min(L.RowCount - 1, Mathf.FloorToInt((bottom - L.ContentTopY) / Mathf.Max(0.0001f, rowStride)));

                var count = sections[sectionIndex].ContentCount;
                for (var r = firstRow; r <= lastRow; r++)
                {
                    for (var c = 0; c < L.Grid.Columns; c++)
                    {
                        var idx = r * L.Grid.Columns + c;
                        if (idx >= count) break;
                        needed.Add(new CellKey(sectionIndex, idx));
                    }
                }
            }
        }

        private static bool OverlapsRange(float y, float h, float top, float bottom)
            => y + h > top && y < bottom;

        private void ReleaseUnneeded()
        {
            // 활성 셀 중 needed에 없는 것을 풀로 반환.
            List<CellKey> toRemove = null;
            foreach (var kv in active)
            {
                if (!needed.Contains(kv.Key))
                {
                    (toRemove ??= new List<CellKey>()).Add(kv.Key);
                }
            }
            if (toRemove == null) return;
            foreach (var k in toRemove)
            {
                var cell = active[k];
                EnsurePool().Release(cell.Prefab, cell.Instance);
                active.Remove(k);
            }
        }

        private void AcquireMissing()
        {
            foreach (var key in needed)
            {
                if (active.ContainsKey(key))
                {
                    PositionCell(key, active[key].Instance);
                    continue;
                }

                var prefab = ResolvePrefabFor(key);
                if (prefab == null) continue;
                var inst = EnsurePool().Acquire(prefab);
                active[key] = new ActiveCell { Instance = inst, Prefab = prefab };
                BindCell(key, inst);
                PositionCell(key, inst);
            }
        }

        private SindyComponent ResolvePrefabFor(CellKey k)
        {
            ref var L = ref layouts[k.Section];
            return k.Slot switch
            {
                CellKey.HeaderSlot => L.HeaderPrefab,
                CellKey.FooterSlot => L.FooterPrefab,
                CellKey.EmptySlot => L.EmptyPrefab,
                _ => L.ContentPrefab,
            };
        }

        private void BindCell(CellKey k, SindyComponent inst)
        {
            var s = sections[k.Section];
            object vm = k.Slot switch
            {
                CellKey.HeaderSlot => s.Header,
                CellKey.FooterSlot => s.Footer,
                CellKey.EmptySlot => s.EmptyContent,
                _ => s.GetContentVMAt(k.Slot),
            };
            inst.SetModel(vm);
        }

        private void PositionCell(CellKey k, SindyComponent inst)
        {
            ref var L = ref layouts[k.Section];
            var rt = inst.transform as RectTransform;
            if (rt == null) return;

            // 모든 셀: top-left 앵커, top-left 피벗으로 통일.
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            var opt = sections[k.Section].Option;
            var paddingLeft = opt.HorizontalPadding != null ? opt.HorizontalPadding.left : 0;
            var paddingRight = opt.HorizontalPadding != null ? opt.HorizontalPadding.right : 0;
            var fullWidth = Mathf.Max(0f, lastContainerWidth - paddingLeft - paddingRight);

            float x, y, w, h;
            switch (k.Slot)
            {
                case CellKey.HeaderSlot:
                    x = paddingLeft; y = L.HeaderTopY; w = fullWidth; h = L.HeaderHeight; break;
                case CellKey.FooterSlot:
                    x = paddingLeft; y = L.FooterTopY; w = fullWidth; h = L.FooterHeight; break;
                case CellKey.EmptySlot:
                    // FR-EMPTY-03. 가로/세로 중앙 배치.
                    w = L.EmptyPrefabSize.x;
                    h = L.EmptyPrefabSize.y;
                    x = paddingLeft + (fullWidth - w) * 0.5f;
                    y = L.ContentTopY + (L.ContentHeight - h) * 0.5f;
                    break;
                default:
                    var col = k.Slot % L.Grid.Columns;
                    var row = k.Slot / L.Grid.Columns;
                    x = L.Grid.CellX(col);
                    y = L.ContentTopY + row * (L.CellHeight + opt.VerticalGap);
                    w = L.Grid.CellWidth;
                    h = L.CellHeight;
                    break;
            }

            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -y);
        }

        private void ReleaseAllActive()
        {
            if (active.Count == 0) return;
            foreach (var kv in active)
            {
                EnsurePool().Release(kv.Value.Prefab, kv.Value.Instance);
            }
            active.Clear();
        }

        // ───────── Data change handling (8장) ─────────

        private void OnSectionChanged(int sectionIndex, ListChange<object> e)
        {
            // FR-DATA-03. Move는 Remove + Insert의 합성으로 처리한다.
            // FR-HEIGHT-02에 의해 셀 높이가 고정이므로 Replace는 레이아웃 재계산 없이 바인딩만 갱신해도 충분하지만,
            // 프로토타입은 단순성을 위해 영향받는 섹션의 레이아웃만 invalidate한다.
            switch (e.Action)
            {
                case ListChangeAction.Replace:
                    // 셀 높이 불변(FR-HEIGHT-02) → 레이아웃 재계산 불필요.
                    // 화면 안에 있으면 같은 인스턴스에 새 VM을 다시 바인딩한다 (8.3).
                    var key = new CellKey(sectionIndex, e.NewIndex);
                    if (active.TryGetValue(key, out var cell))
                    {
                        cell.Instance.SetModel(e.NewItem);
                    }
                    return;

                case ListChangeAction.Add:
                case ListChangeAction.Remove:
                case ListChangeAction.Move:
                case ListChangeAction.Reset:
                    // 영향받는 섹션 이후의 모든 y좌표가 변할 수 있으므로 레이아웃 재계산.
                    // 이 섹션의 활성 콘텐츠 셀은 인덱스 변동의 안전을 위해 일괄 회수한다.
                    ReleaseSectionContentCells(sectionIndex);
                    InvalidateLayout();
                    break;
            }
        }

        private void ReleaseSectionContentCells(int sectionIndex)
        {
            List<CellKey> toRemove = null;
            foreach (var kv in active)
            {
                if (kv.Key.Section == sectionIndex && kv.Key.Slot >= 0)
                {
                    (toRemove ??= new List<CellKey>()).Add(kv.Key);
                }
            }
            if (toRemove == null) return;
            foreach (var k in toRemove)
            {
                var cell = active[k];
                EnsurePool().Release(cell.Prefab, cell.Instance);
                active.Remove(k);
            }
        }

        // ───────── ScrollTo (FR-SCROLL-*) ─────────

        public void ScrollToTop(bool? animated = null, float? duration = null, EaseFunction ease = default)
            => ScrollToY(0f, animated, duration, ease);

        public void ScrollToBottom(bool? animated = null, float? duration = null, EaseFunction ease = default)
        {
            ForceLayoutNow();
            ScrollToY(Mathf.Max(0f, totalContentHeight - ViewportHeight), animated, duration, ease);
        }

        public void ScrollTo(int sectionIndex, int itemIndex = -1,
            ScrollAlignment? alignment = null, bool? animated = null, float? duration = null, EaseFunction ease = default)
        {
            ForceLayoutNow();
            if (sectionIndex < 0 || sectionIndex >= sections.Count)
                throw new ArgumentOutOfRangeException(nameof(sectionIndex));

            var (y, h) = GetTargetRange(sectionIndex, itemIndex);
            ScrollToTarget(y, h, alignment, animated, duration, ease);
        }

        public void ScrollTo(ISection section, int itemIndex = -1,
            ScrollAlignment? alignment = null, bool? animated = null, float? duration = null, EaseFunction ease = default)
        {
            var idx = sections.IndexOf(section);
            if (idx < 0) throw new ArgumentException("Section not present in this scroller.", nameof(section));
            ScrollTo(idx, itemIndex, alignment, animated, duration, ease);
        }

        public void ScrollTo(ISection section, object vm,
            ScrollAlignment? alignment = null, bool? animated = null, float? duration = null, EaseFunction ease = default)
        {
            var idx = sections.IndexOf(section);
            if (idx < 0) throw new ArgumentException("Section not present in this scroller.", nameof(section));
            var itemIndex = section.IndexOfContentVM(vm);
            if (itemIndex < 0) throw new ArgumentException("VM not found in section content.", nameof(vm));
            ScrollTo(idx, itemIndex, alignment, animated, duration, ease);
        }

        public void ScrollTo(object vm,
            ScrollAlignment? alignment = null, bool? animated = null, float? duration = null, EaseFunction ease = default)
        {
            for (var i = 0; i < sections.Count; i++)
            {
                var idx = sections[i].IndexOfContentVM(vm);
                if (idx >= 0)
                {
                    ScrollTo(i, idx, alignment, animated, duration, ease);
                    return;
                }
            }
            throw new ArgumentException("VM not found in any section content.", nameof(vm));
        }

        private void ForceLayoutNow()
        {
            if (viewport == null || content == null) return;
            var width = viewport.rect.width;
            if (lastContainerWidth < 0f) lastContainerWidth = width;
            if (layoutDirty || sectionsLayoutsLengthMismatch())
            {
                RecomputeLayout(width);
                layoutDirty = false;
            }
        }

        private bool sectionsLayoutsLengthMismatch() => layouts.Length != sections.Count;

        private (float y, float h) GetTargetRange(int sectionIndex, int itemIndex)
        {
            ref var L = ref layouts[sectionIndex];
            if (!L.IsVisible) return (L.TopY, 0f);

            if (itemIndex < 0)
            {
                // 섹션 시작 = 섹션 마진을 포함한 시작 y
                return (L.TopY, L.TotalHeight + L.TopMargin);
            }
            var s = sections[sectionIndex];
            if (itemIndex >= s.ContentCount)
                throw new ArgumentOutOfRangeException(nameof(itemIndex));

            var row = itemIndex / Mathf.Max(1, L.Grid.Columns);
            var y = L.ContentTopY + row * (L.CellHeight + s.Option.VerticalGap);
            return (y, L.CellHeight);
        }

        private void ScrollToTarget(float y, float h,
            ScrollAlignment? alignment, bool? animated, float? duration, EaseFunction ease)
        {
            var align = alignment ?? defaultAlignment;
            var target = align switch
            {
                ScrollAlignment.Top => y,
                ScrollAlignment.Center => y - (ViewportHeight - h) * 0.5f,
                ScrollAlignment.Bottom => y - (ViewportHeight - h),
                _ => y,
            };
            ScrollToY(target, animated, duration, ease);
        }

        private void ScrollToY(float targetScrollY, bool? animated, float? duration, EaseFunction ease)
        {
            var maxY = Mathf.Max(0f, totalContentHeight - ViewportHeight);
            targetScrollY = Mathf.Clamp(targetScrollY, 0f, maxY);

            var doAnim = animated ?? defaultAnimated;
            if (!doAnim || !isActiveAndEnabled)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetScrollY);
                return;
            }

            StopScrollAnimation();
            scrollCoroutine = StartCoroutine(AnimateScroll(targetScrollY, duration ?? defaultDuration, ease.IsDefined ? ease : DefaultEase));
        }

        private Coroutine scrollCoroutine;

        private void StopScrollAnimation()
        {
            if (scrollCoroutine != null)
            {
                StopCoroutine(scrollCoroutine);
                scrollCoroutine = null;
            }
        }

        private IEnumerator AnimateScroll(float targetY, float duration, EaseFunction ease)
        {
            if (duration <= 0f)
            {
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
                yield break;
            }

            var startY = content.anchoredPosition.y;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = ease.Evaluate(t);
                var y = Mathf.LerpUnclamped(startY, targetY, eased);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, y);
                yield return null;
            }
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, targetY);
            scrollCoroutine = null;
        }
    }
}
