using System;
using System.Threading.Tasks;
using R3;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 요청 실패 시 자동 재시도 기능.
    /// Network / Timeout 에러에만 재시도하고, 4xx 에러(NotFound, Unauthorized 등)는 즉시 실패합니다.
    ///
    /// 사용 예:
    ///   var api = new ApiModel&lt;Unit, DataDto&gt;(client, "/api/data", HttpMethod.GET)
    ///       .With(new RetryFeature(maxRetry: 3, baseDelay: 1f));
    /// </summary>
    public class RetryFeature : ViewModelFeature
    {
        public PropModel<bool> IsRetrying { get; } = new(false);

        private readonly int maxRetry;
        private readonly float baseDelay;

        /// <param name="maxRetry">최대 재시도 횟수 (원본 요청 제외)</param>
        /// <param name="baseDelay">재시도 기본 지연 시간(초). 지수 백오프 적용: baseDelay * 2^attempt</param>
        public RetryFeature(int maxRetry = 3, float baseDelay = 1f)
        {
            this.maxRetry  = maxRetry;
            this.baseDelay = baseDelay;
            IsRetrying.AddTo(this);
        }

        /// <summary>
        /// Observable 팩토리를 재시도 가능하게 감쌉니다.
        /// 재시도 가능 에러(Network, Timeout) 발생 시 factory를 다시 호출하여 새 요청을 생성합니다.
        /// </summary>
        public Observable<T> Apply<T>(Func<Observable<T>> factory)
        {
            return Attempt<T>(factory, 0);
        }

        private Observable<T> Attempt<T>(Func<Observable<T>> factory, int attempt)
        {
            return factory()
                .Do(_ => IsRetrying.Value = false)
                .Catch<T, Exception>(err =>
                {
                    if (!IsRetryable(err) || attempt >= maxRetry)
                    {
                        IsRetrying.Value = false;
                        return Observable.Throw<T>(err);
                    }

                    IsRetrying.Value = true;

                    if (baseDelay > 0f)
                    {
                        var delayMs = (int)(baseDelay * Math.Pow(2, attempt) * 1000);
                        return Observable.Create<T>(async (observer, ct) =>
                        {
                            await Task.Delay(delayMs, ct);
                            var next = Attempt<T>(factory, attempt + 1);
                            next.Subscribe(
                                val => observer.OnNext(val),
                                result => observer.OnCompleted(result)
                            );
                        });
                    }

                    return Attempt<T>(factory, attempt + 1);
                });
        }

        private static bool IsRetryable(Exception err)
        {
            if (err is HttpError httpErr)
                return httpErr.Kind is HttpErrorKind.Network or HttpErrorKind.Timeout;

            return false;
        }

        public override void Dispose()
        {
            base.Dispose();
            IsRetrying.Dispose();
        }
    }
}
