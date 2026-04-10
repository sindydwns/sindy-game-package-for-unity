using R3;
using Sindy.Http;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 1 — ApiModel GET 요청 검증
    ///
    /// 검증 항목:
    ///   Case1: GET 요청이 올바른 URL과 Method로 전송되는지
    ///   Case2: 성공 응답이 Response.Data에 반영되는지
    ///   Case3: 에러 응답이 Response.Error / HasError에 반영되는지
    ///   Case4: 연속 요청 시 응답이 독립적으로 처리되는지
    ///   Case5: ViewModel.Dispose() 시 구독이 모두 정리되는지
    /// </summary>
    class TestApiModelGetWork : TestCase
    {
        private class UserDto
        {
            public string Name  { get; set; }
            public int    Level { get; set; }
        }

        public override void Run()
        {
            Case1_RequestUrl();
            Case2_SuccessResponseData();
            Case3_ErrorResponse();
            Case4_SequentialRequests();
            Case5_DisposeCleanup();
        }

        // ── Case 1: 요청 URL / Method 검증 ──────────────────────────────

        private void Case1_RequestUrl()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new UserDto { Name = "Sindy", Level = 10 });

            var api = new ApiModel<Unit, UserDto>(fake, "/api/user", HttpMethod.GET);
            api.Request.Send(Unit.Default);

            Assert.AreEqual(1, fake.ReceivedRequests.Count);
            Assert.AreEqual("/api/user", fake.ReceivedRequests[0].Url);
            Assert.AreEqual(HttpMethod.GET, fake.ReceivedRequests[0].Method);
            Assert.IsNull(fake.ReceivedRequests[0].Body);  // GET은 바디 없음

            api.Dispose();
        }

        // ── Case 2: 성공 응답 → Response.Data 반영 ──────────────────────

        private void Case2_SuccessResponseData()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new UserDto { Name = "Sindy", Level = 42 });

            var api = new ApiModel<Unit, UserDto>(fake, "/api/user", HttpMethod.GET);

            // 초기 상태
            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.IsNull(api.Response.Data.Value);

            api.Request.Send(Unit.Default);

            // 동기 FakeHttpClient이므로 즉시 완료
            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.IsNotNull(api.Response.Data.Value);
            Assert.AreEqual("Sindy", api.Response.Data.Value.Name);
            Assert.AreEqual(42, api.Response.Data.Value.Level);

            api.Dispose();
        }

        // ── Case 3: 에러 응답 → Response.Error / HasError 반영 ──────────

        private void Case3_ErrorResponse()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Network);

            var api = new ApiModel<Unit, UserDto>(fake, "/api/user", HttpMethod.GET);

            api.Request.Send(Unit.Default);

            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual(true, api.Response.HasError.Value);
            Assert.IsNotNull(api.Response.Error.Value);
            Assert.AreEqual(HttpErrorKind.Network, api.Response.Error.Value.Kind);
            Assert.IsNull(api.Response.Data.Value);  // 에러 시 Data는 null 유지

            api.Dispose();
        }

        // ── Case 4: 연속 요청 → 각 응답이 독립 처리 ─────────────────────

        private void Case4_SequentialRequests()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new UserDto { Name = "First",  Level = 1 });
            fake.Returns(new UserDto { Name = "Second", Level = 2 });

            var api = new ApiModel<Unit, UserDto>(fake, "/api/user", HttpMethod.GET);

            // 1번째 요청
            api.Request.Send(Unit.Default);
            Assert.AreEqual("First", api.Response.Data.Value.Name);
            Assert.AreEqual(1, api.Response.Data.Value.Level);

            // 2번째 요청 — 이전 응답이 덮어씌워짐
            api.Request.Send(Unit.Default);
            Assert.AreEqual("Second", api.Response.Data.Value.Name);
            Assert.AreEqual(2, api.Response.Data.Value.Level);

            Assert.AreEqual(2, fake.ReceivedRequests.Count);

            api.Dispose();
        }

        // ── Case 5: Dispose 후 구독이 정리되는지 ────────────────────────

        private void Case5_DisposeCleanup()
        {
            var fake = new FakeHttpClient();
            var api  = new ApiModel<Unit, UserDto>(fake, "/api/user", HttpMethod.GET);

            Assert.AreEqual(false, api.IsDisposed);
            api.Dispose();
            Assert.AreEqual(true, api.IsDisposed);

            // Dispose 후 자식 모델도 정리됨
            Assert.AreEqual(true, api.Request.IsDisposed);
            Assert.AreEqual(true, api.Response.IsDisposed);
        }
    }
}
