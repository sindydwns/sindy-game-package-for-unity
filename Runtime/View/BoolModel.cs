using R3;

namespace Sindy.View
{
    public class BoolPropModel : PropModel<bool>
    {
        public ReactiveProperty<bool> Show => Prop;

        public BoolPropModel() { }
        public BoolPropModel(bool show) : base(show) { }
        public BoolPropModel(ReactiveProperty<bool> show) : base(show) { }
    }

    public class BoolStreamModel : StreamModel<bool>
    {
        public Subject<bool> Bool => Stream;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
