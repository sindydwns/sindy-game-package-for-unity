using Sindy.RedDot;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// 자주 쓰는 ViewModel 조합의 팩토리. 전용 모델 클래스 없이
    /// "ViewModel + Feature 조합"을 한 줄로 생성한다.
    ///
    /// <code>
    /// sindy.Bind(Models.Label("신디"));
    /// var notice = Models.Notice("알림", "정말 삭제할까요?");
    /// notice["confirm"].Feature&lt;ButtonFeature&gt;().OnClick.Subscribe(_ => Delete());
    /// </code>
    /// </summary>
    public static class Models
    {
        public static ViewModel Label(string text) => new ViewModel().With(new TextFeature(text));

        /// <summary>TimerModel·FormatNumberPropModel 등 자가 갱신 모델 주입.</summary>
        public static ViewModel Label(PropModel<string> text) => new ViewModel().With(new TextFeature(text));

        public static ViewModel Icon(Sprite sprite) => new ViewModel().With(new ImageFeature(sprite));

        public static ViewModel Gauge(float ratio = 0f) => new ViewModel().With(new GaugeFeature(ratio));

        public static ViewModel Toggle(bool isOn = false) => new ViewModel().With(new ToggleFeature(isOn));

        public static ViewModel Button(bool allowHold = false) => new ViewModel().With(new ButtonFeature(allowHold));

        /// <summary>라벨이 있는 버튼. 같은 오브젝트에 TextFeatureView + ButtonFeatureView를 부착해 사용한다.</summary>
        public static ViewModel Button(string label, bool allowHold = false) =>
            new ViewModel()
                .With(new TextFeature(label))
                .With(new ButtonFeature(allowHold));

        /// <summary>
        /// 확인/취소 팝업 모델 (구 NoticeComponent 대체).
        /// 키: "title", "content", "confirm", "cancel".
        /// hasCancel이 false이면 cancel 자식의 VisibilityFeature가 꺼진 상태로 생성된다.
        /// </summary>
        public static ViewModel Notice(string title, string content, bool hasCancel = true)
        {
            var vm = new ViewModel();
            vm["title"] = Label(title);
            vm["content"] = Label(content);
            vm["confirm"] = Button();
            vm["cancel"] = new ViewModel()
                .With(new ButtonFeature())
                .With(new VisibilityFeature(hasCancel));
            return vm;
        }

        /// <summary>
        /// 아이템 슬롯 모델 (구 ItemSlotComponent 대체).
        /// 키: "icon", "count", "redDot".
        /// </summary>
        public static ViewModel ItemSlot(Sprite icon, int count, string redDotPath = null)
        {
            var vm = new ViewModel();
            vm["icon"] = Icon(icon);
            vm["count"] = Label(new FormatNumberPropModel<int>(count));
            vm["redDot"] = new ViewModel().With(string.IsNullOrEmpty(redDotPath)
                ? new RedDotFeature((RedDotNode)null)
                : new RedDotFeature(redDotPath));
            return vm;
        }
    }
}
