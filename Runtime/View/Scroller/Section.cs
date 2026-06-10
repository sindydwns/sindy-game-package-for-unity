using System;
using Sindy.Reactive;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 비제네릭 섹션. 셀 모델은 "ViewModel + Feature 조합"이므로 VM 타입 제약이 없다.
    ///
    /// prefab 해상 우선순위 (슬롯별):
    ///   1) Section의 명시 prefab (ContentPrefab 등)
    ///   2) SectionOption의 prefab 오버라이드 (보조)
    ///   3) 셀 키 (ContentKey 등) → CellRegistry/CellCatalog 해상
    ///   4) 어느 것도 없으면 Bind 시점에 throw (atomic — 섹션 상태는 손대지 않음)
    ///
    /// 모든 설정은 스크롤러에 부착되기 전에 마쳐야 한다 (FR-CELL-06).
    /// 부착 이후 변경은 InvalidOperationException을 던진다.
    /// </summary>
    public class Section
    {
        public ObservableList<IViewModel> Content { get; }
        public SectionOption Option { get; }

        private string contentKey;
        private SindyComponent contentPrefab;
        private IViewModel header;
        private string headerKey;
        private SindyComponent headerPrefab;
        private IViewModel footer;
        private string footerKey;
        private SindyComponent footerPrefab;
        private IViewModel emptyContent;
        private string emptyContentKey;
        private SindyComponent emptyContentPrefab;
        private bool isAttached;

        public Section(ObservableList<IViewModel> content, SectionOption option)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Option = option != null ? option : throw new ArgumentNullException(nameof(option));
        }

        /// <summary>콘텐츠 셀 키. CellRegistry/CellCatalog에서 prefab을 해상한다.</summary>
        public string ContentKey
        {
            get => contentKey;
            set { ThrowIfAttached(nameof(ContentKey)); contentKey = value; }
        }

        /// <summary>콘텐츠 prefab 직접 지정. 키 등록 없이 일회성 셀에 사용한다. 키보다 우선한다.</summary>
        public SindyComponent ContentPrefab
        {
            get => contentPrefab;
            set { ThrowIfAttached(nameof(ContentPrefab)); contentPrefab = value; }
        }

        public IViewModel Header
        {
            get => header;
            set { ThrowIfAttached(nameof(Header)); header = value; }
        }

        public string HeaderKey
        {
            get => headerKey;
            set { ThrowIfAttached(nameof(HeaderKey)); headerKey = value; }
        }

        public SindyComponent HeaderPrefab
        {
            get => headerPrefab;
            set { ThrowIfAttached(nameof(HeaderPrefab)); headerPrefab = value; }
        }

        public IViewModel Footer
        {
            get => footer;
            set { ThrowIfAttached(nameof(Footer)); footer = value; }
        }

        public string FooterKey
        {
            get => footerKey;
            set { ThrowIfAttached(nameof(FooterKey)); footerKey = value; }
        }

        public SindyComponent FooterPrefab
        {
            get => footerPrefab;
            set { ThrowIfAttached(nameof(FooterPrefab)); footerPrefab = value; }
        }

        public IViewModel EmptyContent
        {
            get => emptyContent;
            set { ThrowIfAttached(nameof(EmptyContent)); emptyContent = value; }
        }

        public string EmptyContentKey
        {
            get => emptyContentKey;
            set { ThrowIfAttached(nameof(EmptyContentKey)); emptyContentKey = value; }
        }

        public SindyComponent EmptyContentPrefab
        {
            get => emptyContentPrefab;
            set { ThrowIfAttached(nameof(EmptyContentPrefab)); emptyContentPrefab = value; }
        }

        public int ContentCount => Content.Count;
        public IViewModel GetContentVMAt(int index) => Content[index];
        public int IndexOfContentVM(IViewModel vm) => Content.IndexOf(vm);

        public event Action<ListChange<IViewModel>> OnContentChanged;

        public bool IsAttached => isAttached;

        public void AttachListener()
        {
            // 멱등성 보장: 이미 부착되어 있으면 no-op (이중 구독 방지).
            if (isAttached) return;
            Content.OnChanged += OnContentChangedInternal;
            isAttached = true;
        }

        public void DetachListener()
        {
            // 멱등성 보장: 부착되지 않은 상태의 Detach는 no-op.
            if (!isAttached) return;
            Content.OnChanged -= OnContentChangedInternal;
            isAttached = false;
        }

        private void ThrowIfAttached(string property)
        {
            if (isAttached)
                throw new InvalidOperationException(
                    $"Section.{property} cannot be modified after the section is attached to a Scroller. " +
                    $"Configure all section fields before binding, or bind a new ScrollerFeature to rebuild.");
        }

        private void OnContentChangedInternal(ListChange<IViewModel> e) => OnContentChanged?.Invoke(e);
    }
}
