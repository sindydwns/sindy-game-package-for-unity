using R3;
using UnityEngine;

namespace Sindy.View.Model
{
    public class ColorPropModel : PropModel<Color>
    {
        public ReactiveProperty<Color> Color => Prop;

        public ColorPropModel() : base(UnityEngine.Color.white) { }
        public ColorPropModel(Color color) : base(color) { }
        public ColorPropModel(ReactiveProperty<Color> color) : base(color) { }
    }

    public class ColorStreamModel : StreamModel<Color>
    {
        public Subject<Color> Color => Stream;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
