using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 요청 발행자. SubjModel을 상속하므로 기존 SubjModel과 동일하게 구독 가능합니다.
    /// model.Request.Send(body) 로 요청을 시작합니다.
    /// </summary>
    public class ApiRequestModel<T> : SubjModel<T>
    {
        /// <summary>요청을 발행합니다. ApiModel 파이프라인이 이를 구독하여 HTTP 전송을 시작합니다.</summary>
        public void Send(T body) => OnNext(body);
    }
}
