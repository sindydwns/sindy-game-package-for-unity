using System;
using R3;

namespace Sindy.RedDot
{
    public class RedDotLeaf : RedDotNode, IDisposable
    {
        public new ReactiveProperty<int> Count { get; private set; }

        public RedDotLeaf(string name, RedDotBranch parent = null, int count = 0) : base(name, parent)
        {
            Count.Value = count;
        }

        public override void Clear()
        {
            Count.Value = 0;
        }

        protected override ReadOnlyReactiveProperty<int> CreateCountProp()
        {
            Count = new ReactiveProperty<int>();
            return Count.ToReadOnlyReactiveProperty();
        }
    }
}
