using NUnit.Framework;

namespace Sindy.Test
{
    [TestFixture]
    class HttpTests
    {
        [Test] public void ApiModelGet() { using var t = new TestApiModelGetWork(); t.Run(); }
        [Test] public void ApiModelPost() { using var t = new TestApiModelPostWork(); t.Run(); }
        [Test] public void ApiResponseState() { using var t = new TestApiResponseStateWork(); t.Run(); }
        [Test] public void RetryFeature() { using var t = new TestRetryFeatureWork(); t.Run(); }
        [Test] public void TimeoutFeature() { using var t = new TestTimeoutFeatureWork(); t.Run(); }
        [Test] public void TokenRefresh() { using var t = new TestTokenRefreshWork(); t.Run(); }
        [Test] public void OAuthFlow() { using var t = new TestOAuthFlowWork(); t.Run(); }
        [Test] public void OfflineCache() { using var t = new TestOfflineCacheWork(); t.Run(); }
        [Test] public void Pagination() { using var t = new TestPaginationWork(); t.Run(); }
    }
}
