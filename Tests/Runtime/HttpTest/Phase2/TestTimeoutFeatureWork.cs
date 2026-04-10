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

        // 타임아웃 내 정상 응답이 올바르게 처리되는지 확인
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

        // 지연 응답 중 IsLoading이 유지되고 응답 후 해제되는지 확인
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

        // TimeoutFeature Dispose 시 IsDisposed가 true로 설정되는지 확인
        private void Case3_DisposeCleanup()
        {
            var feature = new TimeoutFeature(seconds: 30f);
            Assert.AreEqual(false, feature.IsDisposed);
            Assert.AreEqual(30f, feature.Duration.Value);

            feature.Dispose();
            Assert.AreEqual(true, feature.IsDisposed);
        }

        // Duration PropModel 값을 변경할 수 있는지 확인
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
