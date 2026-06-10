using Sindy.Common;
using Sindy.Reactive;
using Sindy.View;

namespace Sindy.RedDot
{
    /// <summary>
    /// RedDotNode의 카운트를 따라가는 PropModel&lt;int&gt;.
    /// <see cref="RedDotFeature"/>의 외부 모델 주입 생성자에 그대로 전달할 수 있다:
    /// <c>new RedDotFeature(new RedDotModel("inven.new_item"))</c>
    /// </summary>
    public class RedDotModel : PropModel<int>
    {
        /// <summary>Node가 null인 경우 RedDotNode.Root를 사용</summary>
        public RedDotNode Node { get; private set; }

        public RedDotModel(RedDotNode node)
        {
            Node = node ?? RedDotNode.Root;
            Node.Count.Subscribe(Prop).AddTo(disposables);
        }

        public RedDotModel(string path, bool isLeaf = false)
            : this(isLeaf ? RedDotNode.Root.EnsureLeaf(path) : RedDotNode.Root.EnsureBranch(path))
        {
        }
    }
}
