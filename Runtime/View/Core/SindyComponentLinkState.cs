using System.Collections.Generic;
using System.Linq;

namespace Sindy.View
{
    internal sealed class SindyComponentLinkState
    {
        private readonly SindyComponent owner;
        private SindyComponent parent;
        private HashSet<SindyComponent> children;

        public SindyComponentLinkState(SindyComponent owner)
        {
            this.owner = owner;
        }

        public void AttachTo(SindyComponent newParent)
        {
            DetachFromParent();
            if (newParent == null)
            {
                return;
            }

            parent = newParent;
            newParent.LinkState.AddChild(owner);
        }

        public void DetachFromParent()
        {
            if (parent == null)
            {
                return;
            }

            parent.LinkState.RemoveChild(owner);
            parent = null;
        }

        public IEnumerable<SindyComponent> GetChildrenSnapshot()
        {
            return children == null ? Enumerable.Empty<SindyComponent>() : children.ToArray();
        }

        public void ClearChildrenLinks()
        {
            if (children == null)
            {
                return;
            }

            foreach (var child in children)
            {
                child.LinkState.parent = null;
            }

            children.Clear();
        }

        private void AddChild(SindyComponent child)
        {
            children ??= new HashSet<SindyComponent>();
            children.Add(child);
        }

        private void RemoveChild(SindyComponent child)
        {
            children?.Remove(child);
        }
    }
}
