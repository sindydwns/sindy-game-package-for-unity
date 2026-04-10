namespace Sindy.Http
{
    /// <summary>
    /// 토큰 영속 저장소 인터페이스.
    /// PlayerPrefs, SecureStorage 등 다양한 구현이 가능합니다.
    /// </summary>
    public interface ITokenStore
    {
        void Save(string accessToken, string refreshToken);
        TokenData Load();
        void Clear();
    }

    /// <summary>저장소에서 로드한 토큰 데이터.</summary>
    public class TokenData
    {
        public string AccessToken  { get; set; }
        public string RefreshToken { get; set; }
    }
}
