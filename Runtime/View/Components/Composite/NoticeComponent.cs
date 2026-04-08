using UnityEngine;

namespace Sindy.View.Components.Composite
{
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
