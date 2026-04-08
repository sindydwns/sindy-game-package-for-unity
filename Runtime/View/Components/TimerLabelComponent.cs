using System;
using Sindy.View.Model;
using TMPro;
using UnityEngine;
using R3;

namespace Sindy.View.Components
{
    public class TimerLabelComponent : SindyComponent<TimerModel>
    {
        [SerializeField] private TMP_Text label;
        /// <summary>
        /// TimeSpan 복합 서식 문자열. 예: @"mm\:ss", @"hh\:mm\:ss"
        /// </summary>
        [SerializeField] private string format = @"mm\:ss";

        protected override void Init(TimerModel model)
        {
            model.Remaining
                .Subscribe(v => label.text = TimeSpan.FromSeconds(v).ToString(format))
                .AddTo(disposables);
        }
    }
}
