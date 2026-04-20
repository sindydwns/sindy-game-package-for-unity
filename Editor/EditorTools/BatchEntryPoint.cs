#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// Unity 배치 모드 태스크의 베이스 클래스.
    /// <para>
    /// AI는 이 클래스를 상속하여 <see cref="Execute"/>만 구현하면 됩니다.
    /// Unity 배치 모드 진입점은 정적 메서드로 <see cref="RunTask{T}"/>를 호출하세요.
    /// </para>
    /// <example>
    /// <code>
    /// public class MyBatchTask : BatchEntryPoint
    /// {
    ///     // Unity -executeMethod MyBatchTask.Run 으로 호출됨
    ///     public static void Run() => RunTask<MyBatchTask>();
    ///
    ///     protected override void Execute()
    ///     {
    ///         Log("작업 시작");
    ///
    ///         using var ctx = SceneEditor.Open("Assets/.../MyScene.unity");
    ///         ctx.GO("SomeObject").AddComp<SomeComp>().Apply();
    ///         ctx.MarkDirty();
    ///
    ///         Log("작업 완료");
    ///         // RunTask가 자동으로 Success() 호출
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// ⚠ 배치 모드 주의사항:
    ///   - AssetDatabase.Refresh()는 RunTask 시작 시 자동 호출됨
    ///   - EditorUtility.DisplayDialog 사용 불가 (배치 모드에서 자동 무시됨)
    ///   - Dispose / using 블록이 완료되기 전에 Exit가 호출되지 않도록 주의
    /// </remarks>
    /// </summary>
    public abstract class BatchEntryPoint
    {
        // 배치 결과 요약 파일 (batch_run.sh가 파싱하여 출력)
        private static string _resultFilePath;

        static BatchEntryPoint()
        {
            string logsDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Logs"));
            _resultFilePath = Path.Combine(logsDir, "batch_result.txt");
        }

        // ── 진입점 헬퍼 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 배치 태스크를 안전하게 실행합니다.
        /// 예외 발생 시 자동으로 <see cref="Fail"/>을 호출합니다.
        /// </summary>
        /// <typeparam name="T">실행할 BatchEntryPoint 서브클래스</typeparam>
        protected static void RunTask<T>() where T : BatchEntryPoint, new()
        {
            string taskName = typeof(T).Name;
            ClearResultFile();
            Log($"[{taskName}] 배치 태스크 시작");

            try
            {
                AssetDatabase.Refresh();
                new T().Execute();
                Success($"[{taskName}] 태스크 완료");
            }
            catch (Exception e)
            {
                Fail($"[{taskName}] 태스크 실패: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 실제 배치 작업을 구현합니다. 예외를 던지면 Fail()이 자동 호출됩니다.
        /// </summary>
        protected abstract void Execute();

        // ── 로그 헬퍼 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 로그 메시지를 Debug.Log와 결과 파일에 기록합니다.
        /// </summary>
        protected static void Log(string msg)
        {
            Debug.Log(msg);
            AppendResult($"[LOG] {msg}");
        }

        /// <summary>
        /// 에러 메시지를 Debug.LogError와 결과 파일에 기록합니다.
        /// </summary>
        protected static void LogError(string msg)
        {
            Debug.LogError(msg);
            AppendResult($"[ERROR] {msg}");
        }

        // ── 종료 헬퍼 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 성공으로 종료합니다. 배치 모드에서는 EditorApplication.Exit(0)을 호출합니다.
        /// </summary>
        protected static void Success(string msg = null)
        {
            if (!string.IsNullOrEmpty(msg))
                Debug.Log(msg);
            AppendResult($"[SUCCESS] {msg ?? "완료"}");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
            else
                Debug.Log("[BatchEntryPoint] 에디터 모드 — Exit(0) 생략");
        }

        /// <summary>
        /// 실패로 종료합니다. 배치 모드에서는 EditorApplication.Exit(1)을 호출합니다.
        /// </summary>
        protected static void Fail(string msg)
        {
            Debug.LogError(msg);
            AppendResult($"[FAIL] {msg}");

            if (Application.isBatchMode)
                EditorApplication.Exit(1);
            else
                Debug.LogError("[BatchEntryPoint] 에디터 모드 — Exit(1) 생략");
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

        private static void ClearResultFile()
        {
            try
            {
                EnsureLogsDir();
                File.WriteAllText(_resultFilePath, string.Empty);
            }
            catch { /* 무시 */ }
        }

        private static void AppendResult(string line)
        {
            try
            {
                EnsureLogsDir();
                File.AppendAllText(_resultFilePath,
                    $"{DateTime.Now:HH:mm:ss}  {line}\n");
            }
            catch { /* 무시 */ }
        }

        private static void EnsureLogsDir()
        {
            string dir = Path.GetDirectoryName(_resultFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
#endif
