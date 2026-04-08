using System;
using System.Collections.Generic;
using R3;

namespace Sindy.View.Model
{
    public class ListViewModel : ViewModel
    {
        private readonly ReactiveProperty<IReadOnlyList<IViewModel>> items = new(Array.Empty<IViewModel>());
        public ReadOnlyReactiveProperty<IReadOnlyList<IViewModel>> Items => items;

        public void SetItems(IReadOnlyList<IViewModel> list)
        {
            items.Value = list ?? Array.Empty<IViewModel>();
        }

        public override void Dispose()
        {
            base.Dispose();
            items.Dispose();
        }
    }

    public class ListViewModel<T> : ViewModel where T : IViewModel
    {
        private readonly ReactiveProperty<IReadOnlyList<T>> items = new(Array.Empty<T>());
        public ReadOnlyReactiveProperty<IReadOnlyList<T>> Items => items;

        public void SetItems(IReadOnlyList<T> list)
        {
            items.Value = list ?? Array.Empty<T>();
        }

        public override void Dispose()
        {
            base.Dispose();
            items.Dispose();
        }
    }
}
