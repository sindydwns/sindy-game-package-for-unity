using R3;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// 인증 서비스.
    /// IOAuthProvider를 통한 소셜 로그인 흐름을 관리합니다.
    /// IsLoggedIn, IsLoading, ErrorMessage를 PropModel로 노출하여 UI 바인딩이 가능합니다.
    ///
    /// 사용 예:
    ///   var auth = new AuthService(tokenModel);
    ///   auth.LoginWith(googleProvider).Subscribe();
    ///   auth.IsLoggedIn.Subscribe(v => loginButton.SetActive(!v));
    /// </summary>
    public class AuthService : ViewModel
    {
        public PropModel<bool>   IsLoggedIn   { get; } = new(false);
        public PropModel<bool>   IsLoading    { get; } = new(false);
        public PropModel<string> ErrorMessage { get; } = new();
        public SubjModel<Unit>   OnLoginSuccess { get; } = new();

        private readonly TokenModel tokenModel;

        public AuthService(TokenModel tokenModel)
        {
            this.tokenModel = tokenModel;
        }

        /// <summary>
        /// 지정된 OAuth 제공자로 로그인을 시도합니다.
        /// 성공 시 TokenModel을 갱신하고 OnLoginSuccess 이벤트를 발행합니다.
        /// </summary>
        public Observable<Unit> LoginWith(IOAuthProvider provider)
        {
            IsLoading.Value    = true;
            ErrorMessage.Value = null;

            return provider.Login()
                .Do(tokenRes =>
                {
                    tokenModel.Update(tokenRes.AccessToken, tokenRes.RefreshToken, tokenRes.ExpiresIn);
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

        /// <summary>토큰을 초기화하고 로그아웃 상태로 전환합니다.</summary>
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
}
