using System.Collections.Generic;
using System.Threading;
using R3;

namespace Sindy.Http
{
    /// <summary>
    /// 인증 토큰을 자동 주입하는 IHttpClient 래퍼.
    ///
    /// 동작:
    ///   1. 모든 요청에 "Authorization: Bearer {AccessToken}" 헤더를 추가합니다.
    ///   2. 401 Unauthorized 응답 시 TokenRefreshService로 토큰을 갱신합니다.
    ///   3. 갱신 성공 시 원래 요청을 새 토큰으로 재전송합니다.
    ///   4. 갱신 실패 시 토큰을 초기화하고 Unauthorized 에러를 전파합니다.
    /// </summary>
    public class AuthenticatedHttpClient : IHttpClient
    {
        private readonly IHttpClient inner;
        private readonly TokenModel tokenModel;
        private readonly TokenRefreshService refreshService;

        public AuthenticatedHttpClient(
            IHttpClient inner,
            TokenModel tokenModel,
            TokenRefreshService refreshService)
        {
            this.inner          = inner;
            this.tokenModel     = tokenModel;
            this.refreshService = refreshService;
        }

        public Observable<HttpResponse<T>> Send<T>(HttpRequest request, CancellationToken ct = default)
        {
            request = InjectAuthHeader(request);

            return inner.Send<T>(request, ct)
                .Catch<HttpResponse<T>, HttpError>(err =>
                {
                    if (err.Kind != HttpErrorKind.Unauthorized || !tokenModel.HasToken)
                        return Observable.Throw<HttpResponse<T>>(err);

                    // 401 → 토큰 갱신 시도
                    return refreshService.Refresh(tokenModel.RefreshToken.Value)
                        .Select(tokenRes =>
                        {
                            tokenModel.Update(tokenRes.AccessToken, tokenRes.RefreshToken);
                            return tokenRes;
                        })
                        .SelectMany(_ =>
                        {
                            // 새 토큰으로 원래 요청 재전송
                            var retryReq = InjectAuthHeader(request);
                            return inner.Send<T>(retryReq, ct);
                        })
                        .Catch<HttpResponse<T>, HttpError>(refreshErr =>
                        {
                            // 갱신 실패 → 토큰 초기화
                            tokenModel.Clear();
                            return Observable.Throw<HttpResponse<T>>(refreshErr);
                        });
                });
        }

        public Observable<HttpResponse<T>> Get<T>(
            string url, Dictionary<string, string> headers = null, CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.GET, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Post<T>(
            string url, string jsonBody, Dictionary<string, string> headers = null, CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.POST, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Put<T>(
            string url, string jsonBody, Dictionary<string, string> headers = null, CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.PUT, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Delete<T>(
            string url, Dictionary<string, string> headers = null, CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.DELETE, Headers = headers }, ct);

        private HttpRequest InjectAuthHeader(HttpRequest request)
        {
            if (!tokenModel.HasToken) return request;

            request.Headers ??= new Dictionary<string, string>();
            request.Headers["Authorization"] = $"Bearer {tokenModel.AccessToken.Value}";
            return request;
        }
    }
}
