using System;

namespace Sindy.Http
{
    public enum HttpErrorKind
    {
        Network,        // 연결 실패 (no internet, DNS failure)
        Timeout,        // 요청 시간 초과
        Unauthorized,   // 401 — 인증 필요 / 토큰 만료
        Forbidden,      // 403 — 접근 권한 없음
        NotFound,       // 404 — 리소스 없음
        ServerError,    // 5xx — 서버 오류
        ParseFailed,    // 응답 JSON 역직렬화 실패
        Cancelled,      // 요청 취소
    }

    public class HttpError : Exception
    {
        public int          StatusCode { get; }
        public string       RawBody    { get; }
        public HttpErrorKind Kind      { get; }

        public HttpError(int statusCode, string rawBody, HttpErrorKind kind, Exception inner = null)
            : base($"[{kind}] HTTP {statusCode}: {rawBody}", inner)
        {
            StatusCode = statusCode;
            RawBody    = rawBody;
            Kind       = kind;
        }

        /// <summary>상태 코드로부터 HttpErrorKind를 추론합니다.</summary>
        public static HttpErrorKind KindFromStatusCode(int statusCode) => statusCode switch
        {
            401 => HttpErrorKind.Unauthorized,
            403 => HttpErrorKind.Forbidden,
            404 => HttpErrorKind.NotFound,
            >= 500 => HttpErrorKind.ServerError,
            _ => HttpErrorKind.Network,
        };
    }
}
