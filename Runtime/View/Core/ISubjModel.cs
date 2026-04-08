using R3;

namespace Sindy.View
{
    public interface ISubjModel { }

    public interface ISubjModel<T> : IViewModel, ISubjModel
    {
        Subject<T> Subj { get; }
    }
}
