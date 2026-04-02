using R3;

namespace Sindy.View.Model
{
    public class StringPropModel : PropModel<string>
    {
        public ReactiveProperty<string> Text => Prop;

        public StringPropModel() : base(string.Empty) { }
        public StringPropModel(string text) : base(text) { }
        public StringPropModel(ReactiveProperty<string> text) : base(text) { }
    }

    public class StringSubjModel : SubjModel<string>
    {
        public Subject<string> Text => Subj;

        public override void Dispose()
        {
            base.Dispose();
        }
    }
}
