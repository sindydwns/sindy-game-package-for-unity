using R3;
using Sindy.Http;
using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 3 — 토큰 관리 및 자동 갱신 검증
    ///
    /// 검증 항목:
    ///   Case1: 유효한 토큰 → 헤더에 자동 주입
    ///   Case2: 만료된 토큰(401) → RefreshService 호출 → 갱신 토큰으로 재요청
    ///   Case3: 리프레시 실패 → 토큰 초기화 + Unauthorized 에러
    ///   Case4: TokenModel.IsExpired PropModel 상태 전환
    ///   Case5: ITokenStore 저장/불러오기 동작
    /// </summary>
    class TestTokenRefreshWork : TestCase
    {
        private class ProfileDto { public string Name { get; set; } }

        public override void Run()
        {
            Case1_ValidToken_HeaderInjected();
            Case2_ExpiredToken_RefreshAndRetry();
            Case3_RefreshFails_TokenCleared();
            Case4_TokenModelIsExpiredState();
            Case5_TokenStore_SaveAndLoad();
        }

        // 유효한 토큰이 Authorization 헤더에 자동 주입되는지 확인
        private void Case1_ValidToken_HeaderInjected()
        {
            var tokenModel = new TokenModel();
            tokenModel.Update("valid_access_token", "refresh_token");

            var fake = new FakeHttpClient();
            fake.Returns(new ProfileDto { Name = "Sindy" });

            var refreshService = new TokenRefreshService(fake, "/auth/refresh");
            var authClient = new AuthenticatedHttpClient(fake, tokenModel, refreshService);

            ProfileDto received = null;
            authClient.Get<ProfileDto>("/api/profile")
                .Subscribe(res => received = res.Data);

            Assert.IsNotNull(received);
            Assert.AreEqual("Sindy", received.Name);

            // Authorization 헤더가 주입됐는지 확인
            var sentRequest = fake.ReceivedRequests[0];
            Assert.IsTrue(sentRequest.Headers.ContainsKey("Authorization"));
            Assert.AreEqual("Bearer valid_access_token", sentRequest.Headers["Authorization"]);

            tokenModel.Dispose();
        }

        // 만료 토큰(401) 시 리프레시 후 갱신된 토큰으로 재요청되는지 확인
        private void Case2_ExpiredToken_RefreshAndRetry()
        {
            var tokenModel = new TokenModel();
            tokenModel.Update("expired_token", "valid_refresh");

            // 요청+갱신 모두 같은 FakeHttpClient 사용
            var fake = new FakeHttpClient();

            // 1번째: 401 Unauthorized
            fake.Throws(HttpErrorKind.Unauthorized, statusCode: 401);
            // 2번째: 리프레시 성공 응답
            fake.Returns(new TokenResponseDto
            {
                AccessToken  = "new_access_token",
                RefreshToken = "new_refresh_token",
                ExpiresIn    = 3600,
            });
            // 3번째: 재전송 성공
            fake.Returns(new ProfileDto { Name = "Sindy" });

            var refreshService = new TokenRefreshService(fake, "/auth/refresh");
            var authClient = new AuthenticatedHttpClient(fake, tokenModel, refreshService);

            ProfileDto received = null;
            authClient.Get<ProfileDto>("/api/profile")
                .Subscribe(res => received = res.Data);

            // 리프레시가 발생하고 TokenModel이 갱신됐는지 확인
            Assert.AreEqual("new_access_token", tokenModel.AccessToken.Value);
            Assert.AreEqual("new_refresh_token", tokenModel.RefreshToken.Value);
            Assert.IsNotNull(received);

            tokenModel.Dispose();
        }

        // 리프레시 실패 시 토큰이 초기화되고 Unauthorized 에러가 전파되는지 확인
        private void Case3_RefreshFails_TokenCleared()
        {
            var tokenModel = new TokenModel();
            tokenModel.Update("expired_token", "invalid_refresh");

            var fake = new FakeHttpClient();

            // 1번째: 401 Unauthorized
            fake.Throws(HttpErrorKind.Unauthorized, statusCode: 401);
            // 2번째: 리프레시도 실패
            fake.Throws(HttpErrorKind.Unauthorized, statusCode: 401);

            var refreshService = new TokenRefreshService(fake, "/auth/refresh");
            var authClient = new AuthenticatedHttpClient(fake, tokenModel, refreshService);

            bool errored = false;
            HttpErrorKind? errorKind = null;

            authClient.Get<ProfileDto>("/api/profile")
                .Subscribe(
                    _ => { },
                    result =>
                    {
                        if (result.IsFailure)
                        {
                            errored = true;
                            errorKind = (result.Exception as HttpError)?.Kind;
                        }
                    }
                );

            Assert.AreEqual(true, errored);
            Assert.AreEqual(HttpErrorKind.Unauthorized, errorKind);
            Assert.AreEqual(null, tokenModel.AccessToken.Value);   // 토큰 초기화
            Assert.AreEqual(false, tokenModel.HasToken);

            tokenModel.Dispose();
        }

        // TokenModel의 IsExpired 상태가 만료/갱신에 따라 올바르게 전환되는지 확인
        private void Case4_TokenModelIsExpiredState()
        {
            var tokenModel = new TokenModel();

            var isExpiredHistory = new System.Collections.Generic.List<bool>();
            tokenModel.IsExpired.Subscribe(v => isExpiredHistory.Add(v)).AddTo(disposables);

            // 초기: false
            Assert.AreEqual(false, tokenModel.IsExpired.Value);

            // 토큰 설정: false 유지
            tokenModel.Update("access", "refresh");
            Assert.AreEqual(false, tokenModel.IsExpired.Value);

            // 만료 처리: true
            tokenModel.ExpireNow();
            Assert.AreEqual(true, tokenModel.IsExpired.Value);

            // 갱신: false
            tokenModel.Update("new_access", "new_refresh");
            Assert.AreEqual(false, tokenModel.IsExpired.Value);

            // 구독 이력: false(초기) → true(만료) → false(갱신)
            // R3 ReactiveProperty는 같은 값 설정 시 emit하지 않으므로 Update()의 false→false는 생략됨
            Assert.AreEqual(3, isExpiredHistory.Count);

            tokenModel.Dispose();
        }

        // InMemoryTokenStore의 Save/Load/Clear가 정상 동작하는지 확인
        private void Case5_TokenStore_SaveAndLoad()
        {
            var store = new InMemoryTokenStore();

            // 비어있는 상태
            Assert.IsNull(store.Load());

            // 저장
            store.Save("access_123", "refresh_456");

            var loaded = store.Load();
            Assert.IsNotNull(loaded);
            Assert.AreEqual("access_123", loaded.AccessToken);
            Assert.AreEqual("refresh_456", loaded.RefreshToken);

            // 초기화
            store.Clear();
            Assert.IsNull(store.Load());
        }

        /// <summary>테스트용 인메모리 ITokenStore.</summary>
        private class InMemoryTokenStore : ITokenStore
        {
            private TokenData stored;

            public void Save(string accessToken, string refreshToken) =>
                stored = new TokenData { AccessToken = accessToken, RefreshToken = refreshToken };

            public TokenData Load() => stored;
            public void Clear() => stored = null;
        }
    }
}
