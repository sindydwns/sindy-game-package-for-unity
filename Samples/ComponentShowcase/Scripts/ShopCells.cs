using System;
using R3;
using Sindy.View;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.Samples.ComponentShowcase
{
    /// <summary>
    /// 데모 아이템 데이터. Inspector에서 편집할 수 있도록 직렬화 필드로 구성한다.
    /// frame은 등급별 9-slice 스프라이트 — ImageFeature로 런타임 교체되는 것을 시연한다.
    /// </summary>
    [Serializable]
    public class ShopItemData
    {
        public string name;
        [TextArea] public string description;
        public long price;
        public Sprite icon;
        public Sprite frame;

        public ShopItemData() { }

        public ShopItemData(string name, string description, long price, Sprite icon, Sprite frame)
        {
            this.name = name;
            this.description = description;
            this.price = price;
            this.icon = icon;
            this.frame = frame;
        }
    }

    /// <summary>
    /// 셀 ViewModel 정적 팩토리. 전용 VM 클래스 없이 "ViewModel + Feature 조합"만으로
    /// 셀 모델을 만드는 표준 패턴 — 타 프로젝트에서 그대로 복사해 쓰는 것을 의도한다.
    /// </summary>
    public static class ShopCells
    {
        /// <summary>
        /// 아이템 셀. 루트에 ButtonFeature(셀 전체 클릭), 자식 키 icon/name/price.
        /// 클릭 구독은 셀의 ButtonFeature(Subject)에 묶이므로 셀 VM이 Dispose되면 함께 정리된다.
        /// </summary>
        public static ViewModel Item(ShopItemData data, Action<ShopItemData> onSelect)
        {
            var cell = new ViewModel().With(new ButtonFeature());
            cell["icon"] = Models.Icon(data.icon);
            cell["name"] = Models.Label(data.name);
            cell["price"] = Models.Label(new FormatNumberPropModel<long>(data.price, v => $"{v:n0}G"));

            cell.Feature<ButtonFeature>().OnClick.Subscribe(_ => onSelect?.Invoke(data));
            return cell;
        }

        /// <summary>섹션 헤더 셀. TextFeature 하나면 충분하다.</summary>
        public static ViewModel Header(string title) => Models.Label(title);

        /// <summary>이벤트 배너 셀. 키 등록 없이 Section.ContentPrefab으로 직접 지정하는 일회성 셀.</summary>
        public static ViewModel Banner(string message) => Models.Label(message);
    }
}
