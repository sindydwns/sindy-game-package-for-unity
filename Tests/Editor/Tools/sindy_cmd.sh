#!/bin/bash
# ───────────────────────────────────────────────────────────────────────────────
# sindy_cmd.sh — Unity 에디터 IPC 커맨드 실행 스크립트
#
# Unity 에디터가 열려 있는 상태에서 메서드를 원격으로 실행합니다.
# EditorCommandWatcher가 Temp/sindy_cmd.json을 폴링하여 메서드를 실행하고
# 결과를 Temp/sindy_result.json에 기록합니다.
#
# 사용법:
#   bash Tools/sindy_cmd.sh "Namespace.Class.Method"
#
# 예시:
#   bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.BatchTest.Ping"
#
# 전제 조건:
#   - Unity 에디터가 이 프로젝트를 열고 있어야 함
#   - EditorCommandWatcher가 활성화되어 있어야 함 (컴파일 완료 상태)
# ───────────────────────────────────────────────────────────────────────────────

METHOD="${1:-}"

if [ -z "$METHOD" ]; then
    echo "사용법: $0 \"Namespace.Class.Method\""
    echo "예시:   $0 \"Sindy.Editor.Examples.BatchTest.Ping\""
    exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_PATH="$(cd "$SCRIPT_DIR/.." && pwd)"
CMD_FILE="$PROJECT_PATH/Temp/sindy_cmd.json"
RESULT_FILE="$PROJECT_PATH/Temp/sindy_result.json"

# 고유 ID 생성 (macOS/Linux 호환)
ID="$(uuidgen 2>/dev/null | tr -d '-' | tr '[:upper:]' '[:lower:]' | head -c 8 2>/dev/null || date +%s$$)"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  Sindy IPC Commander"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  메서드 : $METHOD"
echo "  ID     : $ID"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Temp 디렉토리 확인 (Unity가 자동 생성하지만 없을 경우 대비)
mkdir -p "$PROJECT_PATH/Temp"

# 이전 결과 파일 삭제
rm -f "$RESULT_FILE"

# 커맨드 파일 작성
printf '{"method":"%s","id":"%s"}' "$METHOD" "$ID" > "$CMD_FILE"
echo "커맨드 파일 작성 완료: $CMD_FILE"
echo ""

# 결과 대기 (최대 30초)
TIMEOUT=30
echo "Unity 에디터 응답 대기 중... (최대 ${TIMEOUT}초)"

for i in $(seq 1 $TIMEOUT); do
    sleep 1

    if [ ! -f "$RESULT_FILE" ]; then
        printf "."
        continue
    fi

    echo ""
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "  결과"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    cat "$RESULT_FILE"
    echo ""

    # success 필드로 exit code 결정
    SUCCESS=$(python3 -c "
import json, sys
try:
    d = json.load(open('$RESULT_FILE'))
    sys.exit(0 if d.get('success') else 1)
except Exception as e:
    print('결과 파싱 오류:', e)
    sys.exit(1)
" 2>&1)
    EXIT_CODE=$?

    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    if [ "$EXIT_CODE" -eq 0 ]; then
        echo "  성공 (exit 0)"
    else
        echo "  실패 (exit 1)"
        [ -n "$SUCCESS" ] && echo "  $SUCCESS"
    fi
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    exit "$EXIT_CODE"
done

echo ""
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  TIMEOUT: Unity 에디터가 ${TIMEOUT}초 내에 응답하지 않았습니다."
echo ""
echo "  확인사항:"
echo "  - Unity 에디터가 이 프로젝트를 열고 있는지 확인"
echo "  - 에디터가 컴파일 중이거나 모달 다이얼로그가 열려있지 않은지 확인"
echo "  - Console에서 [SindyCmd] 로그가 보이는지 확인"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
exit 1
