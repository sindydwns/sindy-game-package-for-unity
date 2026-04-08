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

    public class ListViewModel<T> : ListViewModel where T : IViewModel
    {
        public void SetItems(IReadOnlyList<T> list)
        {
            if (list == null)
            {
                base.SetItems(null);
                return;
            }
            var converted = new IViewModel[list.Count];
            for (int i = 0; i < list.Count; i++) converted[i] = list[i];
            base.SetItems(converted);
        }
    }
}
