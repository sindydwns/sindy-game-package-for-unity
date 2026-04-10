using R3;
using Sindy.Http;
using Sindy.View;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 4 — 소셜 로그인 OAuth2 흐름 검증
    ///
    /// 검증 항목:
    ///   Case1: Provider.Login() 성공 → TokenModel 갱신
    ///   Case2: Provider.Login() 실패 → 에러 전파
    ///   Case3: AuthService.LoginWith() → IsLoading 상태 전환
    ///   Case4: AuthService.Logout() → 토큰 초기화 + IsLoggedIn=false
    ///   Case5: 로그인 성공 → OnLoginSuccess 이벤트 발행
    /// </summary>
    class TestOAuthFlowWork : TestCase
    {
        // ── 스텁 OAuth 제공자 ─────────────────────────────────────────────

        private class StubOAuthProvider : IOAuthProvider
        {
            public string ProviderName { get; }
            private readonly bool shouldSucceed;
            private readonly string accessToken;

            public StubOAuthProvider(string name, bool shouldSucceed, string accessToken = "stub_access")
            {
                ProviderName       = name;
                this.shouldSucceed = shouldSucceed;
                this.accessToken   = accessToken;
            }

            public Observable<TokenResponseDto> Login()
            {
                if (!shouldSucceed)
                    return Observable.Throw<TokenResponseDto>(
                        new HttpError(401, "Login failed", HttpErrorKind.Unauthorized));

                return Observable.Return(new TokenResponseDto
                {
                    AccessToken  = accessToken,
                    RefreshToken = "stub_refresh",
                    ExpiresIn    = 3600,
                });
            }

            public Observable<Unit> Logout() => Observable.Return(Unit.Default);
        }

        // ─────────────────────────────────────────────────────────────────

        public override void Run()
        {
            Case1_ProviderLoginSuccess();
            Case2_ProviderLoginFails();
            Case3_IsLoadingTransition();
            Case4_Logout();
            Case5_OnLoginSuccessEvent();
        }

        // OAuth 로그인 성공 시 TokenModel이 갱신되고 IsLoggedIn=true인지 확인
        private void Case1_ProviderLoginSuccess()
        {
            var tokenModel  = new TokenModel();
            var authService = new AuthService(tokenModel);
            var provider    = new StubOAuthProvider("google", shouldSucceed: true, "google_access_token");

            authService.LoginWith(provider).Subscribe();

            Assert.AreEqual(true,  authService.IsLoggedIn.Value);
            Assert.AreEqual(false, authService.IsLoading.Value);
            Assert.AreEqual("google_access_token", tokenModel.AccessToken.Value);

            authService.Dispose();
            tokenModel.Dispose();
        }

        // OAuth 로그인 실패 시 에러가 전파되고 IsLoggedIn=false인지 확인
        private void Case2_ProviderLoginFails()
        {
            var tokenModel  = new TokenModel();
            var authService = new AuthService(tokenModel);
            var provider    = new StubOAuthProvider("google", shouldSucceed: false);

            bool errored = false;
            authService.LoginWith(provider)
                .Subscribe(
                    _ => { },
                    result => { if (result.IsFailure) errored = true; }
                );

            Assert.AreEqual(true,  errored);
            Assert.AreEqual(false, authService.IsLoggedIn.Value);
            Assert.AreEqual(false, authService.IsLoading.Value);
            Assert.IsNotNull(authService.ErrorMessage.Value);
            Assert.AreEqual(false, tokenModel.HasToken);

            authService.Dispose();
            tokenModel.Dispose();
        }

        // 로그인 중 IsLoading이 true→false로 전환되는지 확인
        private void Case3_IsLoadingTransition()
        {
            var tokenModel  = new TokenModel();
            var authService = new AuthService(tokenModel);
            var provider    = new StubOAuthProvider("apple", shouldSucceed: true);

            var loadingHistory = new System.Collections.Generic.List<bool>();
            authService.IsLoading.Subscribe(v => loadingHistory.Add(v)).AddTo(disposables);

            authService.LoginWith(provider).Subscribe();

            // false(초기) → true(로그인중) → false(완료)
            Assert.IsTrue(loadingHistory.Count >= 2);
            Assert.AreEqual(false, authService.IsLoading.Value);

            authService.Dispose();
            tokenModel.Dispose();
        }

        // 로그아웃 시 토큰이 초기화되고 IsLoggedIn=false인지 확인
        private void Case4_Logout()
        {
            var tokenModel  = new TokenModel();
            var authService = new AuthService(tokenModel);
            var provider    = new StubOAuthProvider("google", shouldSucceed: true);

            authService.LoginWith(provider).Subscribe();
            Assert.AreEqual(true, authService.IsLoggedIn.Value);
            Assert.AreEqual(true, tokenModel.HasToken);

            authService.Logout();

            Assert.AreEqual(false, authService.IsLoggedIn.Value);
            Assert.AreEqual(false, tokenModel.HasToken);
            Assert.IsNull(tokenModel.AccessToken.Value);

            authService.Dispose();
            tokenModel.Dispose();
        }

        // 로그인 성공 시 OnLoginSuccess 이벤트가 발행되는지 확인
        private void Case5_OnLoginSuccessEvent()
        {
            var tokenModel  = new TokenModel();
            var authService = new AuthService(tokenModel);
            var provider    = new StubOAuthProvider("apple", shouldSucceed: true);

            bool successFired = false;
            authService.OnLoginSuccess.Obs.Subscribe(_ => successFired = true).AddTo(disposables);

            authService.LoginWith(provider).Subscribe();

            Assert.AreEqual(true, successFired);

            authService.Dispose();
            tokenModel.Dispose();
        }
    }
}
