using UnityEngine;

namespace Sindy.View.Components
{
    public class PopupComponent : ViewComponent
    {
        [SerializeField] private GameObject root;

        private void Awake()
        {
            if (root != null) root.SetActive(false);
        }

        protected override void Init(ViewModel model)
        {
            if (root != null) root.SetActive(true);
            base.Init(model);
        }

        protected override void Clear(ViewModel model)
        {
            if (root != null) root.SetActive(false);
        }
    }
}
