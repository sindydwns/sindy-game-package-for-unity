using System.Collections.Generic;
using R3;

namespace Sindy.RedDot
{
    public class RedDotNode
    {
        protected RedDotNode parent;
        protected List<RedDotNode> children;
        public ReactiveProperty<int> CounterProp { get; private set; } = new();
        public object userData;
        public int Counter
        {
            get => CounterProp.Value;
            set
            {
                var delta = value - CounterProp.Value;
                if (delta != 0)
                {
                    if (parent != null)
                    {
                        parent.Counter += delta;
                    }
                    CounterProp.Value += delta;
                }
            }
        }
        public IEnumerable<RedDotNode> Children => children;

        public string name;

        public RedDotNode(string name, RedDotNode parent = null)
        {
            this.name = name;
            this.parent = parent;
        }

        public RedDotNode GetNode(string path)
        {
            if (string.IsNullOrEmpty(path) || string.Equals(path, "."))
            {
                return this;
            }
            var names = path.Split('.', System.StringSplitOptions.RemoveEmptyEntries);
            return GetNode(names);
        }

        public RedDotNode GetNode(string[] path)
        {
            if (path == null)
            {
                throw new System.ArgumentNullException(nameof(path));
            }
            if (path.Length == 0)
            {
                return this;
            }
            var node = this;
            foreach (var name in path)
            {
                node.children ??= new List<RedDotNode>();

                var child = node.children.Find(n => n.name.Equals(name));
                if (child == null)
                {
                    child = new RedDotNode(name, node);
                    node.children.Add(child);
                }

                node = child;
            }
            return node;
        }

        public override string ToString()
        {
            return $"{name}({Counter})";
        }

        public void Clear()
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.Clear();
                }
            }
            Counter = 0;
        }
    }
}
