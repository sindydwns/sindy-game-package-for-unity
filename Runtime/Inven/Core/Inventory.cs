using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

namespace Sindy.Inven
{
    /// <summary>
    /// 저장(<see cref="IInventoryStore{TKey}"/>)·런타임(이 클래스)·관심사(<see cref="IInventoryFeature{TKey}"/>)를 분리한 인벤토리 코어.
    /// <para>
    /// 키는 제네릭(string·int·Entity 전부 같은 코드). 저장 형식은 프로젝트가 스토어로 정한다.
    /// 반응형(<see cref="CountProp"/>·<see cref="Changes"/>)은 이 객체가 갖고, 스토어 교체(<see cref="Rebind"/>) 후에도 구독은 살아 있다.
    /// </para>
    /// <para>
    /// Add 실행 순서(동기, 같은 호출 안):
    /// ① 전 Feature CanAccept — 하나라도 false면 즉시 false, 상태 무변경
    /// ② store.Set ③ CountProp 갱신(같은 값이면 방출 생략) ④ 전 Feature OnChanged(재진입 가드 ON) ⑤ Changes.OnNext
    /// </para>
    /// <para>메인 스레드 전용(R3 ReactiveProperty와 같다). 예외는 인자 오류·재진입·Feature 중복 등록에만 던지고, 게이트 거부는 false + reason.</para>
    /// </summary>
    public sealed class Inventory<TKey> : IInventory<TKey>, IDisposable
    {
        private IInventoryStore<TKey> store;
        private readonly IEqualityComparer<TKey> comparer;
        private readonly List<IInventoryFeature<TKey>> features = new();
        private readonly Subject<ItemChange<TKey>> changes = new();
        private Dictionary<TKey, ReactiveProperty<long>> props;   // 지연 생성 — 구독된 키만
        private Dictionary<TKey, long> scratch;                    // HasAll/Pay 합산용, 재사용
        private bool scratchBusy;
        private bool notifying;                                    // Feature.OnChanged 재진입 가드
        private bool disposed;

        public Inventory(IInventoryStore<TKey> store, IEqualityComparer<TKey> comparer = null)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>현재 스토어.</summary>
        public IInventoryStore<TKey> Store => store;
        /// <summary>키 비교자. CountProp 캐시·합산에 쓰인다.</summary>
        public IEqualityComparer<TKey> Comparer => comparer;
        /// <summary>등록 순서대로의 Feature 목록.</summary>
        public IReadOnlyList<IInventoryFeature<TKey>> Features => features;
        public bool IsDisposed => disposed;

        // ─────────────────────────── 합성 ───────────────────────────

        /// <summary>Feature를 등록한다(체이닝). 같은 타입 중복은 예외. 등록 즉시 Attach.</summary>
        public Inventory<TKey> With(IInventoryFeature<TKey> feature)
        {
            if (feature == null) throw new ArgumentNullException(nameof(feature));
            ThrowIfDisposed();
            var type = feature.GetType();
            for (var i = 0; i < features.Count; i++)
            {
                if (features[i].GetType() == type)
                    throw new InvalidOperationException($"Feature {type.Name} is already registered.");
            }
            features.Add(feature);
            feature.Attach(this);
            return this;
        }

        public T Feature<T>() where T : class, IInventoryFeature<TKey>
        {
            for (var i = 0; i < features.Count; i++)
            {
                if (features[i] is T t) return t;
            }
            return null;
        }

        // ─────────────────────────── 읽기 ───────────────────────────

        public long Count(TKey key)
        {
            return store.TryGet(key, out var count) ? count : 0;
        }

        public ReadOnlyReactiveProperty<long> CountProp(TKey key)
        {
            ThrowIfDisposed();
            props ??= new Dictionary<TKey, ReactiveProperty<long>>(comparer);
            if (!props.TryGetValue(key, out var prop))
            {
                prop = new ReactiveProperty<long>(Count(key));
                props[key] = prop;
            }
            return prop;
        }

        public Observable<ItemChange<TKey>> Changes => changes;

        public IEnumerable<KeyValuePair<TKey, long>> Entries => store.All();

        public bool HasAll(IEnumerable<KeyValuePair<TKey, long>> costs)
        {
            if (costs == null) throw new ArgumentNullException(nameof(costs));
            var agg = Aggregate(costs, out var rented);
            try
            {
                foreach (var kv in agg)
                {
                    if (Count(kv.Key) < kv.Value) return false;
                }
                return true;
            }
            finally { Release(rented); }
        }

        // ─────────────────────────── 게이트 ───────────────────────────

        public bool CanAdd(TKey key, long n, out string reason)
        {
            ThrowIfBadAmount(n);
            return Gate(key, n, out reason);
        }

        public bool CanRemove(TKey key, long n, out string reason)
        {
            ThrowIfBadAmount(n);
            if (Count(key) < n)
            {
                reason = InventoryReason.Insufficient;
                return false;
            }
            return Gate(key, -n, out reason);
        }

        /// <summary>이동 가능 여부와 거부 이유(주는 쪽·받는 쪽 순).</summary>
        public bool CanMove(IInventory<TKey> to, TKey key, long n, out string reason)
        {
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (ReferenceEquals(to, this)) throw new ArgumentException("Cannot move to self.", nameof(to));
            return CanRemove(key, n, out reason) && to.CanAdd(key, n, out reason);
        }

