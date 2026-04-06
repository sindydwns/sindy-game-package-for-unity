using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Sindy.Common;
using Sindy.Reactive;

namespace Sindy.RedDot
{
    public class RedDotNode : IDisposable
    {
        public static RedDotNode Root { get; } = new RedDotNode(string.Empty);

        /// <summary>
        /// 노드의 단일 이름. 노드의 전체 경로는 Path 속성을 참조.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 노드의 전체 경로. 부모 노드의 이름과 현재 노드의 이름을 '.'으로 구분하여 구성.
        /// </summary>
        public string Path => _pathProp.Value;
        private readonly ReactiveProperty<string> _pathProp = new();

        protected ReactiveProperty<RedDotNode> parent = new();
        protected ReactiveList<RedDotNode> children = new();
        public ReadOnlyReactiveProperty<int> Count { get; private set; }
        public object UserData { get; set; }

        public RedDotNode(string name, RedDotNode parent = null, IEnumerable<RedDotNode> children = null)
        {
            Name = name;
            this.parent.Value = parent;
            this.parent.Subscribe(UpdatePath);

            Count = this.children.OnChange
                .Select(x => children.Count())
                .Prepend(children?.Count() ?? 0)
                .ToReadOnlyReactiveProperty();
            this.children.OnChange
                .Where(x => x.Type == ReactiveList<RedDotNode>.ChangeType.Remove)
                .Subscribe(x => x.Item.parent.Value = null);
            this.children.OnChange
                .Where(x => x.Type == ReactiveList<RedDotNode>.ChangeType.Add)
                .Subscribe(x => x.Item.parent.Value = this);
            this.children.AddRange(children ?? Enumerable.Empty<RedDotNode>());
        }

        private void UpdatePath() => _pathProp.Value = $"{parent.Value?.Path ?? string.Empty}.{Name}".TrimStart('.');

        public static RedDotNode GetNodeAbs(string path) => Root.GetNode(path);
        public RedDotNode GetNode(string path)
        {
            path = path.TrimStart('.');
            if (string.IsNullOrEmpty(path))
            {
                return this;
            }
            var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return GetNode(names);
        }

        public RedDotNode GetNode(string[] path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (path.Length == 0)
            {
                return this;
            }
            var node = this;
            foreach (var name in path)
            {
                var child = node.children.Find(n => n.Name.Equals(name));
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
            return $"{Path}({Count})";
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
        }

        public void Dispose()
        {
            _pathProp.Dispose();
            parent.Dispose();
            children.DisposeAll();
        }
    }
}
