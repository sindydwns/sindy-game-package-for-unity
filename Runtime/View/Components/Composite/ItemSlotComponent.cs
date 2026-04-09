using Sindy.RedDot;
using Sindy.View;
using UnityEngine;

namespace Sindy.View.Components.Composite
{
    public class ItemSlotModel : ViewModel
    {
        public PropModel<Sprite> Icon { get; } = new();
        public FormatNumberPropModel<int> Count { get; } = new(0);
        public RedDotModel RedDot { get; }

        public ItemSlotModel(string redDotPath = null)
        {
            RedDot = string.IsNullOrEmpty(redDotPath)
                ? new RedDotModel((RedDotNode)null)
                : new RedDotModel(redDotPath);

            this["icon"] = Icon;
            this["count"] = Count;
            this["redDot"] = RedDot;
        }

        public override void Dispose()
        {
            base.Dispose();
            Icon.Dispose();
            Count.Dispose();
            RedDot.Dispose();
        }
    }

    public class ItemSlotComponent : SindyComponent<ItemSlotModel>
    {
        [SerializeField] private IconComponent icon;
        [SerializeField] private LabelComponent count;
        [SerializeField] private RedDotComponent redDot;

        protected override void Init(ItemSlotModel model)
        {
            icon.SetModel(model.Icon).SetParent(this);
            count.SetModel(model.Count).SetParent(this);
            if (redDot != null) redDot.SetModel(model.RedDot).SetParent(this);
        }
    }
}
