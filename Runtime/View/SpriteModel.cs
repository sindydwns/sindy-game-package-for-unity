using R3;
using UnityEngine;

namespace Sindy.View.Model
{
    public class SpritePropModel : PropModel<Sprite>
    {
        public ReactiveProperty<Sprite> Sprite => Prop;

        public SpritePropModel() : base() { }
        public SpritePropModel(Sprite sprite) : base(sprite) { }
        public SpritePropModel(ReactiveProperty<Sprite> sprite) : base(sprite) { }
    }

    public class SpriteStreamModel : StreamModel<Sprite>
    {
        public Subject<Sprite> Sprite => Stream;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
