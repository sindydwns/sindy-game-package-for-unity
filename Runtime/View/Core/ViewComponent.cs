using System;

namespace Sindy.View
{
    /// <summary>
    /// [Deprecated] 트리 매핑 기능은 <see cref="SindyComponent"/>에 통합되었다.
    /// 새 코드는 <see cref="SindyComponent"/>를 직접 사용한다.
    ///
    /// 이 타입은 기존 씬/프리팹에 직렬화된 컴포넌트 참조를 유지하기 위한 호환용 셸로만 남아 있으며,
    /// 자체 동작은 없다(트리 API는 베이스에서 상속). 다음 메이저 버전에서 제거 예정.
    /// </summary>
    [Obsolete("ViewComponent는 SindyComponent에 통합되었습니다. SindyComponent를 직접 사용하세요. 다음 메이저에서 제거됩니다.")]
    public class ViewComponent : SindyComponent
    {
    }
}
