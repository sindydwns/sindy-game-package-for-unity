// ────────────────────────────────────────────────────────────────────────────
// PlacePrefabsInScene — Tests/ViewComponentTest/Prefabs의 프리팹을 씬에 배치
//
// IPC 실행: bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.PlacePrefabsInScene.Execute"
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sindy.Editor.Examples
{
    /// <summary>
    /// Tests/Runtime/ViewComponentTest/Prefabs 폴더의 프리팹 12개를
    /// 현재 열린 씬(_test_component_builder_quick)에 그리드 형태로 배치합니다.
    ///
    /// 배치 규칙:
    ///   - 가로 4개씩, 간격 200 유닛
    ///   - PrefabUtility.InstantiatePrefab 으로 프리팹 연결 유지
    ///
    /// 결과: Temp/sindy_placed_prefabs.json
    /// </summary>
    public static class PlacePrefabsInScene
    {
        private const string PrefabFolder =
            "Assets/sindy-game-package-for-unity/Tests/Runtime/ViewComponentTest/Prefabs";

        private static readonly string[] PrefabNames = new[]
        {
            "notice_popup",
            "character_inventory_popup",
            "label",
            "character_slot",
            "shop_popup",
            "toast_popup",
            "gauge",
            "character_profile_popup",
            "icon",
            "shop_item_slot",
            "button",
            "search_input",
        };

        private const int   Columns = 4;
        private const float Spacing = 200f;

        [MenuItem("Sindy/Examples/▶ Place Prefabs In Scene")]
        public static void Execute()
        {
            var scene = EditorSceneManager.GetActiveScene();
            Debug.Log($"[PlacePrefabsInScene] 씬: {scene.name}  프리팹 {PrefabNames.Length}개 배치 시작");

            var placed = new List<PlacedInfo>();

            for (int i = 0; i < PrefabNames.Length; i++)
            {
                string assetPath = $"{PrefabFolder}/{PrefabNames[i]}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    Debug.LogWarning($"[PlacePrefabsInScene] 프리팹 없음: {assetPath}");
                    continue;
                }

                int col = i % Columns;
                int row = i / Columns;
                var pos = new Vector3(col * Spacing, -row * Spacing, 0f);

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = PrefabNames[i];
                instance.transform.position = pos;

                placed.Add(new PlacedInfo
                {
                    name   = PrefabNames[i],
                    row    = row,
                    col    = col,
                    x      = pos.x,
                    y      = pos.y,
                    z      = pos.z,
                    path   = assetPath,
                });

                Debug.Log($"[PlacePrefabsInScene]  ✓ {PrefabNames[i]}  pos=({pos.x}, {pos.y}, {pos.z})");
            }

            // ── 씬 더럽힘 → 저장 ────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            // ── JSON 결과 저장 ───────────────────────────────────────────────
            string json = BuildJson(scene.name, placed);
            string tempDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Temp"));
            string outputPath = Path.Combine(tempDir, "sindy_placed_prefabs.json");
            File.WriteAllText(outputPath, json, Encoding.UTF8);

            Debug.Log($"[PlacePrefabsInScene] 배치 완료 ({placed.Count}개). JSON: {outputPath}");
        }

        // ── JSON 직렬화 (외부 라이브러리 없이 수동 빌드) ─────────────────────

        private struct PlacedInfo
        {
            public string name;
            public int    row, col;
            public float  x, y, z;
            public string path;
        }

        private static string BuildJson(string sceneName, List<PlacedInfo> list)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"scene\": \"{Esc(sceneName)}\",");
            sb.AppendLine($"  \"count\": {list.Count},");
            sb.AppendLine($"  \"columns\": {Columns},");
            sb.AppendLine($"  \"spacing\": {Spacing},");
            sb.AppendLine("  \"placed\": [");

            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"name\": \"{Esc(p.name)}\",");
                sb.AppendLine($"      \"row\": {p.row},");
                sb.AppendLine($"      \"col\": {p.col},");
                sb.AppendLine($"      \"position\": {{ \"x\": {p.x}, \"y\": {p.y}, \"z\": {p.z} }},");
                sb.AppendLine($"      \"path\": \"{Esc(p.path)}\"");
                sb.Append("    }");
                if (i < list.Count - 1) sb.Append(",");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string Esc(string s)
            => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
#endif
