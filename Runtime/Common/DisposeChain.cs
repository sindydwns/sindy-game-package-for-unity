using System.Collections.Generic;

namespace Sindy.Common
{
    public class DisposeChain : IDisposeChain
    {
        private readonly object _lock = new object();
        private readonly List<IDisposeChain> _children = new();
        private bool _isDisposed;

        public void AddTo(IDisposeChain disposable)
        {
            if (disposable == null || ReferenceEquals(this, disposable))
            {
                return;
            }

            disposable.AddChild(this);
        }

        public void AddChild(IDisposeChain child)
        {
            if (child == null || ReferenceEquals(this, child))
            {
                return;
            }

            var shouldDisposeImmediately = false;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    shouldDisposeImmediately = true;
                }
                else
                {
                    _children.Add(child);
                }
            }

            if (shouldDisposeImmediately)
            {
                child.Dispose();
            }
        }

        public void Dispose()
        {
            List<IDisposeChain> childrenToDispose;

            lock (_lock)
            {
                if (_isDisposed)
                {
                    return;
                }

                _isDisposed = true;
                childrenToDispose = new(_children);
                _children.Clear();
            }

            foreach (var child in childrenToDispose)
            {
                child?.Dispose();
            }
        }
    }
}
