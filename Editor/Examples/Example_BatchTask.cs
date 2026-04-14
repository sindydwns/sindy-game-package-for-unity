// ────────────────────────────────────────────────────────────────────────────
// 예제 D — Unity 배치 모드 태스크 (BatchEntryPoint)
//
// 배치 실행 방법:
//   ./Tools/batch_run.sh "BatchTest.Ping"
//   ./Tools/batch_run.sh "SetupShowcaseTask.Run"
//
// 에디터 메뉴:
//   Sindy/Batch/Ping           — BatchTest.Ping 메뉴 실행 (에디터 모드)
//   Sindy/Batch/Setup Showcase — SetupShowcaseTask 메뉴 실행 (에디터 모드)
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using Sindy.Editor.SceneTools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Editor.Examples
{
    // ══════════════════════════════════════════════════════════════════════════
    // ReadSceneHierarchy — 열린 씬의 하이라키를 JSON으로 출력
    //
    // IPC 실행: bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.ReadSceneHierarchy.Execute"
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 현재 열려 있는 씬의 전체 하이라키를 재귀적으로 읽어
    /// Debug.Log와 Temp/sindy_hierarchy.json에 출력합니다.
    /// </summary>
    public static class ReadSceneHierarchy
    {
        [MenuItem("Sindy/Batch/▶ Read Scene Hierarchy")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            var sb = new StringBuilder();
            sb.AppendLine($"[ReadSceneHierarchy] Scene: {scene.name}  (루트 오브젝트 {roots.Length}개)");
            sb.AppendLine();

            foreach (var root in roots)
                AppendNode(sb, root, 0);

            Debug.Log(sb.ToString());

            // ── JSON 직렬화 ────────────────────────────────────────────────
            var jsonSb = new StringBuilder();
            jsonSb.AppendLine("{");
            jsonSb.AppendLine($"  \"scene\": \"{EscapeJson(scene.name)}\",");
            jsonSb.AppendLine("  \"hierarchy\": [");

            for (int i = 0; i < roots.Length; i++)
            {
                BuildNodeJson(jsonSb, roots[i], 2);
                if (i < roots.Length - 1) jsonSb.Append(",");
                jsonSb.AppendLine();
            }

            jsonSb.AppendLine("  ]");
            jsonSb.AppendLine("}");

            string tempDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Temp"));
            string outputPath = Path.Combine(tempDir, "sindy_hierarchy.json");
            File.WriteAllText(outputPath, jsonSb.ToString(), Encoding.UTF8);

            Debug.Log($"[ReadSceneHierarchy] JSON 저장 완료: {outputPath}");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        // ── 트리 텍스트 출력 ─────────────────────────────────────────────────

        private static void AppendNode(StringBuilder sb, GameObject go, int depth)
        {
            string indent = new string(' ', depth * 2);
            string activeMarker = go.activeSelf ? "" : " [inactive]";
            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
                if (c != null) compNames.Add(c.GetType().Name);

            sb.AppendLine($"{indent}▸ {go.name}{activeMarker}  [{string.Join(", ", compNames)}]");

            for (int i = 0; i < go.transform.childCount; i++)
                AppendNode(sb, go.transform.GetChild(i).gameObject, depth + 1);
        }

        // ── JSON 빌더 ────────────────────────────────────────────────────────

        private static void BuildNodeJson(StringBuilder sb, GameObject go, int indent)
        {
            string pad = new string(' ', indent * 2);
            var comps = go.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
                if (c != null) compNames.Add($"\"{EscapeJson(c.GetType().Name)}\"");

            sb.AppendLine($"{pad}{{");
            sb.AppendLine($"{pad}  \"name\": \"{EscapeJson(go.name)}\",");
            sb.AppendLine($"{pad}  \"active\": {(go.activeSelf ? "true" : "false")},");
            sb.AppendLine($"{pad}  \"components\": [{string.Join(", ", compNames)}],");
            sb.Append($"{pad}  \"children\": [");

            int childCount = go.transform.childCount;
            if (childCount == 0)
            {
                sb.AppendLine("]");
            }
            else
            {
                sb.AppendLine();
                for (int i = 0; i < childCount; i++)
                {
                    BuildNodeJson(sb, go.transform.GetChild(i).gameObject, indent + 2);
                    if (i < childCount - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine($"{pad}  ]");
            }

            sb.Append($"{pad}}}");
        }

        private static string EscapeJson(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // BatchTest — 배치 모드 동작 확인용 최소 테스트
    //
    // 배치 실행: ./Tools/batch_run.sh "BatchTest.Ping"
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 배치 모드 정상 동작을 확인하는 최소 테스트 태스크.
    /// BatchEntryPoint를 상속하지 않는 단독 static 메서드 패턴 예시.
    /// </summary>
    public static class BatchTest
    {
        [MenuItem("Sindy/Batch/▶ Ping (BatchTest)")]
        public static void Ping()
        {
            Debug.Log("[BatchTest] ========== Ping ==========");
            Debug.Log($"[BatchTest] Unity Version  : {Application.unityVersion}");
            Debug.Log($"[BatchTest] Is Batch Mode  : {Application.isBatchMode}");
            Debug.Log($"[BatchTest] Project Path   : {Application.dataPath}");
            Debug.Log($"[BatchTest] Platform       : {Application.platform}");

            AssetDatabase.Refresh();
            Debug.Log("[BatchTest] AssetDatabase.Refresh() 완료");
            Debug.Log("[BatchTest] ========== Pong! 배치 모드 정상 동작 확인됨 ==========");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SetupShowcaseTask — BatchEntryPoint 상속 패턴 예시
    //
    // 배치 실행: ./Tools/batch_run.sh "SetupShowcaseTask.Run"
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 쇼케이스 씬을 자동으로 세팅하는 배치 태스크 예시.
    /// <para>
    /// BatchEntryPoint를 상속하여 Execute()만 구현하는 패턴:
    /// <list type="bullet">
    ///   <item>RunTask&lt;T&gt;()가 예외 처리, AssetDatabase.Refresh, Success/Fail 종료를 자동 처리</item>
    ///   <item>Log(), LogError()로 배치 결과 파일(Logs/batch_result.txt)에도 기록됨</item>
    /// </list>
    /// </para>
    /// <example>
    /// <code>
    /// // AI가 씬 세팅 작업을 받으면 이런 패턴으로 작성하고 배치 실행:
    /// // ./Tools/batch_run.sh "SetupShowcaseTask.Run"
    /// </code>
    /// </example>
    /// </summary>
    public class SetupShowcaseTask : BatchEntryPoint
    {
        private const string ScenePath =
            "Assets/sindy-game-package-for-unity/Tests/Runtime/ComponentBuilderTest" +
            "/_test_component_builder_quick.unity";

        // ── Unity 배치 모드 진입점 ─────────────────────────────────────────────
        // -executeMethod SetupShowcaseTask.Run 으로 호출됨

        [MenuItem("Sindy/Batch/▶ Setup Showcase Scene")]
        public static void Run() => RunTask<SetupShowcaseTask>();

        // ── 실제 작업 ──────────────────────────────────────────────────────────

        protected override void Execute()
        {
            Log($"씬 열기: {ScenePath}");

            // ⚠ 배치 모드에서 SceneEditor.Open()은 dialog 없이 동작함
            //   (SaveCurrentModifiedScenesIfUserWantsTo → 배치 모드에서 자동 true 반환)
            using var ctx = SceneEditor.Open(ScenePath);
            if (ctx == null)
            {
                LogError($"씬을 열 수 없습니다: {ScenePath}");
                return; // RunTask가 Success() 호출 — 정상 종료로 처리
                        // 실패로 처리하려면 throw new Exception(...);
            }

            AssetFinder.ClearCache();

            SetupShowcaseRunner(ctx);
            SetupHUD(ctx);

            ctx.MarkDirty();
            Log("씬 변경사항 저장 예약 완료 (MarkDirty)");
        }

        // ── (1) ShowcaseRunner GO 설정 ────────────────────────────────────────

        private void SetupShowcaseRunner(SceneEditor ctx)
        {
            // 어셈블리 경계: 타입 전체 이름(FullName) 문자열 오버로드 사용
            var label  = AssetFinder.Prefab("Sindy.View.Components.LabelComponent",  "label");
            var button = AssetFinder.Prefab("Sindy.View.Components.ButtonComponent", "button");

            ctx.GO("ShowcaseRunner")
               .AddComp("Sindy.Test.ShowcaseRunner")
               .SORef("labelPrefab",  label,  ignoreNullWarning: true)
               .SORef("buttonPrefab", button, ignoreNullWarning: true)
               .SOFloat("cellWidth",  240f)
               .SOFloat("cellHeight", 200f)
               .SOInt("gridColumns",  3)
               .SOColor("bgColor",    new Color(0.12f, 0.12f, 0.15f))
               .Apply();

            Log("ShowcaseRunner 컴포넌트 설정 완료");
        }

        // ── (2) Canvas HUD 계층 설정 ─────────────────────────────────────────

        private void SetupHUD(SceneEditor ctx)
        {
            ctx.GO("Canvas.HUD.Title")
               .AddComp<TextMeshProUGUI>()
               .SOStr("m_text",       "ComponentBuilder Showcase")
               .SOFloat("m_fontSize", 28f)
               .SOColor("m_fontColor", new Color(0.5f, 1f, 0.9f))
               .Apply();

            ctx.GO("Canvas.HUD.Background")
               .AddComp<Image>()
               .SOColor("m_Color", new Color(0f, 0f, 0f, 0.6f))
               .Apply();

            Log("Canvas HUD 계층 설정 완료");
        }
    }
}
#endif
