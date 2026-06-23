namespace Sindy.View.Features
{
    /// <summary>
    /// 텍스트 출력 능력. <see cref="FeatureViews.TextFeatureView"/>와 1:1 대칭.
    /// 텍스트 상태는 <see cref="PropModel{T}"/>로 보유하므로
    /// TimerModel·FormatNumberPropModel 등 PropModel&lt;string&gt; 파생 자가 갱신 모델을
    /// 생성자 주입으로 그대로 재사용할 수 있다.
    ///
    /// 폰트 크기는 선택적 모델 구동 속성이다(<see cref="ColorFeature"/>가 색을 구동하는 것과 동일 패턴).
    /// <see cref="FontSize"/> 값이 0 이하이면 프리팹/Variant의 기본 크기를 그대로 유지한다 —
    /// 일반 라벨은 디자인(크기)을 프리팹에 두고, 동적 크기가 필요한 경우에만 모델로 구동한다.
    /// </summary>
    public class TextFeature : ModelFeature
    {
        public PropModel<string> Text { get; }

        /// <summary>폰트 크기. 0 이하이면 프리팹/Variant 기본 크기를 유지한다.</summary>
        public PropModel<float> FontSize { get; }

        /// <summary>단순 값으로 초기화. fontSize가 0 이하이면 프리팹 기본 크기를 유지한다.</summary>
        public TextFeature(string text = null, float fontSize = 0f)
        {
            Text = new PropModel<string>(text);
            Text.AddTo(this);
            FontSize = new PropModel<float>(fontSize);
            FontSize.AddTo(this);
        }

        /// <summary>외부 모델 주입 (TimerModel 등 자가 갱신 모델). Feature와 함께 Dispose된다.</summary>
        public TextFeature(PropModel<string> external, float fontSize = 0f)
        {
            Text = external ?? throw new System.ArgumentNullException(nameof(external));
            Text.AddTo(this);
            FontSize = new PropModel<float>(fontSize);
            FontSize.AddTo(this);
        }
    }
}
