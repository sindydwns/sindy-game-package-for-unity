using R3;

namespace Sindy.View.Components.Composite
{
    public class NoticeModel : ViewModel
    {
        public PropModel<string> Title { get; } = new();
        public PropModel<string> Content { get; } = new();
        public SubjModel<Unit> Confirm { get; } = new();
        public SubjModel<Unit> Cancel { get; } = new();
        public bool HasCancel { get; }

        public NoticeModel(bool hasCancel = true)
        {
            HasCancel = hasCancel;
            this["title"] = Title;
            this["content"] = Content;
            this["confirm"] = Confirm;
            if (hasCancel) this["cancel"] = Cancel;
        }

        public NoticeModel(string title, string content, bool hasCancel = true) : this(hasCancel)
        {
            Title.Value = title;
            Content.Value = content;
        }

        public override void Dispose()
        {
            base.Dispose();
            Title.Dispose();
            Content.Dispose();
            Confirm.Dispose();
            Cancel.Dispose();
        }
    }
}
