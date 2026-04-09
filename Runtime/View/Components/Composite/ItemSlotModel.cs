using Sindy.RedDot;
using Sindy.View.Model;
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
                ? new RedDotModel((Sindy.RedDot.RedDotNode)null)
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
}
