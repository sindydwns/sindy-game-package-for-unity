using R3;
using UnityEngine;

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

    public class NoticeComponent : SindyComponent<NoticeModel>
    {
        [SerializeField] private LabelComponent title;
        [SerializeField] private LabelComponent content;
        [SerializeField] private ButtonComponent confirm;
        [SerializeField] private ButtonComponent cancel;
        /// <summary>
        /// HasCancel이 false일 때 비활성화할 루트 오브젝트.
        /// </summary>
        [SerializeField] private GameObject cancelRoot;

        protected override void Init(NoticeModel model)
        {
            title.SetModel(model.Title).SetParent(this);
            content.SetModel(model.Content).SetParent(this);
            confirm.SetModel(model.Confirm).SetParent(this);

            if (cancelRoot != null) cancelRoot.SetActive(model.HasCancel);
            if (model.HasCancel) cancel.SetModel(model.Cancel).SetParent(this);
        }
    }
}
