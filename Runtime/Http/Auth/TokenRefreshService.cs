using R3;

namespace Sindy.Http
{
    /// <summary>
    /// 토큰 갱신 서비스.
    /// RefreshToken으로 새로운 AccessToken/RefreshToken 쌍을 발급받습니다.
    /// </summary>
    public class TokenRefreshService
    {
        private readonly IHttpClient client;
        private readonly string refreshUrl;

        public TokenRefreshService(IHttpClient client, string refreshUrl)
        {
            this.client     = client;
            this.refreshUrl = refreshUrl;
        }

        /// <summary>
        /// RefreshToken을 사용하여 새 토큰을 발급받습니다.
        /// </summary>
        public Observable<TokenResponseDto> Refresh(string refreshToken)
        {
            var body = $"{{\"refreshToken\":\"{refreshToken}\"}}";
            return client.Post<TokenResponseDto>(refreshUrl, body)
                .Select(res => res.Data);
        }
    }
}
