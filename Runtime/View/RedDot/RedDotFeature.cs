using R3;
using Sindy.Common;
using Sindy.View;

namespace Sindy.RedDot
{
    /// <summary>
    /// 알림 뱃지(RedDot) 능력. <see cref="RedDotFeatureView"/>와 1:1 대칭.
    /// RedDotNode 트리의 카운트를 구독하거나, 외부 PropModel을 직접 주입할 수 있다.
    /// </summary>
    public class RedDotFeature : ModelFeature
    {
        public PropModel<int> Count { get; }

        /// <summary>node가 null이면 RedDotNode.Root를 사용한다.</summary>
        public RedDotNode Node { get; }

        public RedDotFeature(RedDotNode node)
        {
            Node = node ?? RedDotNode.Root;
            Count = new PropModel<int>();
            Count.AddTo(this);
            Node.Count.Subscribe(v => Count.Value = v).AddTo(disposables);
        }

        public RedDotFeature(string path, bool isLeaf = false)
            : this(isLeaf ? RedDotNode.Root.EnsureLeaf(path) : RedDotNode.Root.EnsureBranch(path))
        {
        }

        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public RedDotFeature(PropModel<int> external)
        {
            Count = external ?? throw new System.ArgumentNullException(nameof(external));
            Count.AddTo(this);
        }
    }
}
