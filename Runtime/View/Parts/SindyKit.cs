using Sindy.View.Features;
using Sindy.View.Parts;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// 자주 쓰는 화면 단위를 합성한 재사용 ComponentBlueprint 모음.
    /// 디자인(레이아웃)만 정의하며, 모델(기능)은 호출부가 Open 시점에 주입한다.
    /// 전제: SindyDefaultParts 카탈로그가 ComponentManager에 등록되어 있어야 한다.
    ///
    /// 사용 예:
    ///   ComponentBlueprint.Create(SindyKit.Card)
    ///       .Patch("icon", PartKeys.Icon).WithModel(() => new ViewModel().With(new ImageFeature(sprite)))
    ///       .Patch("label", PartKeys.Label).WithModel(() => Models.Label("신디"))
    ///       .Open();
    /// </summary>
    public static class SindyKit
    {
        /// <summary>패널 + 아이콘 + 라벨 (세로 카드).</summary>
        public static readonly ComponentBlueprint Card = ComponentBlueprint
            .Create(PartKeys.Panel)
                .Layout(Direction.Vertical, spacing: 16).Padding(24).Align(TextAnchor.UpperCenter)
            .Patch("icon", PartKeys.Icon).Size(96, 96)
            .Patch("label", PartKeys.Label);

        /// <summary>아이콘 + 라벨 (가로 행). 라벨이 남는 가로폭을 채운다(Flexible).</summary>
        public static readonly ComponentBlueprint LabeledRow = ComponentBlueprint
            .Create(PartKeys.Container)
                .Layout(Direction.Horizontal, spacing: 16).Align(TextAnchor.MiddleLeft)
            .Patch("icon", PartKeys.Icon).Size(96, 96)
            .Patch("label", PartKeys.Label).Flexible(1);

        /// <summary>버튼 + 가운데 라벨.</summary>
        public static readonly ComponentBlueprint ButtonLabel = ComponentBlueprint
            .Create(PartKeys.Button)
                .Layout(Direction.Horizontal, spacing: 0).Padding(8, 24, 8, 24).Align(TextAnchor.MiddleCenter)
            .Patch("label", PartKeys.Label);

        /// <summary>라벨 + (우측) 토글 행. 라벨이 남는 폭을 채워 토글을 오른쪽 끝으로 민다(Flexible).</summary>
        public static readonly ComponentBlueprint ToggleRow = ComponentBlueprint
            .Create(PartKeys.Container)
                .Layout(Direction.Horizontal, spacing: 16).Align(TextAnchor.MiddleLeft).Size(-1, 96)
            .Patch("label", PartKeys.Label).Flexible(1)
            .Patch("toggle", PartKeys.Toggle).Size(96, 96);

        /// <summary>
        /// 패널 + 제목 + 내용 컨테이너 + 버튼 행.
        /// 제목 크기를 키우려면 모델에서 지정: Patch("title", PartKeys.Label).WithModel(() => Models.Label("제목", 48)).
        /// </summary>
        public static readonly ComponentBlueprint Popup = ComponentBlueprint
            .Create(PartKeys.Panel)
                .Layout(Direction.Vertical, spacing: 24).Padding(32)
            .Patch("title", PartKeys.Label)
            .Patch("content", PartKeys.Container).Layout(Direction.Vertical, spacing: 16)
            .Patch("buttons", PartKeys.Container).Layout(Direction.Horizontal, spacing: 16).Align(TextAnchor.MiddleCenter);

        /// <summary>Popup 파생 — 버튼 행에 취소/확인 라벨 버튼 2개.</summary>
        public static readonly ComponentBlueprint Dialog = ComponentBlueprint
            .Create(Popup)
            .Patch("buttons.cancel", ButtonLabel)
            .Patch("buttons.confirm", ButtonLabel);
    }
}
