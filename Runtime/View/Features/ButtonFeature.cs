using R3;

namespace Sindy.View.Features
{
    /// <summary>
    /// 버튼 입력 능력 (클릭 + 홀드). <see cref="FeatureViews.ButtonFeatureView"/>와 1:1 대칭.
    ///
    /// 클릭과 홀드는 같은 포인터 제스처 공간을 공유하므로 별개 Feature가 아니라
    /// 이 Feature의 내부 옵션(<paramref name="allowHold"/>)이다 (설계 원칙 2).
    /// "이 버튼이 홀드 가능한가?"의 답은 모델 생성 인자가 곧 답이다.
    ///
    /// 테스트에서는 <c>OnClick.OnNext(Unit.Default)</c>로 클릭을 시뮬레이션할 수 있다.
    /// </summary>
    public class ButtonFeature : ModelFeature
    {
        /// <summary>클릭 이벤트. 홀드가 발생한 프레스의 릴리스에서는 발행되지 않는다.</summary>
        public Subject<Unit> OnClick { get; } = new();

        /// <summary>홀드 반복 이벤트. 누적 반복 횟수를 전달한다.</summary>
        public Subject<int> OnHold { get; } = new();

        /// <summary>홀드 허용 여부. 런타임 토글 가능 (기존 HoldFeature.AllowHold 계승).</summary>
        public PropModel<bool> AllowHold { get; }

        /// <summary>false로 바꾸면 진행 중인 홀드를 즉시 중단한다 (기존 HoldFeature.KeepHold 계승).</summary>
        public PropModel<bool> KeepHold { get; }

        public ButtonFeature(bool allowHold = false)
        {
            AllowHold = new PropModel<bool>(allowHold);
            KeepHold = new PropModel<bool>(false);
            AllowHold.AddTo(this);
            KeepHold.AddTo(this);
        }

        /// <summary>진행 중인 홀드를 중단한다.</summary>
        public void Release() => KeepHold.Value = false;

        public override void Dispose()
        {
            base.Dispose();
            OnClick.Dispose();
            OnHold.Dispose();
        }
    }
}
