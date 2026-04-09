using R3;
using Sindy.Reactive;
using Sindy.View;
using UnityEngine;
using TMPro;

namespace Sindy.RedDot
{
    public class RedDotComponent : SindyComponent<RedDotModel>
    {
        [SerializeField] private GameObject dot;
        [SerializeField] private TextMeshProUGUI text;
        /// <summary>
        /// text가 표시 되지 않을 경우 dot의 크기를 조절하기 위한 스케일러.
        /// </summary>
        [SerializeField] private float scaler = 0.5f;
        /// <summary>
        /// model이 지정되지 않은 경우 RedDotNode.Root에서 이 경로로 노드를 찾아 구독.
        /// </summary>
        [SerializeField] private string defaultPath;
        [SerializeField] private bool isLeaf = false;

        private readonly ReactiveProperty<Observable<int>> _countSource = new();

        private void Awake()
        {
            // RedDotBranch.CreateCountProp()과 동일한 Switch 패턴으로
            // _countSource가 바뀔 때마다 최신 Observable을 구독
            _countSource
                .Where(x => x != null)
                .Switch()
                .Where(_ => _countSource.Value != null)
                .Subscribe(UpdateRedDot);

            SetDefaultSource();
        }

        private void SetDefaultSource()
        {
            if (string.IsNullOrEmpty(defaultPath)) return;
            var node = RedDotNode.Root.GetNode(defaultPath);
            node ??= isLeaf
                ? RedDotNode.Root.EnsureLeaf(defaultPath)
                : RedDotNode.Root.EnsureBranch(defaultPath);
            if (node != null)
            {
                _countSource.Value = node.Count.AsObservable();
            }
        }

        protected override void Init(RedDotModel model)
        {
            _countSource.Value = model.Prop.AsObservable();
        }

        protected override void Clear(RedDotModel model)
        {
            SetDefaultSource();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _countSource?.Dispose();
        }

        private void UpdateRedDot(int count)
        {
            if (dot == null)
            {
                return;
            }
            dot.SetActive(count > 0);
            if (text == null)
            {
                dot.transform.localScale = Vector3.one * scaler;
            }
            else
            {
                if (count < 2)
                {
                    dot.transform.localScale = Vector3.one * scaler;
                    text.text = string.Empty;
                }
                else
                {
                    dot.transform.localScale = Vector3.one;
                    text.text = count.ToString();
                }
            }
        }
    }

    public class RedDotModel : ViewModel, IPropModel<int>
    {
        /// <summary>
        /// Node가 null인 경우 RedDotNode.Root를 사용
        /// </summary>
        public RedDotNode Node { get; private set; }

        public ReactiveProperty<int> Prop { get; private set; } = new();

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
