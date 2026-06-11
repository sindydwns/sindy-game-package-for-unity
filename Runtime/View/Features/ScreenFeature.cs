using System;
using Sindy.Common;

namespace Sindy.View.Features
{
    /// <summary>
    /// 화면 상태에 반응하는 능력. <see cref="FeatureViews.ResponsiveLayoutFeatureView"/>와 1:1 대칭.
    ///
    /// 모델은 "지금 어떤 변형(variant)인가"라는 의미적 상태만 알고,
    /// 실제 앵커·오프셋 좌표는 뷰의 직렬화 데이터에 둔다 (MVVM 경계 유지).
    ///
    /// 기본 selector는 가로/세로 2종. 3종 이상이 필요한 프로젝트는 selector를 직접 주입한다:
    /// <code>
    /// new ScreenFeature(s => s.Aspect > 1.5f ? "wide" : s.IsLandscape ? "landscape" : "portrait")
    /// </code>
    ///
    /// 테스트에서는 외부 모델 주입으로 화면 회전을 시뮬레이션할 수 있다:
    /// <code>
    /// var screen = new PropModel&lt;ScreenState&gt;(new ScreenState(1920, 1080, default));
    /// var feature = new ScreenFeature(screen);
    /// screen.Value = new ScreenState(1080, 1920, default);  // 회전
    /// </code>
    /// </summary>
    public class ScreenFeature : ModelFeature
    {
        public const string Landscape = "landscape";
        public const string Portrait = "portrait";

        /// <summary>화면 상태 스트림.</summary>
        public PropModel<ScreenState> Screen { get; }

        /// <summary>현재 변형 키. selector 결과가 바뀔 때만 방출된다.</summary>
        public PropModel<string> Variant { get; }

        /// <summary>실제 화면을 추적하는 기본 생성. selector 미지정 시 가로/세로 2종.</summary>
        public ScreenFeature(Func<ScreenState, string> selector = null)
            : this(new ScreenStateModel(), selector) { }

        /// <summary>외부 모델 주입 (테스트·커스텀 소스). Feature와 함께 Dispose된다.</summary>
        public ScreenFeature(PropModel<ScreenState> external, Func<ScreenState, string> selector = null)
        {
            Screen = external ?? throw new ArgumentNullException(nameof(external));
            Screen.AddTo(this);

            Variant = new PropModel<string>();
            Variant.AddTo(this);

            var select = selector ?? DefaultSelector;
            Screen.Subscribe(s =>
            {
                var key = select(s);
                if (Variant.Value != key)
                    Variant.Value = key;
            }).AddTo(disposables);
        }

        private static string DefaultSelector(ScreenState state) =>
            state.IsLandscape ? Landscape : Portrait;
    }
}
