// ────────────────────────────────────────────────────────────────────────────
// EditorCommandWatcher — HTTP IPC 서버
//
// HTTP 서버: POST /execute, POST /edit, GET /ping 엔드포인트
// 포트: EditorPrefs "Sindy.EditorTools.HttpPort" (기본값 6060)
// 포트 변경: Edit > Preferences > Sindy
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
        // ── HTTP 서버 ─────────────────────────────────────────────────────────────
        private const int DefaultPort = 6060;
        private const string PortPrefKey = "Sindy.EditorTools.HttpPort";

        private static HttpListener _httpListener;
        private static Thread _listenerThread;
        private static readonly ConcurrentQueue<PendingRequest> _requestQueue
            = new ConcurrentQueue<PendingRequest>();

        // ── 직렬화 DTO ──────────────────────────────────────────────────────────

        [Serializable]
        private class ResultDto
        {
            public string id;
            public bool success;
            public string message;
            public string timestamp;
        }

        [Serializable]
        private class CommandData
        {
            public string method;
        }

        [Serializable]
        private class EditRequestDto
        {
            public string asset;
            public string go;
            public string prop;
            // value 필드는 string/number/bool/array 등 이형(heterogeneous)이므로
            // 별도 파싱 로직(ParseValueFromJson)에서 처리합니다.
        }

        private class PendingRequest
        {
            public string RequestType; // "ping" | "execute" | "edit"
            public string Method;
            public string RawBody;
            public HttpListenerContext Context;
        }

        // ── 초기화 ──────────────────────────────────────────────────────────────

        static EditorCommandWatcher()
        {
            EditorApplication.update += Poll;
            AssemblyReloadEvents.beforeAssemblyReload += StopHttpServer;
            StartHttpServer();
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
                        else if (context.Request.HttpMethod == "POST" && urlPath == "/edit")
                        {
                            var body = new StreamReader(context.Request.InputStream).ReadToEnd();
                            _requestQueue.Enqueue(new PendingRequest
                            {
                                RequestType = "edit",
                                RawBody = body,
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
            })
            { IsBackground = true };
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
            while (_requestQueue.TryDequeue(out var req))
            {
                ResultDto result;
                if (req.RequestType == "ping")
                {
                    result = new ResultDto
                    {
                        id = "",
                        success = true,
                        message = "pong",
                        timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
                    };
                }
                else if (req.RequestType == "edit")
                {
                    result = ExecuteEditCore(req.RawBody);
                }
                else
                {
                    result = ExecuteMethodCore(req.Method);
                }
                SendHttpResponse(req.Context, result);
            }
        }

        // ── HTTP 응답 전송 ────────────────────────────────────────────────────────

        private static void SendHttpResponse(HttpListenerContext context, ResultDto result)
        {
            try
            {
                var json = JsonUtility.ToJson(result);
                var bytes = Encoding.UTF8.GetBytes(json);
                context.Response.ContentType = "application/json";
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

            string typeName = fullMethodName.Substring(0, lastDot);
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

        // ── /edit 엔드포인트 처리 ────────────────────────────────────────────────

        /// <summary>
        /// POST /edit 요청을 처리합니다.
        /// SindyEdit 파사드를 통해 씬·프리팹·SO를 동적으로 편집합니다.
        /// </summary>
        private static ResultDto ExecuteEditCore(string rawJson)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            if (string.IsNullOrEmpty(rawJson))
                return new ResultDto { success = false, message = "요청 본문이 비어있습니다.", timestamp = timestamp };

            try
            {
                var dto = JsonUtility.FromJson<EditRequestDto>(rawJson);

                if (string.IsNullOrEmpty(dto?.asset))
                    return new ResultDto { success = false, message = "asset 필드가 없습니다.", timestamp = timestamp };

                if (string.IsNullOrEmpty(dto.prop))
                    return new ResultDto { success = false, message = "prop 필드가 없습니다.", timestamp = timestamp };

                object value = ParseValueFromJson(rawJson);
                if (value == null)
                    return new ResultDto { success = false, message = "value 필드를 파싱할 수 없습니다.", timestamp = timestamp };

                // 에셋 세션 열기: 전체 경로면 Open, 이름이면 Find
                bool isPath = dto.asset.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)
                           || dto.asset.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
                var session = isPath ? SindyEdit.Open(dto.asset) : SindyEdit.Find(dto.asset);

                if (session == null)
                    return new ResultDto
                    {
                        success = false,
                        message = $"에셋을 열 수 없습니다: {dto.asset}",
                        timestamp = timestamp
                    };

                using (session)
                {
                    if (!string.IsNullOrEmpty(dto.go))
                        session.GO(dto.go);

                    session.SetProperty(dto.prop, value);
                }

                string goDesc = string.IsNullOrEmpty(dto.go) ? "" : $".GO({dto.go})";
                return new ResultDto
                {
                    success = true,
                    message = $"OK — {dto.asset}{goDesc}.{dto.prop}",
                    timestamp = timestamp
                };
            }
            catch (Exception ex)
            {
                string msg = $"예외: {ex.GetType().Name}: {ex.Message}";
                Debug.LogError($"[SindyCmd] /edit 처리 중 예외: {msg}\n{ex.StackTrace}");
                return new ResultDto { success = false, message = msg, timestamp = timestamp };
            }
        }

        /// <summary>
        /// JSON 본문에서 "value" 키의 값을 파싱합니다.
        /// <para>
        /// - string → string<br/>
        /// - true/false → bool<br/>
        /// - 정수 → int<br/>
        /// - 소수점 포함 숫자 → float<br/>
        /// - 2개 float 배열 → Vector2<br/>
        /// - 3개 float 배열 → Vector3<br/>
        /// - 4개 float 배열 → Color
        /// </para>
        /// </summary>
        private static object ParseValueFromJson(string rawJson)
        {
            // "value" 키 위치 탐색
            int keyIdx = rawJson.IndexOf("\"value\"", StringComparison.Ordinal);
            if (keyIdx < 0) return null;

            int colon = rawJson.IndexOf(':', keyIdx + 7);
            if (colon < 0) return null;

            // 공백 스킵
            int pos = colon + 1;
            while (pos < rawJson.Length && char.IsWhiteSpace(rawJson[pos])) pos++;
            if (pos >= rawJson.Length) return null;

            char first = rawJson[pos];

            // ── 문자열 ─────────────────────────────────────────────────────
            if (first == '"')
            {
                pos++; // opening quote 스킵
                var sb = new System.Text.StringBuilder();
                while (pos < rawJson.Length)
                {
                    char ch = rawJson[pos];
                    if (ch == '\\' && pos + 1 < rawJson.Length)
                    {
                        pos++;
                        sb.Append(rawJson[pos]);
                    }
                    else if (ch == '"') break;
                    else sb.Append(ch);
                    pos++;
                }
                return sb.ToString();
            }

            // ── 배열 ───────────────────────────────────────────────────────
            if (first == '[')
            {
                int end = rawJson.IndexOf(']', pos);
                if (end < 0) return null;

                string content = rawJson.Substring(pos + 1, end - pos - 1).Trim();
                if (string.IsNullOrEmpty(content)) return null;

                string[] parts = content.Split(',');
                var floats = new System.Collections.Generic.List<float>(4);
                foreach (var part in parts)
                {
                    if (float.TryParse(part.Trim(),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float f))
                        floats.Add(f);
                }

                if (floats.Count == 4) return new Color(floats[0], floats[1], floats[2], floats[3]);
                if (floats.Count == 3) return new Vector3(floats[0], floats[1], floats[2]);
                if (floats.Count == 2) return new Vector2(floats[0], floats[1]);
                return null;
            }

            // ── bool / 숫자 ────────────────────────────────────────────────
            int tokEnd = pos;
            while (tokEnd < rawJson.Length
                   && rawJson[tokEnd] != ','
                   && rawJson[tokEnd] != '}'
                   && rawJson[tokEnd] != ']'
                   && !char.IsWhiteSpace(rawJson[tokEnd]))
                tokEnd++;

            string token = rawJson.Substring(pos, tokEnd - pos).Trim();

            if (token == "true") return true;
            if (token == "false") return false;
            if (token == "null") return null;

            // 소수점 또는 지수 표기가 있으면 float
            if (token.Contains('.') || token.Contains('e') || token.Contains('E'))
            {
                if (float.TryParse(token,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float fv))
                    return fv;
            }
            else
            {
                if (int.TryParse(token, out int iv)) return iv;
            }

            return null;
        }

        // ── 타입 탐색 ────────────────────────────────────────────────────────────

        private static Type FindType(string typeName)
        {
            // 1차: 풀네임 일치 (Namespace.TypeName)
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(typeName);
                if (t != null)
                    return t;
            }

            // 2차: 심플네임 일치 (TypeName만 전달된 경우 폴백)
            string simpleName = typeName.Contains('.')
                ? typeName.Substring(typeName.LastIndexOf('.') + 1)
                : typeName;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name == simpleName)
                        return t;
                }
            }

            return null;
        }

    }
}
#endif
