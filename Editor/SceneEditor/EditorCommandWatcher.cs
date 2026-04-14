// ────────────────────────────────────────────────────────────────────────────
// EditorCommandWatcher — 파일 기반 IPC 시스템
//
// AI(쉘)가 Temp/sindy_cmd.json을 작성하면 Unity 에디터가 감지하여
// 리플렉션으로 메서드를 실행하고 결과를 Temp/sindy_result.json에 기록합니다.
//
// 커맨드 파일 형식:
//   { "method": "Sindy.Editor.Examples.BatchTest.Ping", "id": "abc123" }
//
// 결과 파일 형식:
//   { "id": "abc123", "success": true, "message": "OK", "timestamp": "..." }
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.SceneTools
{
    [InitializeOnLoad]
    public static class EditorCommandWatcher
    {
        private static readonly string CmdFile =
            Path.Combine(Application.dataPath, "..", "Temp", "sindy_cmd.json");
        private static readonly string ResultFile =
            Path.Combine(Application.dataPath, "..", "Temp", "sindy_result.json");

        private static double _lastPollTime;
        private const double PollIntervalSeconds = 0.1;

        // ── 직렬화 DTO ──────────────────────────────────────────────────────────

        [Serializable]
        private class CommandDto
        {
            public string method;
            public string id;
        }

        [Serializable]
        private class ResultDto
        {
            public string id;
            public bool   success;
            public string message;
            public string timestamp;
        }

        // ── 초기화 ──────────────────────────────────────────────────────────────

        static EditorCommandWatcher()
        {
            EditorApplication.update += Poll;
            Debug.Log("[SindyCmd] EditorCommandWatcher 시작됨. 커맨드 파일 폴링 중...");
        }

        // ── 폴링 ────────────────────────────────────────────────────────────────

        private static void Poll()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastPollTime < PollIntervalSeconds)
                return;
            _lastPollTime = now;

            try
            {
                if (!File.Exists(CmdFile))
                    return;

                // 파일 읽기 + 즉시 삭제 (중복 실행 방지)
                string json = File.ReadAllText(CmdFile);
                File.Delete(CmdFile);

                CommandDto cmd = JsonUtility.FromJson<CommandDto>(json);

                if (string.IsNullOrEmpty(cmd?.method))
                {
                    WriteResult(cmd?.id ?? "?", false, "method 필드가 없거나 비어있습니다.");
                    return;
                }

                Debug.Log($"[SindyCmd] 커맨드 수신: method={cmd.method}, id={cmd.id}");
                ExecuteMethod(cmd.id, cmd.method);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SindyCmd] 폴링 중 예외: {ex.Message}");
                // 에디터 크래시 방지 — 예외를 삼킴
            }
        }

        // ── 리플렉션으로 메서드 실행 ────────────────────────────────────────────

        private static void ExecuteMethod(string cmdId, string fullMethodName)
        {
            // "Namespace.TypeName.MethodName" → 마지막 . 기준으로 분리
            int lastDot = fullMethodName.LastIndexOf('.');
            if (lastDot < 0)
            {
                string msg = $"올바른 형식이 아닙니다 (Namespace.Type.Method 필요): {fullMethodName}";
                Debug.LogError($"[SindyCmd] {msg}");
                WriteResult(cmdId, false, msg);
                return;
            }

            string typeName   = fullMethodName.Substring(0, lastDot);
            string methodName = fullMethodName.Substring(lastDot + 1);

            // 모든 로드된 어셈블리에서 타입 탐색
            Type targetType = FindType(typeName);
            if (targetType == null)
            {
                string msg = $"타입을 찾을 수 없습니다: {typeName}";
                Debug.LogError($"[SindyCmd] {msg}");
                WriteResult(cmdId, false, msg);
                return;
            }

            MethodInfo method = targetType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
            {
                string msg = $"static 메서드를 찾을 수 없습니다: {typeName}.{methodName}";
                Debug.LogError($"[SindyCmd] {msg}");
                WriteResult(cmdId, false, msg);
                return;
            }

            try
            {
                Debug.Log($"[SindyCmd] 실행: {fullMethodName}");
                method.Invoke(null, null);
                Debug.Log($"[SindyCmd] 완료: {fullMethodName}");
                WriteResult(cmdId, true, $"OK — {fullMethodName}");
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                string msg = $"메서드 실행 예외: {inner.GetType().Name}: {inner.Message}";
                Debug.LogError($"[SindyCmd] {msg}\n{inner.StackTrace}");
                WriteResult(cmdId, false, msg);
            }
            catch (Exception ex)
            {
                string msg = $"예외: {ex.GetType().Name}: {ex.Message}";
                Debug.LogError($"[SindyCmd] {msg}\n{ex.StackTrace}");
                WriteResult(cmdId, false, msg);
            }
        }

        // ── 타입 탐색 ────────────────────────────────────────────────────────────

        private static Type FindType(string typeName)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(typeName);
                if (t != null)
                    return t;
            }
            return null;
        }

        // ── 결과 파일 기록 ───────────────────────────────────────────────────────

        private static void WriteResult(string id, bool success, string message)
        {
            try
            {
                var result = new ResultDto
                {
                    id        = id ?? string.Empty,
                    success   = success,
                    message   = message,
                    timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                };

                string json = JsonUtility.ToJson(result);

                string dir = Path.GetDirectoryName(ResultFile);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(ResultFile, json);
                Debug.Log($"[SindyCmd] 결과 기록: {json}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SindyCmd] 결과 파일 기록 실패: {ex.Message}");
            }
        }
    }
}
#endif
