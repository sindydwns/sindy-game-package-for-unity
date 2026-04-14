#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Sindy.Editor.SceneTools
{
    /// <summary>
    /// 에디터에서 Unity 배치 모드 서브프로세스를 실행하는 헬퍼.
    /// <para>
    /// 주요 용도:
    /// <list type="bullet">
    ///   <item>Unity 실행 파일 경로 자동 탐색</item>
    ///   <item>배치 명령어 문자열 생성 (batch_run.sh 없이도 사용 가능)</item>
    ///   <item>에디터 안에서 배치 태스크를 서브프로세스로 직접 실행</item>
    /// </list>
    /// </para>
    /// <example>
    /// <code>
    /// // 에디터에서 배치 태스크 실행
    /// int exitCode = BatchRunner.Run("MyBatchTask.Run", timeoutSeconds: 120);
    ///
    /// // 명령어 문자열만 얻기 (shell 복사용)
    /// string cmd = BatchRunner.BuildCommand("MyBatchTask.Run");
    /// Debug.Log(cmd);
    /// </code>
    /// </example>
    /// </summary>
    public static class BatchRunner
    {
        private const string UnityHubBase = "/Applications/Unity/Hub/Editor";

        private static readonly string ProjectPath =
            Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // ── Unity 경로 탐색 ──────────────────────────────────────────────────────

        /// <summary>
        /// 현재 에디터와 동일한 버전의 Unity 실행 파일 경로를 반환합니다.
        /// 없으면 Hub에 설치된 최신 버전을 반환합니다.
        /// </summary>
        public static string FindUnityPath()
        {
            // 현재 실행 중인 버전 우선
            string currentVersion = Application.unityVersion;
            string byVersion = Path.Combine(
                UnityHubBase, currentVersion, "Unity.app", "Contents", "MacOS", "Unity");
            if (File.Exists(byVersion))
                return byVersion;

            // Hub 디렉토리에서 내림차순(최신)으로 탐색
            if (Directory.Exists(UnityHubBase))
            {
                foreach (string vDir in Directory.GetDirectories(UnityHubBase).OrderByDescending(d => d))
                {
                    string candidate = Path.Combine(vDir, "Unity.app", "Contents", "MacOS", "Unity");
                    if (File.Exists(candidate))
                        return candidate;
                }
            }

            return null;
        }

        // ── 명령어 생성 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 쉘에서 직접 붙여넣을 수 있는 배치 실행 명령어 문자열을 반환합니다.
        /// </summary>
        /// <param name="methodName">실행할 정적 메서드 (예: "BatchTest.Ping")</param>
        /// <param name="logFilePath">로그 파일 경로. null이면 자동 생성.</param>
        public static string BuildCommand(string methodName, string logFilePath = null)
        {
            string unityPath = FindUnityPath();
            if (unityPath == null)
            {
                Debug.LogError("[BatchRunner] Unity 실행 파일을 찾을 수 없습니다.");
                return null;
            }

            logFilePath ??= GetLogFilePath(methodName);

            return $"\"{unityPath}\" " +
                   $"-batchmode -quit " +
                   $"-projectPath \"{ProjectPath}\" " +
                   $"-executeMethod {methodName} " +
                   $"-logFile \"{logFilePath}\"";
        }

        /// <summary>
        /// 타임스탬프 기반 로그 파일 경로를 반환합니다.
        /// 예: Logs/batch_BatchTest_Ping_20250101_120000.log
        /// </summary>
        public static string GetLogFilePath(string methodName = null)
        {
            string logsDir = Path.Combine(ProjectPath, "Logs");
            string tag = string.IsNullOrEmpty(methodName)
                ? string.Empty
                : "_" + methodName.Replace(".", "_");
            return Path.Combine(logsDir,
                $"batch{tag}_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        }

        // ── 실행 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 에디터에서 Unity 배치 모드를 서브프로세스로 실행합니다.
        /// 완료될 때까지 블로킹됩니다 (에디터가 잠깐 멈출 수 있음).
        /// </summary>
        /// <param name="methodName">실행할 정적 메서드 (예: "MyTask.Run")</param>
        /// <param name="timeoutSeconds">타임아웃 (초). 0이면 무제한.</param>
        /// <returns>Unity 프로세스 exit code. 타임아웃 시 -1.</returns>
        public static int Run(string methodName, int timeoutSeconds = 120)
        {
            string unityPath = FindUnityPath();
            if (unityPath == null)
            {
                Debug.LogError("[BatchRunner] Unity 실행 파일을 찾을 수 없습니다.");
                return -1;
            }

            string logFile = GetLogFilePath(methodName);
            string logsDir = Path.GetDirectoryName(logFile)!;
            if (!Directory.Exists(logsDir))
                Directory.CreateDirectory(logsDir);

            string args = $"-batchmode -quit " +
                          $"-projectPath \"{ProjectPath}\" " +
                          $"-executeMethod {methodName} " +
                          $"-logFile \"{logFile}\"";

            Debug.Log($"[BatchRunner] 실행: {unityPath}\n인수: {args}");

            var psi = new ProcessStartInfo(unityPath, args)
            {
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError  = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            bool finished;
            if (timeoutSeconds > 0)
                finished = process.WaitForExit(timeoutSeconds * 1000);
            else
            {
                process.WaitForExit();
                finished = true;
            }

            if (!finished)
            {
                Debug.LogError($"[BatchRunner] 타임아웃 ({timeoutSeconds}초): {methodName}");
                try { process.Kill(); } catch { /* 무시 */ }
                return -1;
            }

            int exitCode = process.ExitCode;
            if (exitCode == 0)
                Debug.Log($"[BatchRunner] ✅ 성공: {methodName}  →  로그: {logFile}");
            else
                Debug.LogError($"[BatchRunner] ❌ 실패 (exit={exitCode}): {methodName}  →  로그: {logFile}");

            return exitCode;
        }

        // ── 메뉴 아이템 ──────────────────────────────────────────────────────────

        [MenuItem("Sindy/Batch/▶ Show Unity Path")]
        public static void ShowUnityPath()
        {
            string path = FindUnityPath();
            if (path == null)
                Debug.LogWarning("[BatchRunner] Unity 실행 파일을 찾을 수 없습니다.");
            else
            {
                GUIUtility.systemCopyBuffer = path;
                Debug.Log($"[BatchRunner] Unity 경로 (클립보드 복사됨): {path}");
            }
        }

        [MenuItem("Sindy/Batch/▶ Copy batch_run.sh Command (BatchTest.Ping)")]
        public static void CopyPingCommand()
        {
            string cmd = BuildCommand("BatchTest.Ping");
            if (cmd == null) return;
            GUIUtility.systemCopyBuffer = cmd;
            Debug.Log($"[BatchRunner] 명령어 복사됨:\n{cmd}");
        }
    }
}
#endif
