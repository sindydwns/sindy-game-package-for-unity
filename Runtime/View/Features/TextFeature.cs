namespace Sindy.View.Features
{
    /// <summary>
    /// 텍스트 출력 능력. <see cref="FeatureViews.TextFeatureView"/>와 1:1 대칭.
    /// 텍스트 상태는 <see cref="PropModel{T}"/>로 보유하므로
    /// TimerModel·FormatNumberPropModel 등 PropModel&lt;string&gt; 파생 자가 갱신 모델을
    /// 생성자 주입으로 그대로 재사용할 수 있다.
    /// </summary>
    public class TextFeature : ModelFeature
    {
        public PropModel<string> Text { get; }

        /// <summary>단순 값으로 초기화.</summary>
        public TextFeature(string text = null)
        {
            Text = new PropModel<string>(text);
            Text.AddTo(this);
        }

        /// <summary>외부 모델 주입 (TimerModel 등 자가 갱신 모델). Feature와 함께 Dispose된다.</summary>
        public TextFeature(PropModel<string> external)
        {
            Text = external ?? throw new System.ArgumentNullException(nameof(external));
            Text.AddTo(this);
        }
    }
}
