using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// FormatLabelComponent — FormatNumberPropModel&lt;int&gt;의 포맷된 문자열이 TMP_Text에 반영되는지 확인
    /// </summary>
    class TestFormatLabelComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestFormatLabelComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var count = new FormatNumberPropModel<int>(0);

            count.Text
                .Subscribe(v => Debug.Log($"[FormatLabel] formatted = \"{v}\""))
                .AddTo(disposables);

            component.SetModel(count);

            // 포맷팅 확인: int는 기본적으로 1000 단위 콤마 표시
            count.Source.Value = 1000;
            count.Source.Value = 9999999;
        }
    }
}
