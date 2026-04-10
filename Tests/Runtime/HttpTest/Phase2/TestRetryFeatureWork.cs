using System.Collections.Generic;
using R3;
using Sindy.Http;
using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 2 — RetryFeature 재시도 동작 검증
    ///
    /// RetryFeature는 Phase 2에서 구현됩니다.
    /// 이 테스트는 RetryFeature의 기대 동작을 명세(Spec)로 작성했습니다.
    /// Phase 2 구현 후 이 테스트를 실제 RetryFeature로 교체하세요.
    ///
    /// 검증 항목:
    ///   Case1: 성공 응답에서는 재시도 없음
    ///   Case2: 재시도 가능 에러(Network)에서 maxRetry 횟수만큼 재시도
    ///   Case3: 재시도 불가 에러(404)에서는 즉시 에러 반환
    ///   Case4: maxRetry 초과 후 최종 에러 반환
    ///   Case5: 재시도 중 IsRetrying=true, 완료 후 false
    /// </summary>
    class TestRetryFeatureWork : TestCase
    {
        private class DataDto { public string Value { get; set; } }

        public override void Run()
        {
            Case1_NoRetryOnSuccess();
            Case2_RetryOnNetworkError();
            Case3_NoRetryOnNonRetryableError();
            Case4_ExceedMaxRetry();
            Case5_IsRetryingState();
        }

        // ── Case 1: 성공 시 재시도 없음 ─────────────────────────────────

        private void Case1_NoRetryOnSuccess()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new DataDto { Value = "ok" });

            // TODO Phase 2: RetryFeature 실제 적용
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new RetryFeature(maxRetry: 3));
            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);

            api.Request.Send(Unit.Default);

            Assert.AreEqual(1, fake.ReceivedRequests.Count);  // 1번만 전송
            Assert.AreEqual("ok", api.Response.Data.Value.Value);

            api.Dispose();
        }

        // ── Case 2: 재시도 가능 에러 → maxRetry 횟수 재시도 ────────────
        // Network 에러 2번 → 성공 1번 (maxRetry=3이면 최종 성공)

        private void Case2_RetryOnNetworkError()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Network);           // 1번째: 실패
            fake.Throws(HttpErrorKind.Network);           // 2번째: 실패 (재시도)
            fake.Returns(new DataDto { Value = "retried" }); // 3번째: 성공 (재시도)

            // TODO Phase 2: 실제 RetryFeature로 교체
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new RetryFeature(maxRetry: 3, baseDelay: 0f));  // baseDelay=0: 테스트에서 즉시 재시도
            //
            // api.Request.Send(Unit.Default);
            //
            // Assert.AreEqual(3, fake.ReceivedRequests.Count);  // 총 3번 전송
            // Assert.AreEqual(false, api.Response.HasError.Value);
            // Assert.AreEqual("retried", api.Response.Data.Value.Value);

            // Phase 2 미구현 — 동작 명세만 문서화
            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);
            api.Request.Send(Unit.Default);
            // RetryFeature 없이는 1번만 시도 후 에러
            Assert.AreEqual(1, fake.ReceivedRequests.Count);
            Assert.AreEqual(true, api.Response.HasError.Value);

            api.Dispose();
        }

        // ── Case 3: 재시도 불가 에러(404) → 즉시 에러 ──────────────────

        private void Case3_NoRetryOnNonRetryableError()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.NotFound, statusCode: 404);
            fake.Returns(new DataDto { Value = "this should not be reached" });

            // TODO Phase 2:
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new RetryFeature(maxRetry: 3));
            //
            // api.Request.Send(Unit.Default);
            //
            // Assert.AreEqual(1, fake.ReceivedRequests.Count);  // 404는 재시도 안 함
            // Assert.AreEqual(HttpErrorKind.NotFound, api.Response.Error.Value.Kind);

            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);
            api.Request.Send(Unit.Default);
            Assert.AreEqual(HttpErrorKind.NotFound, api.Response.Error.Value.Kind);
            Assert.AreEqual(1, fake.ReceivedRequests.Count);

            api.Dispose();
        }

        // ── Case 4: maxRetry 초과 → 최종 에러 반환 ──────────────────────

        private void Case4_ExceedMaxRetry()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Network);
            fake.Throws(HttpErrorKind.Network);
            fake.Throws(HttpErrorKind.Network);
            fake.Throws(HttpErrorKind.Network);  // maxRetry=3 초과

            // TODO Phase 2:
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new RetryFeature(maxRetry: 3, baseDelay: 0f));
            //
            // api.Request.Send(Unit.Default);
            //
            // Assert.AreEqual(4, fake.ReceivedRequests.Count);  // 1 원본 + 3 재시도
            // Assert.AreEqual(true, api.Response.HasError.Value);
            // Assert.AreEqual(HttpErrorKind.Network, api.Response.Error.Value.Kind);

            // Phase 2 미구현 — 단순 에러 반환 확인
            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);
            api.Request.Send(Unit.Default);
            Assert.AreEqual(true, api.Response.HasError.Value);

            api.Dispose();
        }

        // ── Case 5: 재시도 중 IsRetrying PropModel 상태 ──────────────────

        private void Case5_IsRetryingState()
        {
            // TODO Phase 2:
            // var fake = new FakeHttpClient();
            // fake.Throws(HttpErrorKind.Network);
            // fake.Returns(new DataDto { Value = "after_retry" });
            //
            // var retryFeature = new RetryFeature(maxRetry: 1, baseDelay: 0f);
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(retryFeature);
            //
            // var isRetryingHistory = new List<bool>();
            // retryFeature.IsRetrying.Subscribe(v => isRetryingHistory.Add(v)).AddTo(disposables);
            //
            // api.Request.Send(Unit.Default);
            //
            // // false → true(재시도중) → false(완료) 순서로 변경됨을 확인
            // Assert.IsTrue(isRetryingHistory.Contains(true));
            // Assert.AreEqual(false, retryFeature.IsRetrying.Value);  // 최종 상태 false

            // Phase 2 미구현 — 구조만 확인
            var api = new ApiModel<Unit, DataDto>(new FakeHttpClient(), "/api/data", HttpMethod.GET);
            Assert.AreEqual(false, api.IsDisposed);
            api.Dispose();
        }
    }
}
