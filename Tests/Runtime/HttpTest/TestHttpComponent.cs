using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// HTTP 모듈 테스트 실행 호스트.
    /// 기존 TestSindyComponent / TestViewModelComponent와 동일한 MonoBehaviour 패턴.
    ///
    /// 사용 방법:
    ///   1. 빈 씬에 이 컴포넌트를 가진 GameObject를 배치합니다.
    ///   2. Play 모드 진입 시 Start()에서 모든 테스트가 자동 실행됩니다.
    ///   3. Console에서 Assert 실패 여부를 확인합니다.
    ///   4. 녹색(오류 없음) = 통과, 빨간 Assert = 실패.
    /// </summary>
    public class TestHttpComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        private void Start()
        {
            // ── Phase 1 ──────────────────────────────────────────────────
            Add(new TestApiModelGetWork());
            Add(new TestApiModelPostWork());
            Add(new TestApiResponseStateWork());

            // ── Phase 2 ──────────────────────────────────────────────────
            Add(new TestRetryFeatureWork());
            Add(new TestTimeoutFeatureWork());

            // ── Phase 3 ──────────────────────────────────────────────────
            Add(new TestTokenRefreshWork());

            // ── Phase 4 ──────────────────────────────────────────────────
            Add(new TestOAuthFlowWork());

            // ── Phase 5 ──────────────────────────────────────────────────
            Add(new TestOfflineCacheWork());
            Add(new TestPaginationWork());

            // 모든 테스트 실행
            int passed  = 0;
            int failed  = 0;
            int total   = tests.Count;

            foreach (var test in tests)
            {
                try
                {
                    test.Run();
                    passed++;
                    Debug.Log($"[HTTP Test] PASS  {test.GetType().Name}");
                }
                catch (System.Exception ex)
                {
                    failed++;
                    Debug.LogError($"[HTTP Test] FAIL  {test.GetType().Name}\n{ex.Message}");
                }
            }

            var summary = $"[HTTP Test] 결과: {passed}/{total} 통과" +
                          (failed > 0 ? $", {failed}개 실패" : " — 전체 통과!");
            if (failed > 0)
                Debug.LogError(summary);
            else
                Debug.Log(summary);
        }

        private void OnDestroy()
        {
            foreach (var test in tests)
                test.Dispose();

            tests.Clear();
        }

        private void Add(TestCase test) => tests.Add(test);
    }
}
