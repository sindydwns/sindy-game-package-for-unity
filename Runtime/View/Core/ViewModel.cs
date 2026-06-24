using System;
using System.Collections.Generic;
using System.Linq;
using R3;
using Sindy.Common;

namespace Sindy.View
{
    public interface IViewModel : IDisposeChain
    {
        public T GetChild<T>(string name) where T : IViewModel;
        public IViewModel this[string name] { get; set; }
        public IEnumerable<string> GetChildNames();
        public IEnumerable<Type> GetFeatureTypes();
        public T Feature<T>() where T : ModelFeature;
    }

    public class ViewModel : IViewModel
    {
        protected readonly List<IDisposable> disposables = new();
        private readonly Dictionary<string, IViewModel> children = new();
        private Dictionary<Type, ModelFeature> features;
        public bool IsDisposed { get; private set; }

        public virtual void Dispose()
        {
            disposables.DisposeAllClear();
            IsDisposed = true;
        }

        public ViewModel With<T>(T feature) where T : ModelFeature
        {
            features ??= new();
            features[typeof(T)] = feature;
            feature.AddTo(this);
            return this;
        }

        public T Feature<T>() where T : ModelFeature
        {
            if (features != null && features.TryGetValue(typeof(T), out var f))
                return (T)f;
            return default;
        }

        public IEnumerable<Type> GetFeatureTypes()
        {
            return features?.Keys ?? Enumerable.Empty<Type>();
        }

        /// <summary>최상위 레벨 자식 모델의 키 목록. (점 표기 하위 키는 포함하지 않는다.)</summary>
        public IEnumerable<string> GetChildNames()
        {
            return children.Keys;
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

        public IViewModel AddChild(string name, IViewModel model, bool disposeWithParent = true)
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
                    ((IDisposeChain)child).AddTo(this);
                }
                var subName = string.Join(".", tokens.Skip(1));
                if (child is ViewModel vmChild)
                {
                    vmChild.AddChild(subName, model, disposeWithParent);
                }
                else
                {
                    child[subName] = model;
                }
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
