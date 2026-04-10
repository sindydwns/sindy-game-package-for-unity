using UnityEngine;
using UnityEngine.UI;

namespace Sindy.View.Features
{
    public enum Direction { Horizontal, Vertical }

    public class LayoutFeature : ViewModelFeature
    {
        internal float MarginTop, MarginRight, MarginBottom, MarginLeft;
        internal bool HasMargin;

        internal Direction? LayoutDirection;
        internal float Spacing;

        internal float PaddingTop, PaddingRight, PaddingBottom, PaddingLeft;
        internal bool HasPadding;

        internal TextAnchor? Alignment;

        internal float PreferredWidth = -1;
        internal float PreferredHeight = -1;

        internal bool HasLayout => LayoutDirection.HasValue;
        internal bool HasAlignment => Alignment.HasValue;
        internal bool HasSize => PreferredWidth >= 0 || PreferredHeight >= 0;

        public void Apply(RectTransform target)
        {
            if (target == null) return;

            if (HasLayout)
            {
                var group = LayoutDirection == Direction.Horizontal
                    ? (HorizontalOrVerticalLayoutGroup)target.gameObject.AddComponent<HorizontalLayoutGroup>()
                    : target.gameObject.AddComponent<VerticalLayoutGroup>();

                group.spacing = Spacing;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                group.childControlWidth = true;
                group.childControlHeight = true;

                if (HasPadding)
                    group.padding = new RectOffset(
                        Mathf.RoundToInt(PaddingLeft),
                        Mathf.RoundToInt(PaddingRight),
                        Mathf.RoundToInt(PaddingTop),
                        Mathf.RoundToInt(PaddingBottom));

                if (HasAlignment)
                    group.childAlignment = Alignment.Value;
            }

            if (HasSize)
            {
                var element = target.gameObject.GetComponent<LayoutElement>()
                              ?? target.gameObject.AddComponent<LayoutElement>();
                if (PreferredWidth >= 0) element.preferredWidth = PreferredWidth;
                if (PreferredHeight >= 0) element.preferredHeight = PreferredHeight;
            }

            if (HasMargin)
            {
                target.offsetMin = new Vector2(MarginLeft, MarginBottom);
                target.offsetMax = new Vector2(-MarginRight, -MarginTop);
            }
        }
    }
}
