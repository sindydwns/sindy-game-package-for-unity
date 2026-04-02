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

    public class SpriteSubjModel : SubjModel<Sprite>
    {
        public Subject<Sprite> Sprite => Subj;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
