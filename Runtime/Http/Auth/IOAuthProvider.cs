using R3;

namespace Sindy.Http
{
    /// <summary>
    /// 소셜 로그인 제공자 인터페이스.
    /// Google, Apple, Facebook 등 각 플랫폼별 구현을 제공합니다.
    /// </summary>
    public interface IOAuthProvider
    {
        string ProviderName { get; }
        Observable<TokenResponseDto> Login();
        Observable<Unit> Logout();
    }
}
