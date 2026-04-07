using R3;
using Sindy.Reactive;
using Sindy.View;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.RedDot
{
    public class RedDotComponent : SindyComponent<RedDotModel>
    {
        [SerializeField] private GameObject dot;
        [SerializeField] private MaskableGraphic text;
        /// <summary>
        /// text가 표시 되지 않을 경우 dot의 크기를 조절하기 위한 스케일러.
        /// </summary>
        [SerializeField] private float scaler = 0.5f;

        protected override void Init(RedDotModel model)
        {
            model.Prop.Subscribe(UpdateRedDot).AddTo(disposables);
        }

        protected override void Clear(RedDotModel model)
        {
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
                    text.GetType().GetField("text").SetValue(text, string.Empty);
                }
                else
                {
                    dot.transform.localScale = Vector3.one;
                    text.GetType().GetField("text").SetValue(text, count.ToString());
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

        public RedDotModel(string path) : this(RedDotNode.Root.GetNode(path))
        {
        }
    }
}
