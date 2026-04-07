using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Sindy.Reactive;

namespace Sindy.RedDot
{
    public class RedDotBranch : RedDotNode, IDisposable
    {
        private readonly ReactiveList<RedDotNode> _children = new();

        private readonly ReactiveProperty<int> _activeChildrenCount = new();
        private readonly ReactiveProperty<int> _totalCount = new();
        private readonly ReactiveProperty<bool> _useActiveCount = new(true);

        private readonly Dictionary<RedDotNode, IDisposable> _childSubscriptions = new();

        public RedDotBranch(string name, RedDotBranch parent = null, IEnumerable<RedDotNode> children = null) : base(name, parent)
        {
            // 추가 되거나 삭제될 때 _activeChildrenCount 관리
            _children.OnChange
                .Select(x => _children.Count(n => n.IsActive.CurrentValue))
                .Prepend(_children.Count(n => n.IsActive.CurrentValue))
                .Subscribe(_activeChildrenCount);

            // 추가될 때 _totalCount 관리
            _children.OnChange
                .Where(x => x.Type == ReactiveList<RedDotNode>.ChangeType.Add)
                .Select(x => x.Item)
                .Subscribe(x =>
                {
                    _childSubscriptions[x] = x.Count.Subscribe(UpdateTotalCount);
                }).AddTo(_disposables);

            // 삭제될 때 _totalCount 관리
            _children.OnChange
                .Where(x => x.Type == ReactiveList<RedDotNode>.ChangeType.Remove)
                .Select(x => x.Item)
                .Subscribe(x =>
                {
                    if (_childSubscriptions.TryGetValue(x, out var subscription))
                    {
                        subscription.Dispose();
                        _childSubscriptions.Remove(x);
                    }
                    UpdateTotalCount();
                });

            // 초기 자식 노드 추가
            foreach (var child in children ?? Array.Empty<RedDotNode>())
            {
                _children.Add(child);
            }
        }

        private void UpdateTotalCount()
        {
            _totalCount.Value = _children.Sum(n => n.Count.CurrentValue);
        }

        public static RedDotNode GetNodeAbs(string path) => Root.GetNode(path);

        protected override ReadOnlyReactiveProperty<int> CreateCountProp()
        {
            return _useActiveCount
                .Select(x => (x ? _activeChildrenCount : _totalCount).AsObservable())
                .Switch()
                .ToReadOnlyReactiveProperty();
        }

        public override void Clear()
        {
            foreach (var child in _children)
            {
                child.Clear();
            }
        }

        public RedDotNode GetNode(string path)
        {
            path = path.TrimStart('.');
            var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var current = this as RedDotNode;
            foreach (var name in names)
            {
                if (current is not RedDotBranch branch)
                {
                    return null;
                }
                var child = branch._children.Find(n => n.Name.Equals(name));
                if (child == null)
                {
                    return null;
                }
                current = child;
            }
            return current;
        }

        public RedDotBranch EnsureBranch(string path)
        {
            path = path.TrimStart('.');
            var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            return EnsureBranch(names);
        }

        private RedDotBranch EnsureBranch(string[] path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            var current = this;
            foreach (var name in path)
            {
                var child = current._children.Find(n => n.Name.Equals(name));
                if (child == null)
                {
                    child = new RedDotBranch(name, current);
                    current._children.Add(child);
                }
                else if (child is not RedDotBranch branch)
                {
                    throw new InvalidOperationException($"Node with name '{name}' already exists and is not a branch.");
                }
                current = (RedDotBranch)child;
            }
            return current;
        }

        public RedDotLeaf EnsureLeaf(string path)
        {
            path = path.TrimStart('.');
            var names = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            var branch = EnsureBranch(names[..^1]);
            var leafName = names[^1];
            var existingNode = branch._children.Find(n => n.Name.Equals(leafName));
            if (existingNode != null)
            {
                if (existingNode is RedDotLeaf existingLeaf)
                {
                    return existingLeaf;
                }
                else
                {
                    throw new InvalidOperationException($"Node with name '{leafName}' already exists and is not a leaf.");
                }
            }
            else
            {
                var newLeaf = new RedDotLeaf(leafName, branch);
                branch._children.Add(newLeaf);
                return newLeaf;
            }
        }
    }
}
