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
    ///   Case2: TimeoutFeature + 지연 응답 → IsLoading 유지 확인
    ///   Case3: TimeoutFeature Dispose 시 정리
    ///   Case4: Duration PropModel 변경이 반영되는지
    /// </summary>
    class TestTimeoutFeatureWork : TestCase
    {
        private class DataDto { public string Value { get; set; } }

        public override void Run()
        {
            Case1_ResponseWithinTimeout();
            Case2_TimeoutWithDeferredResponse();
            Case3_DisposeCleanup();
            Case4_DurationPropModel();
        }

        // ── Case 1: 타임아웃 내 정상 응답 ───────────────────────────────

        private void Case1_ResponseWithinTimeout()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new DataDto { Value = "fast_response" });

            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
                .With(new TimeoutFeature(seconds: 30f));

            api.Request.Send(Unit.Default);

            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.AreEqual("fast_response", api.Response.Data.Value.Value);

            api.Dispose();
        }

        // ── Case 2: 지연 응답 + TimeoutFeature → IsLoading 유지 ─────────
        // 동기 테스트에서 실제 타임아웃 발동은 검증 불가 (Observable.Timeout은 Scheduler 기반).
        // 여기서는 TimeoutFeature가 적용된 상태에서 지연 응답 시 IsLoading 유지를 확인합니다.

        private void Case2_TimeoutWithDeferredResponse()
        {
            var fake = new FakeHttpClient();
            fake.ReturnsDeferred<DataDto>(out var trigger);

            var api = new ApiModel<Unit, DataDto>(fake, "/api/data", HttpMethod.GET)
                .With(new TimeoutFeature(seconds: 30f));

            api.Request.Send(Unit.Default);
            Assert.AreEqual(true, api.Response.IsLoading.Value);  // 아직 응답 없음

            // 수동으로 응답 전달
            trigger.OnNext(new HttpResponse<DataDto>
            {
                StatusCode = 200,
                Data = new DataDto { Value = "delayed" },
            });
            trigger.OnCompleted();

            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual("delayed", api.Response.Data.Value.Value);

            api.Dispose();
        }

        // ── Case 3: TimeoutFeature Dispose 정리 ──────────────────────────

        private void Case3_DisposeCleanup()
        {
            var feature = new TimeoutFeature(seconds: 30f);
            Assert.AreEqual(false, feature.IsDisposed);
            Assert.AreEqual(30f, feature.Duration.Value);

            feature.Dispose();
            Assert.AreEqual(true, feature.IsDisposed);
        }

        // ── Case 4: Duration PropModel 변경 ──────────────────────────────

        private void Case4_DurationPropModel()
        {
            var feature = new TimeoutFeature(seconds: 10f);
            Assert.AreEqual(10f, feature.Duration.Value);

            feature.Duration.Value = 30f;
            Assert.AreEqual(30f, feature.Duration.Value);

            feature.Dispose();
        }
    }
}
