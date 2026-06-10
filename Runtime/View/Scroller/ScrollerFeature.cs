using System;
using System.Collections.Generic;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 가상화 스크롤 능력 (섹션 데이터). <see cref="ScrollerFeatureView"/>와 1:1 대칭.
    /// null 섹션은 생성자에서 자동으로 걸러진다.
    /// 섹션 컬렉션은 한 번에 교체한다 — 갱신하려면 새 ScrollerFeature를 가진 모델을 Bind하거나
    /// 같은 모델이면 허브의 Reload()를 호출한다.
    /// </summary>
    public class ScrollerFeature : ModelFeature
    {
        public IReadOnlyList<Section> Sections { get; }

        public ScrollerFeature(IEnumerable<Section> sections)
        {
            if (sections == null)
            {
                Sections = Array.Empty<Section>();
                return;
            }

            var list = new List<Section>();
            foreach (var s in sections)
            {
                if (s != null) list.Add(s);
            }
            Sections = list;
        }
    }
}
