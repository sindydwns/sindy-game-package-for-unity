using System;

namespace Sindy.Common
{
    public interface IDisposeChain : IDisposable
    {
        // a.AddTo(b) 하면 a가 b에 종속되어, b가 Dispose 될 때 a도 함께 Dispose 된다.
        public void AddTo(IDisposeChain disposable);

        // a.AddChild(b) 하면 a가 b의 부모가 되어, a가 Dispose 될 때 b도 함께 Dispose 된다.
        public void AddChild(IDisposeChain child);
    }
}
