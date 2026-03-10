using System;
using System.Collections.Generic;
using System.Linq;
using Sindy.Common;

namespace Sindy.View
{
    public class ViewModel : IViewModel
    {
        protected readonly List<IDisposable> disposables = new();
        private readonly Dictionary<string, IViewModel> children = new();

        public virtual void Dispose()
        {
            disposables.DisposeAll();
            disposables.Clear();
        }

        public virtual IViewModel this[string name]
        {
            get => GetView<IViewModel>(name);
            set => SetView(name, value);
        }

        public T GetView<T>(string name) where T : IViewModel
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
                return children[token].GetView<T>(subName);
            }
            else
            {
                return (T)children[token];
            }
        }

        public void SetView(string name, IViewModel model)
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
                ((ViewModel)child).SetView(subName, model);
            }
            else
            {
                children.Add(token, model);
            }
        }

        public void AddTo(IDisposeChain disposable) => disposable.AddChild(this);
        public void AddChild(IDisposeChain child) => disposables.Add(child);
    }
}
