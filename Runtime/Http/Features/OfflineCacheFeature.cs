using System;
using R3;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// 오프라인 캐시 기능.
    /// 성공 응답을 캐시에 저장하고, 네트워크 에러 시 유효한 캐시를 반환합니다.
    ///
    /// 사용 예:
    ///   var cache = new OfflineCacheFeature<RankingDto>(TimeSpan.FromMinutes(5));
    ///   cache.Apply(client.Get<RankingDto>("/api/ranking"))
    ///       .Subscribe(res => ...);
    /// </summary>
    public class OfflineCacheFeature<T> : ViewModelFeature
    {
        public PropModel<bool> IsFromCache { get; } = new(false);
        public PropModel<string> CachedAt { get; } = new();

        private T stored;
        private DateTime storedAt;
        private bool hasValue;
        private readonly TimeSpan maxAge;

        public OfflineCacheFeature(TimeSpan maxAge)
        {
            this.maxAge = maxAge;
            IsFromCache.AddTo(this);
            CachedAt.AddTo(this);
        }

        /// <summary>
        /// Observable 파이프라인에 캐시 로직을 적용합니다.
        /// 성공 → 캐시 저장, Network/Timeout 에러 + 유효 캐시 → 캐시 반환.
        /// </summary>
        public Observable<HttpResponse<T>> Apply(Observable<HttpResponse<T>> source)
        {
            return source
                .Do(res =>
                {
                    stored = res.Data;
                    storedAt = DateTime.UtcNow;
                    hasValue = true;
                    IsFromCache.Value = false;
                })
                .Catch<HttpResponse<T>, HttpError>(err =>
                {
                    if (err.Kind is not (HttpErrorKind.Network or HttpErrorKind.Timeout))
                        return Observable.Throw<HttpResponse<T>>(err);

                    if (!hasValue)
                        return Observable.Throw<HttpResponse<T>>(err);

                    if (DateTime.UtcNow - storedAt > maxAge)
                        return Observable.Throw<HttpResponse<T>>(err);

                    IsFromCache.Value = true;
                    CachedAt.Value = $"캐시 ({storedAt:HH:mm} 기준)";
                    return Observable.Return(new HttpResponse<T>
                    {
                        StatusCode = 200,
                        Data = stored,
                    });
                });
        }

        public override void Dispose()
        {
            base.Dispose();
            IsFromCache.Dispose();
            CachedAt.Dispose();
        }
    }
}
