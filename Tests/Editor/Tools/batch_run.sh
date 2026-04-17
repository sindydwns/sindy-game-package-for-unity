#!/bin/bash
# ───────────────────────────────────────────────────────────────────────────────
# batch_run.sh — Unity 배치 모드 실행 스크립트
#
# 사용법:
#   ./Tools/batch_run.sh "ClassName.MethodName" [timeout_seconds]
#
# 예시:
#   ./Tools/batch_run.sh "BatchTest.Ping"
#   ./Tools/batch_run.sh "SetupShowcaseTask.Run" 180
#
# AI 사용 패턴:
#   1. C# 스크립트 작성 (BatchEntryPoint 상속 or 단독 static 메서드)
#   2. 이 스크립트로 배치 실행
#   3. 로그 확인 → 성공/실패 판단
# ───────────────────────────────────────────────────────────────────────────────
set -euo pipefail

METHOD="${1:-}"
TIMEOUT="${2:-120}"

if [ -z "$METHOD" ]; then
    echo "❌ 사용법: $0 \"ClassName.MethodName\" [timeout_seconds]"
    echo "   예시:   $0 \"BatchTest.Ping\""
    exit 1
fi

# ── 경로 설정 ──────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_PATH="$(cd "$SCRIPT_DIR/.." && pwd)"
LOGS_DIR="$PROJECT_PATH/Logs"
TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
METHOD_TAG="$(echo "$METHOD" | tr '.' '_')"
LOG_FILE="$LOGS_DIR/batch_${METHOD_TAG}_${TIMESTAMP}.log"
RESULT_FILE="$LOGS_DIR/batch_result.txt"

mkdir -p "$LOGS_DIR"

# ── Unity 경로 탐색 ────────────────────────────────────────────────────────────
UNITY_HUB_BASE="/Applications/Unity/Hub/Editor"

find_unity() {
    if [ ! -d "$UNITY_HUB_BASE" ]; then
        return 1
    fi
    # ls -r로 내림차순(최신 버전 우선) 탐색
    while IFS= read -r dir; do
        local candidate="$UNITY_HUB_BASE/$dir/Unity.app/Contents/MacOS/Unity"
        if [ -f "$candidate" ]; then
            echo "$candidate"
            return 0
        fi
    done < <(ls -r "$UNITY_HUB_BASE" 2>/dev/null)
    return 1
}

UNITY_PATH="$(find_unity)" || {
    echo "❌ Unity 실행 파일을 찾을 수 없습니다."
    echo "   탐색 경로: $UNITY_HUB_BASE"
    exit 1
}

# ── 헤더 출력 ─────────────────────────────────────────────────────────────────
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Unity Batch Runner"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  메서드  : $METHOD"
echo "  Unity   : $UNITY_PATH"
echo "  프로젝트: $PROJECT_PATH"
echo "  로그    : $LOG_FILE"
echo "  타임아웃: ${TIMEOUT}초"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# 이전 결과 파일 삭제
rm -f "$RESULT_FILE"

# ── 배치 실행 ─────────────────────────────────────────────────────────────────
set +e  # exit code를 직접 처리하므로 set -e 잠시 해제

"$UNITY_PATH" \
    -batchmode \
    -quit \
    -projectPath "$PROJECT_PATH" \
    -executeMethod "$METHOD" \
    -logFile "$LOG_FILE" &

UNITY_PID=$!

# 타임아웃 감시 서브쉘
(
    sleep "$TIMEOUT"
    if kill -0 "$UNITY_PID" 2>/dev/null; then
        echo ""
        echo "⚠️  타임아웃 (${TIMEOUT}초) — Unity 프로세스를 종료합니다."
        kill "$UNITY_PID" 2>/dev/null
    fi
) &
TIMEOUT_PID=$!

# Unity 종료 대기
wait "$UNITY_PID"
EXIT_CODE=$?

# 타임아웃 감시 정리
kill "$TIMEOUT_PID" 2>/dev/null
wait "$TIMEOUT_PID" 2>/dev/null || true

set -e

# ── 로그 요약 출력 ─────────────────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Unity 로그 (배치 관련 라인 필터)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

if [ -f "$LOG_FILE" ]; then
    grep -E "(BatchTask|BatchEntryPoint|BatchTest|\[OK\]|\[ERROR\]|\[LOG\]|\[SUCCESS\]|\[FAIL\]|\[WARN\]|Exception|Error)" \
        "$LOG_FILE" 2>/dev/null | tail -50 || true
    echo ""
    echo "  📄 전체 로그: $LOG_FILE"
else
    echo "  ⚠️  로그 파일을 찾을 수 없습니다: $LOG_FILE"
fi

# ── 결과 파일 출력 ─────────────────────────────────────────────────────────────
if [ -f "$RESULT_FILE" ]; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  배치 결과 요약"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    cat "$RESULT_FILE"
fi

# ── 최종 결과 ─────────────────────────────────────────────────────────────────
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ "$EXIT_CODE" -eq 0 ]; then
    echo "  ✅ 성공  (exit code: $EXIT_CODE)"
else
    echo "  ❌ 실패  (exit code: $EXIT_CODE)"
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

exit "$EXIT_CODE"
