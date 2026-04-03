using R3;
using Sindy.View;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.RedDot
{
    public class RedDotComponent : SindyComponent<IPropModel<int>>
    {
        [SerializeField] private MaskableGraphic text;

        protected override void Init(IPropModel<int> model)
        {
            model.Prop.Subscribe(UpdateRedDot).AddTo(disposables);
        }

        protected override void Clear(IPropModel<int> model)
        {
        }

        private void UpdateRedDot(int count)
        {
            gameObject.SetActive(count > 0);
            if (text != null)
            {
                text.GetType().GetField("text").SetValue(text, count.ToString());
            }
        }
    }

    public class RedDotModel : ViewModel, IPropModel<int>
    {
        public RedDotNode Node { get; private set; }

        public ReactiveProperty<int> Prop { get; private set; } = new();
        public int Value { get => Prop.Value; set => Prop.Value = value; }

        public RedDotModel(RedDotNode node)
        {
            Node = node;
            node.CounterProp.Subscribe(Prop).AddTo(disposables);
        }
    }
}
