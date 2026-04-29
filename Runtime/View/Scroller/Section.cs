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
        object GetContentVMAt(int index);
        int IndexOfContentVM(object vm);

        object Header { get; }
        object Footer { get; }
        object EmptyContent { get; }

        event Action<ListChange<object>> OnContentChanged;

        void AttachListener();
        void DetachListener();
    }

    /// <summary>
    /// FR-SEC-02. 단일 VM 타입의 섹션. 제네릭 타입 매개변수로 VM 타입을 강제한다.
    /// </summary>
    public class Section<TVM> : ISection where TVM : class
    {
        public ObservableList<TVM> Content { get; }
        public SectionOption Option { get; }

        public TVM Header { get; set; }
        public TVM Footer { get; set; }
        public TVM EmptyContent { get; set; }

        public event Action<ListChange<object>> OnContentChanged;

        public Section(ObservableList<TVM> content, SectionOption option)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            Option = option != null ? option : throw new ArgumentNullException(nameof(option));
        }

        public Type ContentVMType => typeof(TVM);
        public int ContentCount => Content.Count;
        public object GetContentVMAt(int index) => Content[index];
        public int IndexOfContentVM(object vm) => vm is TVM typed ? Content.IndexOf(typed) : -1;

        object ISection.Header => Header;
        object ISection.Footer => Footer;
        object ISection.EmptyContent => EmptyContent;

        public void AttachListener() => Content.OnChanged += OnContentChangedInternal;
        public void DetachListener() => Content.OnChanged -= OnContentChangedInternal;

        private void OnContentChangedInternal(ListChange<TVM> e)
        {
            // TVM 변경 이벤트를 비제네릭 object 이벤트로 어댑팅한다.
            var adapted = e.Action switch
            {
                ListChangeAction.Add => ListChange<object>.Add(e.NewItem, e.NewIndex),
                ListChangeAction.Remove => ListChange<object>.Remove(e.OldItem, e.OldIndex),
                ListChangeAction.Replace => ListChange<object>.Replace(e.OldItem, e.NewItem, e.NewIndex),
                ListChangeAction.Move => ListChange<object>.Move(e.NewItem, e.OldIndex, e.NewIndex),
                ListChangeAction.Reset => ListChange<object>.Reset(),
                _ => ListChange<object>.Reset(),
            };
            OnContentChanged?.Invoke(adapted);
        }
    }
}
