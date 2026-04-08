using Sindy.RedDot;
using UnityEngine;

namespace Sindy.View.Components.Composite
{
    public class ItemSlotComponent : SindyComponent<ItemSlotModel>
    {
        [SerializeField] private IconComponent icon;
        [SerializeField] private FormatLabelComponent count;
        [SerializeField] private RedDotComponent redDot;

        protected override void Init(ItemSlotModel model)
        {
            icon.SetModel(model.Icon).SetParent(this);
            count.SetModel(model.Count).SetParent(this);
            if (redDot != null) redDot.SetModel(model.RedDot).SetParent(this);
        }
    }
}
