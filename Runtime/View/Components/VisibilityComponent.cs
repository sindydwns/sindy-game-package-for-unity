using Sindy.View.Model;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class VisibilityComponent : SindyComponent<BoolPropModel>
    {
        /// <summary>
        /// null이면 자기 자신의 GameObject를 제어합니다.
        /// </summary>
        [SerializeField] private GameObject target;

        protected override void Init(BoolPropModel model)
        {
            var obj = target != null ? target : gameObject;
            model.Show.Subscribe(v => obj.SetActive(v)).AddTo(disposables);
        }
    }
}
