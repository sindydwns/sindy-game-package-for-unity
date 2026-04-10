using Newtonsoft.Json;
using R3;
using Sindy.Http;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 1 — ApiModel POST 요청 검증
    ///
    /// 검증 항목:
    ///   Case1: POST 요청 바디가 JSON으로 직렬화되어 전송되는지
    ///   Case2: 성공 응답이 Response.Data에 반영되는지
    ///   Case3: 서버 에러(5xx) 응답 처리
    ///   Case4: 401 Unauthorized 에러 종류 구분
    ///   Case5: 에러 후 재요청 시 스트림이 살아있는지 (Subject 재사용)
    /// </summary>
    class TestApiModelPostWork : TestCase
    {
        private class LoginRequest
        {
            public string Email    { get; set; }
            public string Password { get; set; }
        }

        private class LoginResponse
        {
            public bool   Success  { get; set; }
            public string UserId   { get; set; }
        }

        public override void Run()
        {
            Case1_RequestBodySerialized();
            Case2_SuccessResponse();
            Case3_ServerError();
            Case4_UnauthorizedError();
            Case5_StreamAliveAfterError();
        }

        // POST 요청 바디가 JSON으로 직렬화되어 전송되는지 확인
        private void Case1_RequestBodySerialized()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new LoginResponse { Success = true, UserId = "u_001" });

            var api = new ApiModel<LoginRequest, LoginResponse>(fake, "/api/login");

            api.Request.Send(new LoginRequest { Email = "test@sindy.com", Password = "pw123" });

            Assert.AreEqual(1, fake.ReceivedRequests.Count);
            Assert.AreEqual(HttpMethod.POST, fake.ReceivedRequests[0].Method);
            Assert.IsNotNull(fake.ReceivedRequests[0].Body);

            // 전송된 JSON 바디를 역직렬화해 값 확인
            var sentBody = JsonConvert.DeserializeObject<LoginRequest>(fake.ReceivedRequests[0].Body);
            Assert.AreEqual("test@sindy.com", sentBody.Email);
            Assert.AreEqual("pw123", sentBody.Password);

            api.Dispose();
        }

        // POST 성공 응답이 Response.Data에 반영되는지 확인
        private void Case2_SuccessResponse()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new LoginResponse { Success = true, UserId = "u_999" });

            var api = new ApiModel<LoginRequest, LoginResponse>(fake, "/api/login");

            api.Request.Send(new LoginRequest { Email = "a@b.com", Password = "pw" });

            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.IsNotNull(api.Response.Data.Value);
            Assert.AreEqual(true, api.Response.Data.Value.Success);
            Assert.AreEqual("u_999", api.Response.Data.Value.UserId);

            api.Dispose();
        }

        // 5xx 서버 에러가 Response.Error에 반영되는지 확인
        private void Case3_ServerError()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.ServerError, statusCode: 503);

            var api = new ApiModel<LoginRequest, LoginResponse>(fake, "/api/login");

            api.Request.Send(new LoginRequest { Email = "a@b.com", Password = "pw" });

            Assert.AreEqual(true,  api.Response.HasError.Value);
            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual(HttpErrorKind.ServerError, api.Response.Error.Value.Kind);
            Assert.AreEqual(503, api.Response.Error.Value.StatusCode);

            api.Dispose();
        }

        // 401 에러가 Unauthorized 종류로 구분되는지 확인
        private void Case4_UnauthorizedError()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Unauthorized, statusCode: 401);

            var api = new ApiModel<LoginRequest, LoginResponse>(fake, "/api/login");

            api.Request.Send(new LoginRequest { Email = "a@b.com", Password = "wrong" });

            Assert.AreEqual(true,  api.Response.HasError.Value);
            Assert.AreEqual(HttpErrorKind.Unauthorized, api.Response.Error.Value.Kind);
            Assert.AreEqual(401, api.Response.Error.Value.StatusCode);

            api.Dispose();
        }

        // 에러 후 재요청 시 Subject 스트림이 살아있어 정상 처리되는지 확인
        private void Case5_StreamAliveAfterError()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Network);                              // 1번째: 에러
            fake.Returns(new LoginResponse { Success = true, UserId = "u_ok" }); // 2번째: 성공

            var api = new ApiModel<LoginRequest, LoginResponse>(fake, "/api/login");

            // 1번째 요청 — 에러
            api.Request.Send(new LoginRequest { Email = "a@b.com", Password = "pw" });
            Assert.AreEqual(true, api.Response.HasError.Value);

            // 2번째 요청 — 성공 (스트림이 살아있어야 함)
            api.Request.Send(new LoginRequest { Email = "a@b.com", Password = "pw2" });
            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.AreEqual("u_ok", api.Response.Data.Value.UserId);

            Assert.AreEqual(2, fake.ReceivedRequests.Count);

            api.Dispose();
        }
    }
}
