using System;
using System.Threading;
using System.Threading.Tasks;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// 인증 토큰 상태를 관리하는 ViewModel.
    /// AccessToken, RefreshToken, IsExpired를 PropModel로 노출하여 UI 바인딩이 가능합니다.
    /// ExpiresIn(초)을 전달하면 자동으로 만료 시점에 IsExpired가 true로 전환됩니다.
    /// </summary>
    public class TokenModel : ViewModel
    {
        public PropModel<string> AccessToken  { get; } = new();
        public PropModel<string> RefreshToken { get; } = new();
        public PropModel<bool>   IsExpired    { get; } = new(false);

        private CancellationTokenSource _expiryCts;

        public bool HasToken => !string.IsNullOrEmpty(AccessToken.Value);

        public void Update(string access, string refresh, int expiresInSeconds = 0)
        {
            CancelExpiryTimer();
            AccessToken.Value  = access;
            RefreshToken.Value = refresh;
            IsExpired.Value    = false;

            if (expiresInSeconds > 0)
            {
                StartExpiryTimer(expiresInSeconds);
            }
        }

        public void ExpireNow() => IsExpired.Value = true;

        public void Clear()
        {
            CancelExpiryTimer();
            AccessToken.Value  = null;
            RefreshToken.Value = null;
            IsExpired.Value    = false;
        }

        private void StartExpiryTimer(int seconds)
        {
            _expiryCts = new CancellationTokenSource();
            var ct = _expiryCts.Token;
            _ = ExpireAfterAsync(seconds, ct);
        }

        private async Task ExpireAfterAsync(int seconds, CancellationToken ct)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(seconds), ct);
                if (!ct.IsCancellationRequested)
                {
                    IsExpired.Value = true;
                }
            }
            catch (OperationCanceledException) { }
        }

        private void CancelExpiryTimer()
        {
            _expiryCts?.Cancel();
            _expiryCts?.Dispose();
            _expiryCts = null;
        }

        public override void Dispose()
        {
            CancelExpiryTimer();
            base.Dispose();
            AccessToken.Dispose();
            RefreshToken.Dispose();
            IsExpired.Dispose();
        }
    }
}
