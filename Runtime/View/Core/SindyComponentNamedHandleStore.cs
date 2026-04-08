using System;
using System.Collections.Generic;

namespace Sindy.View
{
    internal sealed class SindyComponentNamedHandleStore
    {
        private readonly Dictionary<string, IDisposable> handles = new();

        public T Add<T>(T handle, string name = default) where T : IDisposable
        {
            if (string.IsNullOrEmpty(name))
            {
                name = GenerateName(handle);
            }

            if (handles.TryGetValue(name, out var existing))
            {
                existing.Dispose();
            }

            handles[name] = handle;
            return handle;
        }

        public T Get<T>(string name) where T : IDisposable
        {
            if (handles.TryGetValue(name, out var handle) && handle is T typed)
            {
                return typed;
            }

            return default;
        }

        public void Clear()
        {
            foreach (var handle in handles.Values)
            {
                handle.Dispose();
            }

            handles.Clear();
        }

        private string GenerateName<T>(T handle) where T : IDisposable => $"{typeof(T).Name}_{handle.GetHashCode()}";
    }
}
