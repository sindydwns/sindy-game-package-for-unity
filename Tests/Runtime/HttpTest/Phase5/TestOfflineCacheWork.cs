using System;
using R3;
using Sindy.Http;
using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 5 — 오프라인 캐시 동작 검증
    ///
    /// 검증 항목:
    ///   Case1: 성공 응답 → 캐시 저장
    ///   Case2: 네트워크 에러 + 캐시 있음 → 캐시 반환, IsFromCache=true
    ///   Case3: 네트워크 에러 + 캐시 없음 → 에러 전파
    ///   Case4: 캐시 만료 → 에러 전파 (만료된 캐시 무시)
    ///   Case5: IsFromCache PropModel이 UI에 바인딩 가능한지
    /// </summary>
    class TestOfflineCacheWork : TestCase
    {
        private class RankingDto
        {
            public string Name  { get; set; }
            public int    Score { get; set; }
        }

        // ── 인메모리 캐시 스텁 ────────────────────────────────────────────
        // Phase 5 구현 전 테스트용 최소 캐시 로직.
        // 실제 OfflineCacheFeature는 PlayerPrefs에 저장합니다.

        private class StubCache<T>
        {
            private T        stored;
            private DateTime storedAt;
            private bool     hasValue;
            private readonly TimeSpan maxAge;

            public PropModel<bool>   IsFromCache { get; } = new(false);
            public PropModel<string> CachedAt    { get; } = new();

            public StubCache(TimeSpan maxAge)
            {
                this.maxAge = maxAge;
            }

            public Observable<HttpResponse<T>> Apply(Observable<HttpResponse<T>> source)
            {
                return source
                    .Do(res =>
                    {
                        stored   = res.Data;
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
                        CachedAt.Value    = $"캐시 ({storedAt:HH:mm} 기준)";
                        return Observable.Return(new HttpResponse<T>
                        {
                            StatusCode = 200,
                            Data       = stored,
                        });
                    });
            }
        }

        // ─────────────────────────────────────────────────────────────────

        public override void Run()
        {
            Case1_SuccessResponseCached();
            Case2_NetworkErrorWithCache();
            Case3_NetworkErrorNoCache();
            Case4_ExpiredCache();
            Case5_IsFromCachePropModel();
        }

        // ── Case 1: 성공 응답 → 캐시 저장 ────────────────────────────────

        private void Case1_SuccessResponseCached()
        {
            var fake  = new FakeHttpClient();
            var cache = new StubCache<RankingDto>(maxAge: TimeSpan.FromMinutes(5));

            fake.Returns(new RankingDto { Name = "Sindy", Score = 9999 });

            var raw = fake.Get<RankingDto>("/api/ranking");
            var withCache = cache.Apply(raw);

            RankingDto received = null;
            withCache.Subscribe(res => received = res.Data);

            Assert.IsNotNull(received);
            Assert.AreEqual("Sindy", received.Name);
            Assert.AreEqual(false, cache.IsFromCache.Value);  // 네트워크 응답
        }

        // ── Case 2: 네트워크 에러 + 캐시 → 캐시 반환 ────────────────────

        private void Case2_NetworkErrorWithCache()
        {
            var fake  = new FakeHttpClient();
            var cache = new StubCache<RankingDto>(maxAge: TimeSpan.FromMinutes(5));

            // 1번째: 성공 → 캐시 저장
            fake.Returns(new RankingDto { Name = "Cached", Score = 100 });
            cache.Apply(fake.Get<RankingDto>("/api/ranking")).Subscribe();
            Assert.AreEqual(false, cache.IsFromCache.Value);

            // 2번째: 네트워크 에러 → 캐시 반환
            fake.Throws(HttpErrorKind.Network);

            RankingDto fromCache = null;
            cache.Apply(fake.Get<RankingDto>("/api/ranking"))
                .Subscribe(res => fromCache = res.Data);

            Assert.IsNotNull(fromCache);
            Assert.AreEqual("Cached", fromCache.Name);
            Assert.AreEqual(true, cache.IsFromCache.Value);
            Assert.IsNotNull(cache.CachedAt.Value);
        }

        // ── Case 3: 네트워크 에러 + 캐시 없음 → 에러 전파 ───────────────

        private void Case3_NetworkErrorNoCache()
        {
            var fake  = new FakeHttpClient();
            var cache = new StubCache<RankingDto>(maxAge: TimeSpan.FromMinutes(5));

            fake.Throws(HttpErrorKind.Network);

            bool errored = false;
            HttpErrorKind? errorKind = null;

            cache.Apply(fake.Get<RankingDto>("/api/ranking"))
                .Subscribe(
                    _ => { },
                    result =>
                    {
                        if (result.IsFailure)
                        {
                            errored   = true;
                            errorKind = (result.Exception as HttpError)?.Kind;
                        }
                    }
                );

            Assert.AreEqual(true, errored);
            Assert.AreEqual(HttpErrorKind.Network, errorKind);
            Assert.AreEqual(false, cache.IsFromCache.Value);
        }

        // ── Case 4: 캐시 만료 → 에러 전파 ────────────────────────────────

        private void Case4_ExpiredCache()
        {
            var fake  = new FakeHttpClient();
            // maxAge=0으로 설정 → 저장 즉시 만료
            var cache = new StubCache<RankingDto>(maxAge: TimeSpan.FromSeconds(-1));

            // 저장 (즉시 만료)
            fake.Returns(new RankingDto { Name = "Expired", Score = 1 });
            cache.Apply(fake.Get<RankingDto>("/api/ranking")).Subscribe();

            // 캐시 만료 상태에서 네트워크 에러
            fake.Throws(HttpErrorKind.Network);

            bool errored = false;
            cache.Apply(fake.Get<RankingDto>("/api/ranking"))
                .Subscribe(
                    _ => { },
                    result => { if (result.IsFailure) errored = true; }
                );

            Assert.AreEqual(true,  errored);
            Assert.AreEqual(false, cache.IsFromCache.Value);  // 만료 캐시는 무시
        }

        // ── Case 5: IsFromCache PropModel → UI 바인딩 ────────────────────

        private void Case5_IsFromCachePropModel()
        {
            var cache = new StubCache<RankingDto>(maxAge: TimeSpan.FromMinutes(5));

            var isFromCacheHistory = new System.Collections.Generic.List<bool>();
            cache.IsFromCache.Subscribe(v => isFromCacheHistory.Add(v)).AddTo(disposables);

            // 초기 상태 false
            Assert.AreEqual(false, cache.IsFromCache.Value);
            Assert.AreEqual(1, isFromCacheHistory.Count);

            // 성공 응답 → false
            var fake1 = new FakeHttpClient();
            fake1.Returns(new RankingDto { Name = "Live", Score = 10 });
            cache.Apply(fake1.Get<RankingDto>("/api/ranking")).Subscribe();
            Assert.AreEqual(false, cache.IsFromCache.Value);

            // 에러 + 캐시 → true
            var fake2 = new FakeHttpClient();
            fake2.Throws(HttpErrorKind.Network);
            cache.Apply(fake2.Get<RankingDto>("/api/ranking")).Subscribe();
            Assert.AreEqual(true, cache.IsFromCache.Value);

            cache.IsFromCache.Dispose();
            cache.CachedAt.Dispose();
        }
    }
}
