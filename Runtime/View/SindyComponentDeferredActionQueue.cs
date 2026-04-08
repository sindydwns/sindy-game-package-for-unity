using System;
using System.Collections.Generic;

namespace Sindy.View
{
    internal sealed class SindyComponentDeferredActionQueue
    {
        private readonly List<(Action action, float delay)> actions = new();

        public void Enqueue(Action action, float delay)
        {
            actions.Add((action, delay));
        }

        public IEnumerable<(Action action, float delay)> Drain()
        {
            var snapshot = actions.ToArray();
            actions.Clear();
            return snapshot;
        }
    }
}
