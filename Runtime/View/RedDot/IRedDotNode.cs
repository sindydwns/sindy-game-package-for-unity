using R3;

namespace Sindy.RedDot
{
    public interface IRedDotNode
    {
        /// <summary>
        /// 노드의 단일 이름. 노드의 전체 경로는 Path 속성을 참조.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// 노드의 전체 경로. 부모 노드의 이름과 현재 노드의 이름을 '.'으로 구분하여 구성.
        /// </summary>
        string Path { get; }
        RedDotBranch Parent { get; }
        ReadOnlyReactiveProperty<bool> IsActive { get; }
        ReadOnlyReactiveProperty<int> Count { get; }
        public void Clear();
    }
}
