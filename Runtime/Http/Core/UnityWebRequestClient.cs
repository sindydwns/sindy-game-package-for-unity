using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using R3;
using UnityEngine.Networking;

namespace Sindy.Http
{
    /// <summary>
    /// UnityWebRequest 기반 IHttpClient 구현체.
    /// R3 Observable.Create + async/await 패턴으로 비동기 요청을 처리합니다.
    /// Newtonsoft.Json으로 응답을 역직렬화합니다.
    /// </summary>
    public class UnityWebRequestClient : IHttpClient
    {
        private readonly string baseUrl;

        public UnityWebRequestClient(string baseUrl = "")
        {
            this.baseUrl = baseUrl.TrimEnd('/');
        }

        // ── IHttpClient 편의 메서드 ──────────────────────────────────────

        public Observable<HttpResponse<T>> Get<T>(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = Combine(url), Method = HttpMethod.GET, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Post<T>(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = Combine(url), Method = HttpMethod.POST, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Put<T>(
            string url,
            string jsonBody,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = Combine(url), Method = HttpMethod.PUT, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Delete<T>(
            string url,
            Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = Combine(url), Method = HttpMethod.DELETE, Headers = headers }, ct);

        // ── 핵심 전송 로직 ───────────────────────────────────────────────

        public Observable<HttpResponse<T>> Send<T>(HttpRequest request, CancellationToken ct = default)
        {
            return Observable.Create<HttpResponse<T>>(async (observer, token) =>
            {
                // 외부 ct와 Observable 취소 토큰을 결합
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, token);
                await ExecuteAsync<T>(request, observer, linked.Token);
            });
        }

        private async Task ExecuteAsync<T>(
            HttpRequest request,
            Observer<HttpResponse<T>> observer,
            CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                observer.OnCompleted(Result.Failure(new HttpError(0, "Request cancelled before start", HttpErrorKind.Cancelled)));
                return;
            }

            using var uwr = BuildUnityWebRequest(request);

            // UnityWebRequest를 TaskCompletionSource로 래핑 (UniTask 없이 await 지원)
            var tcs = new TaskCompletionSource<bool>();
            var op = uwr.SendWebRequest();
            op.completed += _ => tcs.TrySetResult(true);

            using var reg = ct.Register(() =>
            {
                uwr.Abort();
                tcs.TrySetCanceled();
            });

            try
            {
                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                observer.OnCompleted(Result.Failure(new HttpError(0, "Request timed out or cancelled", HttpErrorKind.Cancelled)));
                return;
            }

            // 네트워크 오류
            if (uwr.result == UnityWebRequest.Result.ConnectionError ||
                uwr.result == UnityWebRequest.Result.DataProcessingError)
            {
                observer.OnCompleted(Result.Failure(new HttpError(0, uwr.error, HttpErrorKind.Network)));
                return;
            }

            // HTTP 오류 (4xx, 5xx)
            var statusCode = (int)uwr.responseCode;
            if (uwr.result == UnityWebRequest.Result.ProtocolError)
            {
                var kind = HttpError.KindFromStatusCode(statusCode);
                observer.OnCompleted(Result.Failure(new HttpError(statusCode, uwr.downloadHandler?.text ?? "", kind)));
                return;
            }

            // 성공 — JSON 역직렬화
            var rawJson = uwr.downloadHandler?.text ?? "";
            try
            {
                var data = typeof(T) == typeof(Unit)
                    ? default
                    : JsonConvert.DeserializeObject<T>(rawJson);

                observer.OnNext(new HttpResponse<T>
                {
                    StatusCode = statusCode,
                    Data = data,
                    RawJson = rawJson,
                });
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                observer.OnCompleted(Result.Failure(new HttpError(statusCode, rawJson, HttpErrorKind.ParseFailed, ex)));
            }
        }

        // ── UnityWebRequest 빌더 ─────────────────────────────────────────

        private static UnityWebRequest BuildUnityWebRequest(HttpRequest req)
        {
            UnityWebRequest uwr;

            switch (req.Method)
            {
                case HttpMethod.GET:
                    uwr = UnityWebRequest.Get(req.Url);
                    break;

                case HttpMethod.DELETE:
                    uwr = UnityWebRequest.Delete(req.Url);
                    // DELETE도 응답 바디를 읽기 위해 DownloadHandler 추가
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    break;

                case HttpMethod.POST:
                case HttpMethod.PUT:
                case HttpMethod.PATCH:
                    var bodyBytes = req.Body != null
                        ? System.Text.Encoding.UTF8.GetBytes(req.Body)
                        : Array.Empty<byte>();
                    uwr = new UnityWebRequest(req.Url, req.Method.ToString())
                    {
                        uploadHandler = new UploadHandlerRaw(bodyBytes),
                        downloadHandler = new DownloadHandlerBuffer(),
                    };
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(req.Method), req.Method, null);
            }

            // 공통 헤더 적용
            if (req.Headers != null)
            {
                foreach (var (key, value) in req.Headers)
                    uwr.SetRequestHeader(key, value);
            }

            return uwr;
        }

        private string Combine(string path)
        {
            if (string.IsNullOrEmpty(baseUrl)) return path;
            return $"{baseUrl}/{path.TrimStart('/')}";
        }
    }
}
