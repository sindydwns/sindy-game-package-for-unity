using System.Collections.Generic;
using System.Threading;
using R3;
using Sindy.Http;

namespace Sindy.Test
{
    /// <summary>
    /// 테스트용 IHttpClient 구현체.
    /// 실제 네트워크 없이 미리 설정된 응답을 동기적으로 반환합니다.
    ///
    /// 기본 사용 (즉시 응답):
    ///   var fake = new FakeHttpClient();
    ///   fake.Returns(new UserDto { Name = "Sindy" });
    ///   // → 다음 Send&lt;UserDto&gt;() 호출 시 즉시 OnNext + OnCompleted
    ///
    /// 지연 응답 (IsLoading 중간 상태 검증):
    ///   fake.ReturnsDeferred&lt;UserDto&gt;(out var trigger);
    ///   api.Request.Send(Unit.Default);
    ///   Assert.AreEqual(true, api.Response.IsLoading.Value);  // 아직 응답 전
    ///   trigger.OnNext(new HttpResponse&lt;UserDto&gt; { ... });
    ///   trigger.OnCompleted();
    ///   Assert.AreEqual(false, api.Response.IsLoading.Value); // 응답 후
    ///
    /// 에러 반환:
    ///   fake.Throws(HttpErrorKind.Network);
    /// </summary>
    public class FakeHttpClient : IHttpClient
    {
        private readonly Queue<QueuedEntry> queue = new();

        /// <summary>전송된 요청 이력. 테스트에서 URL, Method, Body 검증에 활용합니다.</summary>
        public List<HttpRequest> ReceivedRequests { get; } = new();

        // ── 응답 설정 메서드 ─────────────────────────────────────────────

        /// <summary>성공 응답을 큐에 추가합니다. Send()가 호출되면 즉시 동기적으로 반환합니다.</summary>
        public FakeHttpClient Returns<T>(T data, int statusCode = 200)
        {
            queue.Enqueue(new ImmediateSuccess<T>(data, statusCode));
            return this;
        }

        /// <summary>
        /// 지연 응답을 큐에 추가합니다.
        /// 반환된 Subject에 OnNext/OnCompleted를 호출해야 응답이 전달됩니다.
        /// IsLoading=true 중간 상태 검증에 사용합니다.
        /// </summary>
        public FakeHttpClient ReturnsDeferred<T>(out Subject<HttpResponse<T>> trigger)
        {
            var subject = new Subject<HttpResponse<T>>();
            trigger = subject;
            queue.Enqueue(new DeferredSuccess<T>(subject));
            return this;
        }

        /// <summary>에러 응답을 큐에 추가합니다.</summary>
        public FakeHttpClient Throws(HttpErrorKind kind, int statusCode = 0, string message = "")
        {
            queue.Enqueue(new ErrorEntry(new HttpError(statusCode, message, kind)));
            return this;
        }

        /// <summary>HttpError 인스턴스를 직접 큐에 추가합니다.</summary>
        public FakeHttpClient Throws(HttpError error)
        {
            queue.Enqueue(new ErrorEntry(error));
            return this;
        }

        // ── IHttpClient 구현 ─────────────────────────────────────────────

        public Observable<HttpResponse<T>> Send<T>(HttpRequest request, CancellationToken ct = default)
        {
            ReceivedRequests.Add(request);

            if (!queue.TryDequeue(out var entry))
            {
                return Observable.Throw<HttpResponse<T>>(
                    new HttpError(0, "FakeHttpClient: 큐에 남은 응답이 없습니다.", HttpErrorKind.Network));
            }

            return entry.ToObservable<T>();
        }

        public Observable<HttpResponse<T>> Get<T>(
            string url,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.GET, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Post<T>(
            string url, string jsonBody,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.POST, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Put<T>(
            string url, string jsonBody,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.PUT, Body = jsonBody, Headers = headers }, ct);

        public Observable<HttpResponse<T>> Delete<T>(
            string url,
            System.Collections.Generic.Dictionary<string, string> headers = null,
            CancellationToken ct = default) =>
            Send<T>(new HttpRequest { Url = url, Method = HttpMethod.DELETE, Headers = headers }, ct);

        // ── 내부 큐 엔트리 ───────────────────────────────────────────────

        private abstract class QueuedEntry
        {
            public abstract Observable<HttpResponse<T>> ToObservable<T>();
        }

        private class ImmediateSuccess<TData> : QueuedEntry
        {
            private readonly TData data;
            private readonly int statusCode;

            public ImmediateSuccess(TData data, int statusCode)
            {
                this.data = data;
                this.statusCode = statusCode;
            }

            public override Observable<HttpResponse<T>> ToObservable<T>()
            {
                if (data is T typed)
                {
                    return Observable.Return(new HttpResponse<T>
                    {
                        StatusCode = statusCode,
                        Data = typed,
                        RawJson = "",
                    });
                }

                return Observable.Throw<HttpResponse<T>>(new HttpError(
                    0,
                    $"FakeHttpClient 타입 불일치: 큐에 등록된 타입={typeof(TData).Name}, 요청된 타입={typeof(T).Name}",
                    HttpErrorKind.ParseFailed));
            }
        }

        private class DeferredSuccess<TData> : QueuedEntry
        {
            private readonly Subject<HttpResponse<TData>> subject;

            public DeferredSuccess(Subject<HttpResponse<TData>> subject)
            {
                this.subject = subject;
            }

            public override Observable<HttpResponse<T>> ToObservable<T>()
            {
                if (subject is Subject<HttpResponse<T>> typed)
                    return typed;

                return Observable.Throw<HttpResponse<T>>(new HttpError(
                    0,
                    $"FakeHttpClient 타입 불일치 (Deferred): 큐={typeof(TData).Name}, 요청={typeof(T).Name}",
                    HttpErrorKind.ParseFailed));
            }
        }

        private class ErrorEntry : QueuedEntry
        {
            private readonly HttpError error;
            public ErrorEntry(HttpError error) => this.error = error;

            public override Observable<HttpResponse<T>> ToObservable<T>() =>
                Observable.Throw<HttpResponse<T>>(error);
        }
    }
}
