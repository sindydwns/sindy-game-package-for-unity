using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class VisibilityComponent : SindyComponent<PropModel<bool>>
    {
        /// <summary>
        /// null이면 자기 자신의 GameObject를 제어합니다.
        /// </summary>
        [SerializeField] private GameObject target;

        protected override void Init(PropModel<bool> model)
        {
            var obj = target != null ? target : gameObject;
            model.Subscribe(v => obj.SetActive(v)).AddTo(disposables);
        }
    }

    public class VisibilityModel : PropModel<bool>
    {
        public VisibilityModel() { }
        public VisibilityModel(bool visible) : base(visible) { }
    }
}
