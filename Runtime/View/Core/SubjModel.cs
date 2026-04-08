using R3;

namespace Sindy.View
{
    public class SubjModel<T> : ViewModel, ISubjModel<T>
    {
        public Subject<T> Subj { get; } = new();

        public override void Dispose()
        {
            base.Dispose();
            Subj.Dispose();
        }
    }
}
