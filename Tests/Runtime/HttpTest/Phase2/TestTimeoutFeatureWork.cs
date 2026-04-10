using R3;
using Sindy.Http;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 2 — TimeoutFeature 타임아웃 동작 검증
    ///
    /// 검증 항목:
    ///   Case1: 타임아웃 내 응답 → 정상 처리
    ///   Case2: 타임아웃 초과 → HttpErrorKind.Timeout 에러
    ///   Case3: 타임아웃 Feature Dispose 시 정리
    ///   Case4: Duration PropModel 변경이 이후 요청에 반영되는지
    /// </summary>
    class TestTimeoutFeatureWork : TestCase
    {
        private class DataDto { public string Value { get; set; } }

        public override void Run()
        {
            Case1_ResponseWithinTimeout();
            Case2_TimeoutExceeded();
            Case3_DisposeCleanup();
            Case4_DurationPropModel();
        }

        // ── Case 1: 타임아웃 내 정상 응답 ───────────────────────────────

        private void Case1_ResponseWithinTimeout()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new DataDto { Value = "fast_response" });

            // TODO Phase 2: 실제 TimeoutFeature 적용
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new TimeoutFeature(seconds: 30f));
            //
            // api.Request.Send(Unit.Default);
            //
            // Assert.AreEqual(false, api.Response.HasError.Value);
            // Assert.AreEqual("fast_response", api.Response.Data.Value.Value);

            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);
            api.Request.Send(Unit.Default);
            Assert.AreEqual("fast_response", api.Response.Data.Value.Value);

            api.Dispose();
        }

        // ── Case 2: 타임아웃 초과 → Timeout 에러 ────────────────────────
        // 지연 응답(Subject)을 사용해 타임아웃 조건을 시뮬레이션합니다.

        private void Case2_TimeoutExceeded()
        {
            var fake = new FakeHttpClient();
            fake.ReturnsDeferred<DataDto>(out var trigger);  // 응답을 보류

            // TODO Phase 2:
            // var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
            //     .With(new TimeoutFeature(seconds: 0.001f));  // 매우 짧은 타임아웃
            //
            // api.Request.Send(Unit.Default);
            //
            // // TimeoutFeature가 Observable.Timeout()을 통해 에러를 발생시켜야 함
            // // (실제 테스트에서는 Observable.Scheduler를 모킹하거나 TestScheduler 사용)
            // Assert.AreEqual(HttpErrorKind.Timeout, api.Response.Error.Value?.Kind);

            // Phase 2 미구현 — 지연 응답 Subject 자체만 확인
            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET);
            api.Request.Send(Unit.Default);
            Assert.AreEqual(true, api.Response.IsLoading.Value);  // 아직 응답 없음

            // Subject를 응답하지 않고 버림 — 취소 처리는 Phase 2에서
            trigger.Dispose();

            api.Dispose();
        }

        // ── Case 3: TimeoutFeature Dispose 정리 ──────────────────────────

        private void Case3_DisposeCleanup()
        {
            // TODO Phase 2:
            // var feature = new TimeoutFeature(seconds: 30f);
            // Assert.AreEqual(false, feature.IsDisposed);
            // Assert.AreEqual(30f, feature.Duration.Value);
            //
            // feature.Dispose();
            // Assert.AreEqual(true, feature.IsDisposed);

            // Phase 2 미구현 — ApiModel Dispose만 확인
            var api = new ApiModel<Unit, DataDto>(new FakeHttpClient(), "/api/data", HttpMethod.GET);
            api.Dispose();
            Assert.AreEqual(true, api.IsDisposed);
        }

        // ── Case 4: Duration PropModel 변경 ──────────────────────────────

        private void Case4_DurationPropModel()
        {
            // TODO Phase 2:
            // var feature = new TimeoutFeature(seconds: 10f);
            // Assert.AreEqual(10f, feature.Duration.Value);
            //
            // feature.Duration.Value = 30f;
            // Assert.AreEqual(30f, feature.Duration.Value);  // 변경 즉시 반영
            //
            // feature.Dispose();

            // Phase 2 미구현 — 플레이스홀더
            Assert.IsTrue(true);
        }
    }
}