        private bool Gate(TKey key, long delta, out string reason)
        {
            ThrowIfDisposed();
            ThrowIfReentrant();
            for (var i = 0; i < features.Count; i++)
            {
                if (!features[i].CanAccept(key, delta, out reason))
                {
                    return false;
                }
            }
            reason = null;
            return true;
        }

        // ─────────────────────────── 쓰기 ───────────────────────────

        public bool Add(TKey key, long n, string reason = null)
        {
            if (!CanAdd(key, n, out _)) return false;
            Apply(key, n, reason);
            return true;
        }

        public bool Remove(TKey key, long n, string reason = null)
        {
            if (!CanRemove(key, n, out _)) return false;
            Apply(key, -n, reason);
            return true;
        }

        public bool TryMove(IInventory<TKey> to, TKey key, long n, string reason = null)
        {
            if (!CanMove(to, key, n, out _)) return false;
            Apply(key, -n, reason);
            if (to is Inventory<TKey> core)
            {
                core.Apply(key, n, reason);          // 게이트는 이미 통과 — 재검사 없이 적용
            }
            else if (!to.Add(key, n, reason))
            {
                Apply(key, n, reason);               // 외부 구현이 뒤늦게 거부하면 되돌린다
                return false;
            }
            return true;
        }

        public bool Pay(IEnumerable<KeyValuePair<TKey, long>> costs, string reason = null)
        {
            if (costs == null) throw new ArgumentNullException(nameof(costs));
            var agg = Aggregate(costs, out var rented);
            try
            {
                foreach (var kv in agg)
                {
                    if (!CanRemove(kv.Key, kv.Value, out _)) return false;
                }
                foreach (var kv in agg)
                {
                    Apply(kv.Key, -kv.Value, reason);
                }
                return true;
            }
            finally { Release(rented); }
        }

        public void Rebind(IInventoryStore<TKey> store)
        {
            if (store == null) throw new ArgumentNullException(nameof(store));
            ThrowIfDisposed();
            ThrowIfReentrant();
            this.store = store;
            if (props != null)
            {
                foreach (var kv in props)
                {
                    kv.Value.Value = Count(kv.Key);   // 같은 값이면 R3가 방출을 생략한다
                }
            }
            for (var i = 0; i < features.Count; i++)
            {
                features[i].OnRebind();
            }
        }

        // ─────────────────────────── 내부 ───────────────────────────

        private void Apply(TKey key, long delta, string reason)
        {
            var before = Count(key);
            var after = before + delta;
            store.Set(key, after);
            if (props != null && props.TryGetValue(key, out var prop))
            {
                prop.Value = after;
            }
            var change = new ItemChange<TKey>(key, delta, before, after, reason);
            notifying = true;
            try
            {
                for (var i = 0; i < features.Count; i++)
                {
                    try
                    {
                        features[i].OnChanged(in change);
                    }
                    catch (Exception e)
                    {
                        // 계약 위반 — 상태 일관성을 위해 삼키고 다음 Feature로 진행
                        Debug.LogException(e);
                    }
                }
            }
            finally
            {
                notifying = false;
            }
            changes.OnNext(change);
        }

        /// <summary>같은 키를 합산한다. 반환된 사전은 <see cref="Release"/>로 돌려준다.</summary>
        private Dictionary<TKey, long> Aggregate(IEnumerable<KeyValuePair<TKey, long>> costs, out bool rented)
        {
            Dictionary<TKey, long> dict;
            if (scratchBusy)
            {
                dict = new Dictionary<TKey, long>(comparer);   // 재진입(Changes 구독자 등) — 새로 할당
                rented = false;
            }
            else
            {
                scratch ??= new Dictionary<TKey, long>(comparer);
                scratch.Clear();
                scratchBusy = true;
                dict = scratch;
                rented = true;
            }
            foreach (var kv in costs)
            {
                if (kv.Value < 0) throw new ArgumentOutOfRangeException(nameof(costs), $"Cost of {kv.Key} must be >= 0.");
                if (kv.Value == 0) continue;
                dict.TryGetValue(kv.Key, out var acc);
                dict[kv.Key] = acc + kv.Value;
            }
            return dict;
        }

        private void Release(bool rented)
        {
            if (rented) scratchBusy = false;
        }

        private static void ThrowIfBadAmount(long n)
        {
            if (n <= 0) throw new ArgumentOutOfRangeException(nameof(n), "Amount must be > 0.");
        }

        private void ThrowIfReentrant()
        {
            if (notifying)
                throw new InvalidOperationException("Inventory cannot be modified inside IInventoryFeature.OnChanged.");
        }

        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>Changes 완료 통지 → 모든 CountProp Dispose → Feature Detach(역순).</summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            changes.OnCompleted();
            changes.Dispose();
            if (props != null)
            {
                foreach (var kv in props) kv.Value.Dispose();
                props.Clear();
            }
            for (var i = features.Count - 1; i >= 0; i--)
            {
                features[i].Detach();
            }
            features.Clear();
        }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("[Inventory");
            foreach (var kv in store.All())
            {
                sb.Append(' ').Append(kv.Key).Append('×').Append(kv.Value);
            }
            return sb.Append(']').ToString();
        }
    }
}
