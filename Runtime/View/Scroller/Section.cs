using System;
using Sindy.Reactive;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 비제네릭 섹션 인터페이스. Scroller가 섹션을 다룰 때 사용한다.
    /// FR-SEC-02에 의해 단일 VM 타입은 제네릭 매개변수로 강제되며,
    /// Scroller 내부에서는 타입 무관하게 다루기 위해 이 인터페이스를 사용한다.
    /// </summary>
    public interface ISection
    {
        SectionOption Option { get; }

        Type ContentVMType { get; }
        int ContentCount { get; }
        IViewModel GetContentVMAt(int index);
        int IndexOfContentVM(IViewModel vm);

        IViewModel Header { get; }
        IViewModel Footer { get; }
        IViewModel EmptyContent { get; }

        event Action<ListChange<IViewModel>> OnContentChanged;

        void AttachListener();
        void DetachListener();
    }

    /// <summary>
    /// FR-SEC-02. 단일 VM 타입의 섹션. 제네릭 타입 매개변수로 VM 타입을 강제한다.
    /// </summary>
    public class Section<TVM> : ISection where TVM : class, IViewModel
    {
        public ObservableList<TVM> Content { get; }
        public SectionOption Option { get; }

        private IViewModel header;
        private IViewModel footer;
        private IViewModel emptyContent;
        private bool isAttached;

        /// <summary>
        /// 섹션 헤더 VM. spec 7.3 예시처럼 콘텐츠 TVM과 다른 VM 타입을 자유롭게 할당할 수 있도록
        /// `object`로 선언한다 (예: `Section&lt;ItemVM&gt; { Header = new HeaderVM(...) }`).
        /// SetSections에 전달되기 전(아직 Scroller에 부착되지 않은 상태)에만 설정 가능.
        /// 부착 이후 변경은 InvalidOperationException을 던진다 (FR-CELL-06: prefab은 SetSections
        /// 시점에 검증·캐시되며 사후 재해상되지 않으므로 silent하게 무시되는 일관성 깨짐을 방지).
        /// </summary>
        public IViewModel Header
        {
            get => header;
            set { ThrowIfAttached(nameof(Header)); header = value; }
        }
        public IViewModel Footer
        {
            get => footer;
            set { ThrowIfAttached(nameof(Footer)); footer = value; }
        }
        public IViewModel EmptyContent
        {
            get => emptyContent;
            set { ThrowIfAttached(nameof(EmptyContent)); emptyContent = value; }
        }

        public event Action<ListChange<IViewModel>> OnContentChanged;

        public Section(ObservableList<TVM> content, SectionOption option)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Option = option != null ? option : throw new ArgumentNullException(nameof(option));
        }

        public Type ContentVMType => typeof(TVM);
        public int ContentCount => Content.Count;
        public IViewModel GetContentVMAt(int index) => Content[index];
        public int IndexOfContentVM(IViewModel vm) => vm is TVM typed ? Content.IndexOf(typed) : -1;

        IViewModel ISection.Header => header;
        IViewModel ISection.Footer => footer;
        IViewModel ISection.EmptyContent => emptyContent;

        public void AttachListener()
        {
            // 멱등성 보장: 이미 부착되어 있으면 no-op (이중 구독·이중 isAttached set 방지).
            if (isAttached) return;
            Content.OnChanged += OnContentChangedInternal;
            isAttached = true;
        }
        public void DetachListener()
        {
            // 멱등성 보장: 부착되지 않은 상태의 Detach 호출은 no-op.
            // 이를 통해 짝이 맞지 않는 Detach 호출이 isAttached 가드를 잘못 해제하는 일이 없다.
            if (!isAttached) return;
            Content.OnChanged -= OnContentChangedInternal;
            isAttached = false;
        }

        private void ThrowIfAttached(string property)
        {
            if (isAttached)
                throw new InvalidOperationException(
                    $"Section.{property} cannot be modified after the section is attached to a Scroller " +
                    $"via SetSections. Configure all section fields before calling SetSections, or call " +
                    $"Scroller.SetSections again to rebuild.");
        }

        private void OnContentChangedInternal(ListChange<TVM> e)
        {
            // TVM 변경 이벤트를 비제네릭 object 이벤트로 어댑팅한다.
            var adapted = e.Action switch
            {
                ListChangeAction.Add => ListChange<IViewModel>.Add(e.NewItem, e.NewIndex),
                ListChangeAction.Remove => ListChange<IViewModel>.Remove(e.OldItem, e.OldIndex),
                ListChangeAction.Replace => ListChange<IViewModel>.Replace(e.OldItem, e.NewItem, e.NewIndex),
                ListChangeAction.Move => ListChange<IViewModel>.Move(e.NewItem, e.OldIndex, e.NewIndex),
                ListChangeAction.Reset => ListChange<IViewModel>.Reset(),
                _ => ListChange<IViewModel>.Reset(),
            };
            OnContentChanged?.Invoke(adapted);
        }
    }
}
