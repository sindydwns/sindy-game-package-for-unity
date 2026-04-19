// ────────────────────────────────────────────────────────────────────────────
// EditorCommandWatcher — 파일 기반 IPC + HTTP 서버
//
// 1. 파일 기반 IPC: AI(쉘)가 Temp/sindy_cmd.json을 작성하면 Unity 에디터가
//    감지하여 리플렉션으로 메서드를 실행하고 결과를 Temp/sindy_result.json에 기록
//
// 2. HTTP 서버: POST /execute, GET /ping 엔드포인트
//    포트: EditorPrefs "Sindy.EditorTools.HttpPort" (기본값 6060)
//    포트 변경: Edit > Preferences > Sindy
//
// 커맨드 파일 형식:
//   { "method": "Sindy.Editor.Examples.BatchTest.Ping", "id": "abc123" }
//
// 결과 파일 형식:
//   { "id": "abc123", "success": true, "message": "OK", "timestamp": "..." }
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    [InitializeOnLoad]
    public static class EditorCommandWatcher
    {
        // ── 파일 IPC 경로 ────────────────────────────────────────────────────────
        private static readonly string CmdFile =
            Path.Combine(Application.dataPath, "..", "Temp", "sindy_cmd.json");
        private static readonly string ResultFile =
            Path.Combine(Application.dataPath, "..", "Temp", "sindy_result.json");

        private static double _lastPollTime;
        private const double PollIntervalSeconds = 0.1;

        // ── HTTP 서버 ─────────────────────────────────────────────────────────────
        private const int DefaultPort = 6060;
        private const string PortPrefKey = "Sindy.EditorTools.HttpPort";

        private static HttpListener _httpListener;
        private static Thread _listenerThread;
        private static readonly ConcurrentQueue<PendingRequest> _requestQueue
            = new ConcurrentQueue<PendingRequest>();

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

        [Serializable]
        private class CommandData
        {
            public string method;
        }

        private class PendingRequest
        {
            public string RequestType; // "ping" or "execute"
            public string Method;
            public HttpListenerContext Context;
        }

        // ── 초기화 ──────────────────────────────────────────────────────────────

        static EditorCommandWatcher()
        {
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload += StopHttpServer;
            StartHttpServer();
            Debug.Log("[SindyCmd] EditorCommandWatcher 시작됨. 커맨드 파일 폴링 중...");
        }

        // ── HTTP 서버 ─────────────────────────────────────────────────────────────

        private static void StartHttpServer()
        {
            int port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add($"http://localhost:{port}/");
            try
            {
                _httpListener.Start();
                Debug.Log($"[SindyCmd] HTTP 서버 시작됨 → http://localhost:{port}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SindyCmd] HTTP 서버 시작 실패 (포트 {port}): {e.Message}");
                return;
            }

            _listenerThread = new Thread(() =>
            {
                while (_httpListener.IsListening)
                {
                    try
                    {
                        var context = _httpListener.GetContext(); // 블로킹
                        string urlPath = context.Request.Url.AbsolutePath.TrimEnd('/');

                        if (context.Request.HttpMethod == "GET" && urlPath == "/ping")
                        {
                            _requestQueue.Enqueue(new PendingRequest
                            {
                                RequestType = "ping",
                                Context = context
                            });
                        }
                        else if (context.Request.HttpMethod == "POST" && urlPath == "/execute")
                        {
                            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                            var data = JsonUtility.FromJson<CommandData>(body);
                            _requestQueue.Enqueue(new PendingRequest
                            {
                                RequestType = "execute",
                                Method = data?.method ?? "",
                                Context = context
                            });
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            context.Response.Close();
                        }
                    }
                    catch (Exception) { /* 리스너 중지 시 발생하는 예외 무시 */ }
                }
            }) { IsBackground = true };
            _listenerThread.Start();
        }

        private static void StopHttpServer()
        {
            _httpListener?.Stop();
            _httpListener?.Close();
            _listenerThread?.Join(500);
        }

        // ── 폴링 ────────────────────────────────────────────────────────────────

        private static void Poll()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastPollTime < PollIntervalSeconds)
                return;
            _lastPollTime = now;

            // HTTP 큐 처리
            while (_requestQueue.TryDequeue(out var req))
            {
                ResultDto result;
                if (req.RequestType == "ping")
                {
                    result = new ResultDto
                    {
                        id        = "",
                        success   = true,
                        message   = "pong",
                        timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
                else
                {
                    result = ExecuteMethodCore(req.Method);
                }
                SendHttpResponse(req.Context, result);
            }

            // 파일 IPC 처리
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
                ResultDto result = ExecuteMethodCore(cmd.method);
                result.id = cmd.id ?? string.Empty;
                WriteResultDto(result);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SindyCmd] 폴링 중 예외: {ex.Message}");
                // 에디터 크래시 방지 — 예외를 삼킴
            }
        }

        // ── HTTP 응답 전송 ────────────────────────────────────────────────────────

        private static void SendHttpResponse(HttpListenerContext context, ResultDto result)
        {
            try
            {
                var json  = JsonUtility.ToJson(result);
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType     = "application/json";
                context.Response.ContentLength64 = bytes.Length;
                context.Response.OutputStream.Write(bytes, 0, bytes.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SindyCmd] HTTP 응답 전송 실패: {ex.Message}");
            }
        }

        // ── 리플렉션으로 메서드 실행 (ResultDto 반환) ─────────────────────────────

        private static ResultDto ExecuteMethodCore(string fullMethodName)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            if (string.IsNullOrEmpty(fullMethodName))
                return new ResultDto { success = false, message = "method가 비어있습니다.", timestamp = timestamp };

            // "Namespace.TypeName.MethodName" → 마지막 . 기준으로 분리
            int lastDot = fullMethodName.LastIndexOf('.');
            if (lastDot < 0)
            {
                string msg = $"올바른 형식이 아닙니다 (Namespace.Type.Method 필요): {fullMethodName}";
                Debug.LogError($"[SindyCmd] {msg}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
            }

            string typeName   = fullMethodName.Substring(0, lastDot);
            string methodName = fullMethodName.Substring(lastDot + 1);

            // 모든 로드된 어셈블리에서 타입 탐색
            Type targetType = FindType(typeName);
            if (targetType == null)
            {
                string msg = $"타입을 찾을 수 없습니다: {typeName}";
                Debug.LogError($"[SindyCmd] {msg}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
            }

            MethodInfo method = targetType.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
            {
                string msg = $"static 메서드를 찾을 수 없습니다: {typeName}.{methodName}";
                Debug.LogError($"[SindyCmd] {msg}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
            }

            try
            {
                Debug.Log($"[SindyCmd] 실행: {fullMethodName}");
                method.Invoke(null, null);
                Debug.Log($"[SindyCmd] 완료: {fullMethodName}");
                return new ResultDto { success = true, message = $"OK — {fullMethodName}", timestamp = timestamp };
            }
            catch (TargetInvocationException tie)
            {
                Exception inner = tie.InnerException ?? tie;
                string msg = $"메서드 실행 예외: {inner.GetType().Name}: {inner.Message}";
                Debug.LogError($"[SindyCmd] {msg}\n{inner.StackTrace}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
            }
            catch (Exception ex)
            {
                string msg = $"예외: {ex.GetType().Name}: {ex.Message}";
                Debug.LogError($"[SindyCmd] {msg}\n{ex.StackTrace}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
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
            WriteResultDto(new ResultDto
            {
                id        = id ?? string.Empty,
                success   = success,
                message   = message,
                timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
            });
        }

        private static void WriteResultDto(ResultDto result)
        {
            try
            {
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
