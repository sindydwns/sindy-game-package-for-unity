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
    ///   Case2: 만료된 토큰 → RefreshService 호출 → 갱신 토큰으로 재요청
    ///   Case3: 리프레시 실패 → 토큰 초기화 + Unauthorized 에러
    ///   Case4: TokenModel.IsExpired PropModel 상태 전환
    ///   Case5: ITokenStore 저장/불러오기 동작
    /// </summary>
    class TestTokenRefreshWork : TestCase
    {
        // ── 테스트용 스텁 ─────────────────────────────────────────────────

        /// <summary>Phase 3 TokenModel 스텁. 실제 구현 전 컴파일 가능한 최소 구현.</summary>
        private class StubTokenModel : ViewModel
        {
            public PropModel<string> AccessToken  { get; } = new();
            public PropModel<string> RefreshToken { get; } = new();
            public PropModel<bool>   IsExpired    { get; } = new(false);
            public bool HasToken => !string.IsNullOrEmpty(AccessToken.Value);

            public void Update(string access, string refresh)
            {
                AccessToken.Value  = access;
                RefreshToken.Value = refresh;
                IsExpired.Value    = false;
            }

            public void ExpireNow() => IsExpired.Value = true;
            public void Clear()
            {
                AccessToken.Value  = null;
                RefreshToken.Value = null;
                IsExpired.Value    = false;
            }

            public override void Dispose()
            {
                base.Dispose();
                AccessToken.Dispose();
                RefreshToken.Dispose();
                IsExpired.Dispose();
            }
        }

        private class TokenResponseDto
        {
            public string AccessToken  { get; set; }
            public string RefreshToken { get; set; }
            public int    ExpiresIn    { get; set; }
        }

        private class ProfileDto { public string Name { get; set; } }

        // ─────────────────────────────────────────────────────────────────

        public override void Run()
        {
            Case1_ValidToken_HeaderInjected();
            Case2_ExpiredToken_RefreshAndRetry();
            Case3_RefreshFails_TokenCleared();
            Case4_TokenModelIsExpiredState();
            Case5_TokenStore_SaveAndLoad();
        }

        // ── Case 1: 유효 토큰 → Authorization 헤더 자동 주입 ────────────

        private void Case1_ValidToken_HeaderInjected()
        {
            var tokenModel = new StubTokenModel();
            tokenModel.Update("valid_access_token", "refresh_token");

            var fake = new FakeHttpClient();
            fake.Returns(new ProfileDto { Name = "Sindy" });

            // TODO Phase 3: AuthenticatedHttpClient 사용
            // var authClient = new AuthenticatedHttpClient("https://api.test", tokenModel, refreshService);
            // var api = new ApiModel<Unit, ProfileDto>(authClient, "/api/profile", HttpMethod.GET);
            // api.Request.Send(Unit.Default);
            //
            // var sentRequest = fake.ReceivedRequests[0];
            // Assert.IsTrue(sentRequest.Headers.ContainsKey("Authorization"));
            // Assert.AreEqual("Bearer valid_access_token", sentRequest.Headers["Authorization"]);

            // Phase 3 미구현 — TokenModel 상태 검증
            Assert.AreEqual(true,  tokenModel.HasToken);
            Assert.AreEqual(false, tokenModel.IsExpired.Value);
            Assert.AreEqual("valid_access_token", tokenModel.AccessToken.Value);

            tokenModel.Dispose();
        }

        // ── Case 2: 만료 토큰 → 리프레시 → 원래 요청 재전송 ────────────

        private void Case2_ExpiredToken_RefreshAndRetry()
        {
            var tokenModel = new StubTokenModel();
            tokenModel.Update("expired_token", "valid_refresh");
            tokenModel.ExpireNow();

            var fakeForRefresh  = new FakeHttpClient();
            var fakeForProfile  = new FakeHttpClient();

            fakeForRefresh.Returns(new TokenResponseDto
            {
                AccessToken  = "new_access_token",
                RefreshToken = "new_refresh_token",
                ExpiresIn    = 3600,
            });
            fakeForProfile.Returns(new ProfileDto { Name = "Sindy" });

            // TODO Phase 3:
            // var refreshService = new TokenRefreshService(fakeForRefresh, "/auth/refresh");
            // var authClient = new AuthenticatedHttpClient("https://api.test", tokenModel, refreshService);
            // var api = new ApiModel<Unit, ProfileDto>(authClient, "/api/profile", HttpMethod.GET);
            //
            // api.Request.Send(Unit.Default);
            //
            // // 리프레시가 발생하고 TokenModel이 갱신됐는지 확인
            // Assert.AreEqual("new_access_token", tokenModel.AccessToken.Value);
            // Assert.AreEqual(false, tokenModel.IsExpired.Value);
            // Assert.AreEqual(false, api.Response.HasError.Value);

            // Phase 3 미구현 — 리프레시 로직 직접 시뮬레이션
            Assert.AreEqual(true, tokenModel.IsExpired.Value);

            // 토큰 갱신 시뮬레이션
            tokenModel.Update("new_access_token", "new_refresh_token");

            Assert.AreEqual("new_access_token", tokenModel.AccessToken.Value);
            Assert.AreEqual(false, tokenModel.IsExpired.Value);

            tokenModel.Dispose();
        }

        // ── Case 3: 리프레시 실패 → 토큰 초기화 + 에러 ──────────────────

        private void Case3_RefreshFails_TokenCleared()
        {
            var tokenModel = new StubTokenModel();
            tokenModel.Update("expired_token", "invalid_refresh");
            tokenModel.ExpireNow();

            // TODO Phase 3:
            // var fakeRefresh = new FakeHttpClient();
            // fakeRefresh.Throws(HttpErrorKind.Unauthorized, 401);
            //
            // var refreshService = new TokenRefreshService(fakeRefresh, "/auth/refresh");
            // var authClient = new AuthenticatedHttpClient("https://api.test", tokenModel, refreshService);
            // var api = new ApiModel<Unit, ProfileDto>(authClient, "/api/profile", HttpMethod.GET);
            //
            // api.Request.Send(Unit.Default);
            //
            // Assert.AreEqual(null, tokenModel.AccessToken.Value);   // 토큰 초기화
            // Assert.AreEqual(false, tokenModel.HasToken);
            // Assert.AreEqual(true, api.Response.HasError.Value);
            // Assert.AreEqual(HttpErrorKind.Unauthorized, api.Response.Error.Value.Kind);

            // Phase 3 미구현 — Clear 동작만 확인
            tokenModel.Clear();

            Assert.AreEqual(null, tokenModel.AccessToken.Value);
            Assert.AreEqual(false, tokenModel.HasToken);

            tokenModel.Dispose();
        }

        // ── Case 4: TokenModel.IsExpired PropModel 상태 전환 ────────────

        private void Case4_TokenModelIsExpiredState()
        {
            var tokenModel = new StubTokenModel();

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

            // 구독 이력: false(초기) → false(업데이트) → true(만료) → false(갱신)
            Assert.AreEqual(4, isExpiredHistory.Count);

            tokenModel.Dispose();
        }

        // ── Case 5: ITokenStore 저장/불러오기 ────────────────────────────

        private void Case5_TokenStore_SaveAndLoad()
        {
            // TODO Phase 3: PlayerPrefsTokenStore 사용
            // var store = new PlayerPrefsTokenStore();
            // store.Clear();
            //
            // var loaded = store.Load();
            // Assert.IsNull(loaded);  // 저장된 토큰 없음
            //
            // var tokenModel = new TokenModel();
            // tokenModel.Update("access", "refresh", expiresIn: 3600);
            // store.Save(tokenModel);
            //
            // var restored = store.Load();
            // Assert.IsNotNull(restored);
            // Assert.AreEqual("access", restored.AccessToken.Value);
            //
            // store.Clear();
            // Assert.IsNull(store.Load());

            // Phase 3 미구현 — StubTokenModel 저장/불러오기 시뮬레이션
            var token = new StubTokenModel();
            token.Update("access_123", "refresh_456");

            Assert.AreEqual("access_123", token.AccessToken.Value);
            Assert.AreEqual("refresh_456", token.RefreshToken.Value);

            token.Clear();
            Assert.AreEqual(null, token.AccessToken.Value);
            Assert.AreEqual(false, token.HasToken);

            token.Dispose();
        }
    }
}
