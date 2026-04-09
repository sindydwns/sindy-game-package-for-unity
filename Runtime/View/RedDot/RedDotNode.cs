using System;
using System.Collections.Generic;
using R3;
using Sindy.Common;
using Sindy.Reactive;

namespace Sindy.RedDot
{
    public abstract class RedDotNode : IRedDotNode, IDisposable
    {
        public static RedDotBranch Root { get; } = new(string.Empty);

        /// <summary>
        /// 노드의 단일 이름. 노드의 전체 경로는 Path 속성을 참조.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 노드의 전체 경로. 부모 노드의 이름과 현재 노드의 이름을 '.'으로 구분하여 구성.
        /// </summary>
        public string Path => _pathProp.Value;
        private readonly ReactiveProperty<string> _pathProp = new();

        public RedDotBranch Parent => _parent.Value;
        private readonly ReactiveProperty<RedDotBranch> _parent = new();

        public ReadOnlyReactiveProperty<bool> IsActive { get; private set; }

        /// <summary>
        /// 유효한 자식의 카운트 합산
        /// </summary>
        public ReadOnlyReactiveProperty<int> Count { get; private set; }
        public object UserData { get; set; }

        protected List<IDisposable> _disposables = new();

        public RedDotNode(string name, RedDotBranch parent = null)
        {
            Name = name.Trim().Trim('.');
            _parent.Value = parent;
            _parent.Subscribe(UpdatePath);

            Count = CreateCountProp();
            IsActive = Count.Select(x => x > 0).ToReadOnlyReactiveProperty();
        }

        private void UpdatePath() => _pathProp.Value = $"{Parent?.Path ?? string.Empty}.{Name}".TrimStart('.');

        protected abstract ReadOnlyReactiveProperty<int> CreateCountProp();

        public override string ToString()
        {
            return $"{Path}({Count.CurrentValue})";
        }

        public abstract void Clear();

        public void Dispose()
        {
            _pathProp.Dispose();
            _parent.Dispose();
            _disposables.DisposeAllClear();

            Count.Dispose();
            IsActive.Dispose();
        }
    }
}
