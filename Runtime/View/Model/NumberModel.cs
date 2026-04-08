using R3;

namespace Sindy.View.Model
{
    public class IntPropModel : PropModel<int>
    {
        public ReactiveProperty<int> Number => Prop;

        public IntPropModel() : base(0) { }
        public IntPropModel(int value) : base(value) { }
        public IntPropModel(ReactiveProperty<int> value) : base(value) { }
    }

    public class FloatPropModel : PropModel<float>
    {
        public ReactiveProperty<float> Number => Prop;

        public FloatPropModel() : base(0f) { }
        public FloatPropModel(float value) : base(value) { }
        public FloatPropModel(ReactiveProperty<float> value) : base(value) { }
    }
}
