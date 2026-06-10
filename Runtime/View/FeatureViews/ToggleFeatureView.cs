using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.FeatureViews
{
    /// <summary>uGUI Toggle과 <see cref="ToggleFeature"/>를 양방향 바인딩한다.</summary>
    [AddComponentMenu("Sindy/Feature Views/Toggle Feature View")]
    public class ToggleFeatureView : FeatureView<ToggleFeature>
    {
        [SerializeField] private Toggle toggle;

        protected virtual void Reset()
        {
            toggle = GetComponentInChildren<Toggle>(true);
        }

        protected override void Bind(ToggleFeature feature, ICollection<IDisposable> disposables)
        {
            feature.IsOn.Subscribe(v => toggle.SetIsOnWithoutNotify(v)).AddTo(disposables);

            void OnValueChanged(bool v) => feature.IsOn.Value = v;
            toggle.onValueChanged.AddListener(OnValueChanged);
            disposables.Add(Disposable.Create(() => toggle.onValueChanged.RemoveListener(OnValueChanged)));
        }
    }
}
