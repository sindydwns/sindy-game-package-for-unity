using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Sindy.Common;

namespace Sindy.View
{
    public abstract class ViewModelFeature : ViewModel
    {
    }

    public class ViewModel : IViewModel
    {
        protected readonly List<IDisposable> disposables = new();
        private readonly Dictionary<string, IViewModel> children = new();
        private Dictionary<Type, ViewModelFeature> features;
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            disposables.DisposeAll();
            disposables.Clear();
            IsDisposed = true;
        }

        public ViewModel With<T>(T feature) where T : ViewModelFeature
        {
            features ??= new();
            features[typeof(T)] = feature;
            feature.AddTo(this);
            return this;
        }

        public T Feature<T>() where T : ViewModelFeature
        {
            if (features != null && features.TryGetValue(typeof(T), out var f))
                return (T)f;
            return default;
        }

        protected void Dispose(Result _) => Dispose();
        protected static void DoNothing<T>(T _) { }

        public virtual IViewModel this[string name]
        {
            get => GetChild<IViewModel>(name);
            set => AddChild(name, value);
        }

        public T GetChild<T>(string name) where T : IViewModel
        {
            var tokens = name.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var token = tokens.FirstOrDefault();
            if (token == null || !children.ContainsKey(token))
            {
                return default;
            }
            if (tokens.Length > 1)
            {
                var subName = string.Join(".", tokens.Skip(1));
                return children[token].GetChild<T>(subName);
            }
            else
            {
                return children[token] is T typed ? typed : default;
            }
        }

        public ViewModel AddChild(string name, IViewModel model, bool disposeWithParent = true)
        {
            var tokens = name.Split(".", StringSplitOptions.RemoveEmptyEntries);
            var token = tokens.FirstOrDefault() ?? throw new ArgumentException("Invalid view name");
            var child = children.TryGetValue(token, out var existingChild) ? existingChild : null;
            if (tokens.Length > 1)
            {
                if (child == null)
                {
                    child = new ViewModel();
                    children.Add(token, child);
                }
                var subName = string.Join(".", tokens.Skip(1));
                ((ViewModel)child).AddChild(subName, model, disposeWithParent);
            }
            else
            {
                children[token] = model;
                if (disposeWithParent)
                {
                    model.AddTo(this);
                }
            }
            return this;
        }

        public void AddTo(IDisposeChain disposable) => disposable.AddChild(this);
        public void AddChild(IDisposeChain child) => disposables.Add(child);
    }
}
