using System;
using Sindy.RedDot;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// ViewModel에 Feature를 명시적으로 붙이는 확장 메서드 모음과,
    /// 자주 쓰는 자식 트리(Notice/ItemSlot) 합성 팩토리.
    ///
    /// 단일 Feature는 "어떤 Feature가 붙는지"가 호출부에 드러나도록 Empty()와 AddXxxFeature를 조합한다.
    /// <code>
    /// sindy.Bind(Models.Empty().AddTextFeature("신디"));
    /// sindy.Bind(Models.Empty().AddButtonFeature(allowHold: true));
    /// var notice = Models.Notice("알림", "정말 삭제할까요?");
    /// notice["confirm"].Feature&lt;ButtonFeature&gt;().OnClick.Subscribe(_ => Delete());
    /// </code>
    /// </summary>
    public static class Models
    {
        /// <summary>빈 ViewModel. AddXxxFeature로 능력을 붙여 조합한다.</summary>
        public static ViewModel Empty() => new();

        /// <summary>Feature를 추가한다. 같은 타입이 이미 있으면 예외. 체이닝 반환.</summary>
        public static ViewModel AddFeature<T>(this ViewModel vm, T feature) where T : ModelFeature
        {
            if (vm.Feature<T>() != default)
            {
                throw new InvalidOperationException($"같은 타입의 Feature가 이미 존재합니다. ({typeof(T).Name})");
            }
            vm.With(feature);
            return vm;
        }

        /// <summary>텍스트. fontSize가 0 이하이면 프리팹/Variant 기본 크기를 유지한다.</summary>
        public static ViewModel AddTextFeature(this ViewModel vm, string text, float fontSize = 0f) => vm.AddFeature(new TextFeature(text, fontSize));

        /// <summary>TimerModel·FormatNumberPropModel 등 자가 갱신 모델 주입. fontSize 0 이하이면 기본 크기 유지.</summary>
        public static ViewModel AddTextFeature(this ViewModel vm, PropModel<string> text, float fontSize = 0f) => vm.AddFeature(new TextFeature(text, fontSize));

        /// <summary>이미지(Image.sprite).</summary>
        public static ViewModel AddImageFeature(this ViewModel vm, Sprite sprite) => vm.AddFeature(new ImageFeature(sprite));

        /// <summary>게이지(Image.fillAmount). ratio는 0~1.</summary>
        public static ViewModel AddGaugeFeature(this ViewModel vm, float ratio = 0f) => vm.AddFeature(new GaugeFeature(ratio));

        /// <summary>토글(양방향).</summary>
        public static ViewModel AddToggleFeature(this ViewModel vm, bool isOn = false) => vm.AddFeature(new ToggleFeature(isOn));

        /// <summary>버튼(클릭/홀드). 라벨이 필요하면 AddTextFeature와 함께 조합한다.</summary>
        public static ViewModel AddButtonFeature(this ViewModel vm, bool allowHold = false) => vm.AddFeature(new ButtonFeature(allowHold));

        /// <summary>
        /// 확인/취소 팝업 모델 (구 NoticeComponent 대체).
        /// 키: "title", "content", "confirm", "cancel".
        /// hasCancel이 false이면 cancel 자식의 VisibilityFeature가 꺼진 상태로 생성된다.
        /// </summary>
        public static ViewModel Notice(string title, string content, bool hasCancel = true)
        {
            var vm = new ViewModel();
            vm["title"] = Empty().AddTextFeature(title);
            vm["content"] = Empty().AddTextFeature(content);
            vm["confirm"] = Empty().AddButtonFeature();
            vm["cancel"] = Empty().AddButtonFeature().AddFeature(new VisibilityFeature(hasCancel));
            return vm;
        }

        /// <summary>
        /// 아이템 슬롯 모델 (구 ItemSlotComponent 대체).
        /// 키: "icon", "count", "redDot".
        /// </summary>
        public static ViewModel ItemSlot(Sprite icon, int count, string redDotPath = null)
        {
            var vm = new ViewModel();
            vm["icon"] = Empty().AddImageFeature(icon);
            vm["count"] = Empty().AddTextFeature(new FormatNumberPropModel<int>(count));
            vm["redDot"] = Empty().AddFeature(string.IsNullOrEmpty(redDotPath)
                ? new RedDotFeature((RedDotNode)null)
                : new RedDotFeature(redDotPath));
            return vm;
        }
    }
}
