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

        // FR-CELL-07. 섹션 구성 이후의 등록 변경은 사후 재검증되지 않는다.
        // 사후 변경으로 인한 해상도 실패는 정의되지 않은 동작이므로,
        // 모든 등록은 SetSections 호출 전에 마칠 것을 권장한다.
        public void RegisterCellType<TVM>(SindyComponent prefab) where TVM : class
            => registry.Register(typeof(TVM), prefab);

        // ───────── Pool (FR-POOL-*) ─────────

        private ViewComponentPool pool;

        /// <summary>FR-POOL-04. 명시적 prefab을 N개 사전 워밍한다.</summary>
        public void PrewarmPool(SindyComponent prefab, int count) => EnsurePool().Prewarm(prefab, count);

        /// <summary>
        /// FR-POOL-04. VM 타입에 등록된 prefab을 레지스트리(인스턴스 → 전역)에서 해상하여 사전 워밍한다.
        /// 등록되지 않은 VM 타입에 대해 호출하면 즉시 throw한다.
        /// </summary>
        public void PrewarmPool<TVM>(int count) where TVM : class
            => PrewarmPool(registry.Resolve(typeof(TVM), null), count);

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
        /// FR-CELL-06. 모든 prefab 해상도를 mutation 이전에 검증하므로, 등록 누락은
        /// 본 메서드 안에서 즉시 throw되며 (첫 스크롤이나 첫 빈 콘텐츠 표시까지 지연되지 않음),
        /// 검증 실패 시 스크롤러의 기존 상태는 변경되지 않는다.
        /// </summary>
        public void SetSections(IEnumerable<ISection> newSections)
        {
            var staged = new List<ISection>();
            if (newSections != null)
            {
                foreach (var s in newSections)
                    if (s != null) staged.Add(s);
            }

            // FR-CELL-06. 섹션 구성 시점에 모든 prefab(콘텐츠/헤더/푸터/빈 콘텐츠)을 즉시 검증.
            // 콘텐츠 ObservableList가 비어 있어도 Section<TVM>의 제네릭 매개변수에서
            // 콘텐츠 VM 타입을 알 수 있으므로 검증은 항상 수행된다.
            // 어느 하나라도 해상되지 않으면 registry.Resolve가 InvalidOperationException을 던지며,
            // 그 시점에서 sections·listeners·active 셀은 아직 손대지 않았다 → atomic 동작.
            var stagedLayouts = staged.Count == 0
                ? Array.Empty<SectionLayout>()
                : new SectionLayout[staged.Count];
            for (var i = 0; i < staged.Count; i++)
            {
                ResolveSectionPrefabs(staged[i], ref stagedLayouts[i]);
            }

            // 검증 성공 — 이제부터 mutation
            DetachListeners();
            ReleaseAllActive();

            sections.Clear();
            sections.AddRange(staged);
            layouts = stagedLayouts;

            AttachListeners();
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
            ValidateWiring();
        }

        /// <summary>
        /// 필수 UI 와이어링이 모두 설정되었는지 검증한다.
        /// ScrollRect/Viewport/Content 중 하나라도 누락되어 있으면 명확한 예외를 던져
        /// 추후 ScrollTo·LateUpdate 등에서 발생할 수 있는 NullReferenceException을 방지한다.
        /// </summary>
        private void ValidateWiring()
        {
            if (scrollRect == null || viewport == null || content == null)
            {
                throw new InvalidOperationException(
                    $"Scroller on '{name}' is missing required wiring. " +
                    $"Assign ScrollRect/Viewport/Content in the Inspector, or attach a ScrollRect to the same GameObject. " +
                    $"(scrollRect={(scrollRect == null ? "null" : "OK")}, " +
                    $"viewport={(viewport == null ? "null" : "OK")}, " +
                    $"content={(content == null ? "null" : "OK")})");
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

            // FR-GRID-03 (보강). 가드 경로:
            //   1) 컨테이너 가로 너비가 변했을 때만 컬럼 수를 재산출
            //   2) 새 컬럼 수와 기존 컬럼 수를 비교
            //   3) 다른 경우에만 레이아웃 재계산 / 활성 셀의 RectTransform 갱신
            // 세로 크기 변경은 viewport만 영향을 미치고 그리드 산출에는 영향이 없으므로
            // 의도적으로 가로 너비 변동에만 트리거를 걸어 스크롤바 등장/사라짐에 따른
            // 픽셀 단위 미세 변동에서도 매 프레임 재레이아웃이 발생하지 않도록 한다.
            var width = viewport.rect.width;
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

        // ───────── Prefab resolution (FR-CELL-03 / 05 / 06) ─────────

        // 한 섹션의 모든 prefab을 해상하여 layout slot에 채운다.
        // 어느 하나라도 등록되지 않으면 registry.Resolve가 즉시 throw — FR-CELL-04, FR-CELL-06.
        private void ResolveSectionPrefabs(ISection s, ref SectionLayout L)
        {
            var opt = s.Option;
            L.ContentPrefab = registry.Resolve(s.ContentVMType, opt.ContentPrefab);
            L.HeaderPrefab = s.Header != null ? registry.Resolve(s.Header.GetType(), opt.HeaderPrefab) : null;
            L.FooterPrefab = s.Footer != null ? registry.Resolve(s.Footer.GetType(), opt.FooterPrefab) : null;
            L.EmptyPrefab = s.EmptyContent != null ? registry.Resolve(s.EmptyContent.GetType(), opt.EmptyContentPrefab) : null;
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

                // FR-SEC-04 (보강). "시각적으로 인접한" 두 섹션 사이의 간격은
                // (위 섹션의 BottomMargin + 아래 섹션의 TopMargin)으로 정의된다.
                // 비표시 섹션은 위 IsVisible 분기에서 어떤 yCursor 누적에도 기여하지 않으므로
                // (마진 포함 0), 그 사이를 건너뛰고 시각적으로 맞닿는 두 표시 섹션끼리만
                // 마진이 합산되는 결과가 자연스럽게 도출된다.
                //   예: [A 표시][B 비표시][C 표시] → A.bottom + C.top 만 가산되며 B.* 마진은 미적용.
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

                // TopY는 "TopMargin 적용 전" 섹션 블록 시작점이다.
                // HeaderTopY = TopY + TopMargin이 자동으로 헤더 위치가 된다.
                L.TopY = yCursor;
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

            // OverlapsRange는 half-open 구간(`y + h > top && y < bottom`)을 사용한다.
            // 같은 의미가 되도록 섹션·콘텐츠 early-out도 `<=` / `>=`로 통일한다.
            // (경계에서만 닿는 섹션·콘텐츠는 invisible로 처리되어 불필요한 셀 인스턴스화를 막는다.)
            var sectionTop = L.HeaderTopY;
            var sectionBottom = L.FooterTopY + L.FooterHeight;
            if (sectionBottom <= top || sectionTop >= bottom) return;

            if (L.ShowHeader && OverlapsRange(L.HeaderTopY, L.HeaderHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.HeaderSlot));

            if (L.ShowFooter && OverlapsRange(L.FooterTopY, L.FooterHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.FooterSlot));

            if (L.ShowEmpty && OverlapsRange(L.ContentTopY, L.ContentHeight, top, bottom))
                needed.Add(new CellKey(sectionIndex, CellKey.EmptySlot));

            if (L.RowCount > 0)
            {
                var contentBottom = L.ContentTopY + L.ContentHeight;
                if (contentBottom <= top || L.ContentTopY >= bottom) return;

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
            // 8장 (변경 이벤트 처리) + FR-DATA-03 (보강).
            //
            // FR-DATA-03 (보강): Move 핸들러는 Remove 핸들러와 Add 핸들러의 순차 호출로
            // 구현되어도 명세 위반이 아니다. 본 구현은 그 형태를 따른다.
            // 동일 인스턴스 재사용이나 위치 트랜지션 애니메이션은 본 명세 범위 외이므로
            // (CON-08) 보장하지 않는다.
            //
            // FR-HEIGHT-02에 의해 셀 높이가 고정이므로 Replace는 레이아웃 재계산이 필요 없다.
            switch (e.Action)
            {
                case ListChangeAction.Replace:
                    // 8.3. 화면 안에 있으면 같은 인스턴스에 새 VM을 다시 바인딩.
                    var key = new CellKey(sectionIndex, e.NewIndex);
                    if (active.TryGetValue(key, out var cell))
                    {
                        cell.Instance.SetModel(e.NewItem);
                    }
                    return;

                case ListChangeAction.Move:
                    // FR-DATA-03 (보강). Move = Remove + Add의 합성으로 처리한다.
                    // 두 단계 모두 동일한 후처리(콘텐츠 셀 회수 + 레이아웃 재계산)를
                    // 필요로 하므로, 두 번 호출하지 않고 한 번에 묶어 수행한다.
                    HandleStructuralChange(sectionIndex);
                    return;

                case ListChangeAction.Add:
                case ListChangeAction.Remove:
                case ListChangeAction.Reset:
                    // 8.1 / 8.2 / 8.5. 영향받는 섹션 이후의 모든 y좌표가 변할 수 있으므로
                    // 이 섹션의 활성 콘텐츠 셀을 일괄 회수하고 레이아웃을 invalidate한다.
                    // 빈 섹션 ↔ 비빈 섹션 전환도 이 경로에서 RecomputeLayout이 처리한다 (FR-EMPTY-04).
                    HandleStructuralChange(sectionIndex);
                    return;
            }
        }

        private void HandleStructuralChange(int sectionIndex)
        {
            ReleaseSectionContentCells(sectionIndex);
            InvalidateLayout();
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
                // 섹션 시작 = 헤더(또는 첫 콘텐츠)의 시작 y. 섹션 위쪽 마진은 점프 대상에서 제외한다.
                return (L.HeaderTopY, L.TotalHeight);
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
