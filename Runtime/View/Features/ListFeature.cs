using System;
using System.Collections.Generic;

namespace Sindy.View.Features
{
    /// <summary>
    /// 단순(비가상화) 리스트 능력. <see cref="FeatureViews.ListFeatureView"/>와 1:1 대칭.
    /// 아이템은 자식 ViewModel 목록이며, 교체는 <see cref="SetItems"/>로 일괄 수행한다.
    /// 대량 데이터/가상화가 필요하면 ScrollerFeature를 사용할 것.
    /// </summary>
    public class ListFeature : ModelFeature
    {
        public PropModel<IReadOnlyList<IViewModel>> Items { get; }

        public ListFeature(IReadOnlyList<IViewModel> items = null)
        {
            Items = new PropModel<IReadOnlyList<IViewModel>>(items ?? Array.Empty<IViewModel>());
            Items.AddTo(this);
        }

        public void SetItems(IReadOnlyList<IViewModel> items)
        {
            Items.Value = items ?? Array.Empty<IViewModel>();
        }
    }
}
