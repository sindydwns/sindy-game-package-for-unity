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
        // ── 스텁 클래스들 ──────────────────────────────────────────────────

        private class StubTokenModel : ViewModel
        {
            public PropModel<string> AccessToken  { get; } = new();
            public PropModel<string> RefreshToken { get; } = new();
            public bool HasToken => !string.IsNullOrEmpty(AccessToken.Value);

            public void Update(string access, string refresh)
            {
                AccessToken.Value  = access;
                RefreshToken.Value = refresh;
            }

            public void Clear()
            {
                AccessToken.Value  = null;
                RefreshToken.Value = null;
            }

            public override void Dispose()
            {
                base.Dispose();
                AccessToken.Dispose();
                RefreshToken.Dispose();
            }
        }

        /// <summary>IOAuthProvider 스텁. 미리 설정된 결과를 Observable로 반환합니다.</summary>
        private class StubOAuthProvider
        {
            public string ProviderName { get; }
            private bool   shouldSucceed;
            private string accessToken;

            public StubOAuthProvider(string name, bool shouldSucceed, string accessToken = "stub_access")
            {
                ProviderName      = name;
                this.shouldSucceed = shouldSucceed;
                this.accessToken  = accessToken;
            }

            public Observable<StubTokenModel> Login()
            {
                if (!shouldSucceed)
                    return Observable.Throw<StubTokenModel>(
                        new HttpError(401, "Login failed", HttpErrorKind.Unauthorized));

                var token = new StubTokenModel();
                token.Update(accessToken, "stub_refresh");
                return Observable.Return(token);
            }

            public Observable<Unit> Logout() => Observable.Return(Unit.Default);
        }

        /// <summary>AuthService 스텁 (Phase 4 미구현 전 테스트용).</summary>
        private class StubAuthService : ViewModel
        {
            public PropModel<bool>   IsLoggedIn   { get; } = new(false);
            public PropModel<bool>   IsLoading    { get; } = new(false);
            public PropModel<string> ErrorMessage { get; } = new();
            public SubjModel<Unit>   OnLoginSuccess { get; } = new();

            private readonly StubTokenModel tokenModel;

            public StubAuthService(StubTokenModel tokenModel)
            {
                this.tokenModel = tokenModel;
            }

            public Observable<Unit> LoginWith(StubOAuthProvider provider)
            {
                IsLoading.Value    = true;
                ErrorMessage.Value = null;

                return provider.Login()
                    .Do(token =>
                    {
                        tokenModel.Update(token.AccessToken.Value, token.RefreshToken.Value);
                        IsLoggedIn.Value = true;
                        IsLoading.Value  = false;
                        OnLoginSuccess.OnNext(Unit.Default);
                    })
                    .Select(_ => Unit.Default)
                    .Catch<Unit, HttpError>(err =>
                    {
                        ErrorMessage.Value = err.Message;
                        IsLoading.Value    = false;
                        return Observable.Throw<Unit>(err);
                    });
            }

            public void Logout()
            {
                tokenModel.Clear();
                IsLoggedIn.Value = false;
            }

            public override void Dispose()
            {
                base.Dispose();
                IsLoggedIn.Dispose();
                IsLoading.Dispose();
                ErrorMessage.Dispose();
                OnLoginSuccess.Dispose();
            }
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

        // ── Case 1: 로그인 성공 → TokenModel 갱신 ────────────────────────

        private void Case1_ProviderLoginSuccess()
        {
            var tokenModel  = new StubTokenModel();
            var authService = new StubAuthService(tokenModel);
            var provider    = new StubOAuthProvider("google", shouldSucceed: true, "google_access_token");

            authService.LoginWith(provider).Subscribe();

            Assert.AreEqual(true,  authService.IsLoggedIn.Value);
            Assert.AreEqual(false, authService.IsLoading.Value);
            Assert.AreEqual("google_access_token", tokenModel.AccessToken.Value);

            authService.Dispose();
            tokenModel.Dispose();
        }

        // ── Case 2: 로그인 실패 → 에러 전파 ──────────────────────────────

        private void Case2_ProviderLoginFails()
        {
            var tokenModel  = new StubTokenModel();
            var authService = new StubAuthService(tokenModel);
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

        // ── Case 3: IsLoading 상태 전환 ──────────────────────────────────
        // 로그인 시작 → IsLoading=true → 완료 후 false

        private void Case3_IsLoadingTransition()
        {
            var tokenModel  = new StubTokenModel();
            var authService = new StubAuthService(tokenModel);
            var provider    = new StubOAuthProvider("apple", shouldSucceed: true);

            var loadingHistory = new System.Collections.Generic.List<bool>();
            authService.IsLoading.Subscribe(v => loadingHistory.Add(v)).AddTo(disposables);

            // 로그인 (StubOAuthProvider는 동기적으로 즉시 완료)
            authService.LoginWith(provider).Subscribe();

            // false(초기) → true(로그인중) → false(완료)
            Assert.IsTrue(loadingHistory.Count >= 2);
            Assert.AreEqual(false, authService.IsLoading.Value);  // 최종 상태

            authService.Dispose();
            tokenModel.Dispose();
        }

        // ── Case 4: 로그아웃 → 토큰 초기화 + IsLoggedIn=false ───────────

        private void Case4_Logout()
        {
            var tokenModel  = new StubTokenModel();
            var authService = new StubAuthService(tokenModel);
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

        // ── Case 5: 로그인 성공 → OnLoginSuccess 이벤트 발행 ────────────

        private void Case5_OnLoginSuccessEvent()
        {
            var tokenModel  = new StubTokenModel();
            var authService = new StubAuthService(tokenModel);
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
