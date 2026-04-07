
using System;
using System.Collections.Generic;
using System.Linq;
using R3;

namespace Sindy.Reactive
{
    /// <summary>
    /// ReactiveList의 요소들이 특정 조건을 만족하는지 추적하는 클래스입니다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ReactiveListCondition<T> : IDisposable
    {
        private readonly ReactiveProperty<bool> all = new();
        public ReadOnlyReactiveProperty<bool> All => all;
        private readonly ReactiveProperty<bool> any = new();
        public ReadOnlyReactiveProperty<bool> Any => any;

        private readonly IReadOnlyReactiveList<T> source;
        private readonly Dictionary<T, CompositeDisposable> disposables = new();
        private readonly Func<T, ReadOnlyReactiveProperty<bool>> predicate;

        public ReactiveListCondition(
            IReadOnlyReactiveList<T> source,
            Func<T, ReadOnlyReactiveProperty<bool>> predicate)
        {
            this.source = source;
            this.predicate = predicate;
            foreach (var t in source)
            {
                OnAdded(t);
            }
            source.OnAdded += OnAdded;
            source.OnRemoved += OnRemoved;
        }

        private void OnAdded(T t)
        {
            var prop = predicate(t);
            var dis = prop.Subscribe(UpdateProperty);
            if (disposables.ContainsKey(t) == false)
            {
                disposables[t] = new CompositeDisposable();
            }
            disposables[t].Add(dis);
        }

        private void OnRemoved(T t)
        {
            if (disposables.TryGetValue(t, out var compositeDisposable))
            {
                compositeDisposable.Dispose();
                disposables.Remove(t);
            }
            UpdateProperty();
        }

        private void UpdateProperty(bool b) => UpdateProperty();
        private void UpdateProperty()
        {
            all.Value = source.Select(x => predicate(x)).All(x => x.CurrentValue);
            any.Value = source.Select(x => predicate(x)).Any(x => x.CurrentValue);
        }

        public void Dispose()
        {
            all.Dispose();
            any.Dispose();
            source.OnAdded -= OnAdded;
            source.OnRemoved -= OnRemoved;
            foreach (var kv in disposables)
            {
                kv.Value.Dispose();
            }
        }
    }
}
