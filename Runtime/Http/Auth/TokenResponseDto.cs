namespace Sindy.Http
{
    /// <summary>토큰 갱신 API 응답 DTO.</summary>
    public class TokenResponseDto
    {
        public string AccessToken  { get; set; }
        public string RefreshToken { get; set; }
        public int    ExpiresIn    { get; set; }
    }
}
