using R3;
using Sindy.View;
using Sindy.View.Components;
using UnityEngine;

namespace Sindy.Test
{
    // ═══════════════════════════════════════════════════════════════════════════
    // 공통
    // ═══════════════════════════════════════════════════════════════════════════

    public class CloseButtonModel : ViewModel
    {
        public ButtonModel Close { get; } = new();

        public CloseButtonModel()
        {
            this["close"] = Close;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 예시 3 — Toast
    // ═══════════════════════════════════════════════════════════════════════════

    public class ToastModel : ViewModel
    {
        public LabelModel  Message { get; } = new();
        public TimerModel  Timer   { get; }

        public ToastModel(string message, float duration = 3f)
        {
            Message.Value = message;
            Timer = new TimerModel(duration);

            this["message"] = Message;
            this["timer"]   = Timer;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 예시 4 — 캐릭터 프로필
    // ═══════════════════════════════════════════════════════════════════════════

    public class CharacterData
    {
        public string Name;
        public int    Level;
        public int    Hp;
        public int    MaxHp;
        public int    Mp;
        public int    MaxMp;
        public int    Attack;
        public int    Defense;
        public Sprite Icon;
    }

    public class CharacterProfileModel : ViewModel
    {
        public PropModel<Sprite> Icon    { get; } = new();
        public LabelModel        Name    { get; } = new();
        public LabelModel        Level   { get; } = new();
        public PropModel<float>  HpGauge { get; } = new();
        public PropModel<float>  MpGauge { get; } = new();
        public LabelModel        Attack  { get; } = new();
        public LabelModel        Defense { get; } = new();
        public ButtonModel       Close   { get; } = new();

        public CharacterProfileModel(CharacterData data)
        {
            Icon.Value    = data.Icon;
            Name.Value    = data.Name;
            Level.Value   = $"Lv.{data.Level}";
            HpGauge.Value = data.MaxHp > 0 ? (float)data.Hp / data.MaxHp : 0f;
            MpGauge.Value = data.MaxMp > 0 ? (float)data.Mp / data.MaxMp : 0f;
            Attack.Value  = $"ATK {data.Attack}";
            Defense.Value = $"DEF {data.Defense}";

            this["header.icon"]    = Icon;
            this["header.name"]    = Name;
            this["header.level"]   = Level;
            this["stats.hp"]       = HpGauge;
            this["stats.mp"]       = MpGauge;
            this["stats.attack"]   = Attack;
            this["stats.defense"]  = Defense;
            this["close"]          = Close;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 예시 5 — 상점
    // ═══════════════════════════════════════════════════════════════════════════

    public enum ShopCategory { Weapon, Armor, Consumable }

    public class ShopItemData
    {
        public string Name;
        public int    Price;
        public Sprite Icon;
    }

    public class ShopItemModel : ViewModel
    {
        public PropModel<Sprite>  Icon  { get; } = new();
        public LabelModel         Name  { get; } = new();
        public LabelModel         Price { get; } = new();
        public ButtonModel        Buy   { get; } = new();

        public ShopItemModel(ShopItemData data)
        {
            Icon.Value  = data.Icon;
            Name.Value  = data.Name;
            Price.Value = $"{data.Price:n0}G";

            this["icon"]  = Icon;
            this["name"]  = Name;
            this["price"] = Price;
            this["buy"]   = Buy;
        }
    }

    public class ShopModel : ViewModel
    {
        public TabModel            Category { get; } = new((int)ShopCategory.Weapon);
        public ListViewModel<ShopItemModel> Items    { get; } = new();
        public ButtonModel         Close    { get; } = new();

        public ShopModel()
        {
            this["category"] = Category;
            this["items"]    = Items;
            this["close"]    = Close;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // 예시 6 — 캐릭터 인벤토리 (필터 포함)
    // ═══════════════════════════════════════════════════════════════════════════

    public enum CharacterClass { All, Warrior, Mage, Archer }
    public enum ItemGrade       { All, Common, Rare, Epic, Legendary }

    public class InventoryFilterModel : ViewModel
    {
        public TabModel           ClassFilter { get; } = new((int)CharacterClass.All);
        public TabModel           GradeFilter { get; } = new((int)ItemGrade.All);
        public PropModel<string>  SearchText  { get; } = new(string.Empty);

        public InventoryFilterModel()
        {
            this["class"]  = ClassFilter;
            this["grade"]  = GradeFilter;
            this["search"] = SearchText;
        }
    }

    public class CharacterSlotModel : ViewModel
    {
        public PropModel<Sprite>  Icon    { get; } = new();
        public LabelModel         Name    { get; } = new();
        public LabelModel         Level   { get; } = new();
        public PropModel<float>   HpGauge { get; } = new();
        public ButtonModel        Select  { get; } = new();

        public CharacterSlotModel(CharacterData data)
        {
            Icon.Value    = data.Icon;
            Name.Value    = data.Name;
            Level.Value   = $"Lv.{data.Level}";
            HpGauge.Value = data.MaxHp > 0 ? (float)data.Hp / data.MaxHp : 0f;

            this["icon"]    = Icon;
            this["name"]    = Name;
            this["level"]   = Level;
            this["hp"]      = HpGauge;
            this["select"]  = Select;
        }
    }

    public class CharacterInventoryModel : ViewModel
    {
        public InventoryFilterModel                   Filter   { get; } = new();
        public ListViewModel<CharacterSlotModel>      List     { get; } = new();
        public ButtonModel                            Close    { get; } = new();
        public SubjModel<CharacterSlotModel>          Selected { get; } = new();

        public CharacterInventoryModel()
        {
            this["filter"]   = Filter;
            this["list"]     = List;
            this["close"]    = Close;
            this["selected"] = Selected;
        }
    }
}
