using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// 인증 토큰 상태를 관리하는 ViewModel.
    /// AccessToken, RefreshToken, IsExpired를 PropModel로 노출하여 UI 바인딩이 가능합니다.
    /// </summary>
    public class TokenModel : ViewModel
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
}
