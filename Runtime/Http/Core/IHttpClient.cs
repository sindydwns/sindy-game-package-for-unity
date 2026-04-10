using System.Collections.Generic;
using System.Threading;
using R3;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 클라이언트 인터페이스.
    /// 성공 시 OnNext(response) → OnCompleted.
    /// 실패 시 OnError(HttpError).
    /// </summary>
    public interface IHttpClient
    {
        Observable<HttpResponse<T>> Send<T>(HttpRequest request, CancellationToken ct = default);

        Observable<HttpResponse<T>> Get<T>(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);

        Observable<HttpResponse<T>> Post<T>(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);

        Observable<HttpResponse<T>> Put<T>(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);

        Observable<HttpResponse<T>> Delete<T>(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default);
    }
}
