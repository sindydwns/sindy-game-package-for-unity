// ────────────────────────────────────────────────────────────────────────────
// 작업 A — 기존 12개 프리팹 색상을 밝은 톤으로 수정
//
// IPC 실행:
//   bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.ModifyPrefabColors.Execute"
//
// 결과:
//   Temp/sindy_color_result.json
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using Sindy.Editor.SceneTools;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Editor.Examples
{
    public static class ModifyPrefabColors
    {
        private const string PrefabFolder =
            "Assets/sindy-game-package-for-unity/Tests/Runtime/ViewComponentTest/Prefabs";

        private static readonly string[] PrefabNames =
        {
            "button",
            "character_inventory_popup",
            "character_profile_popup",
            "character_slot",
            "gauge",
            "icon",
            "label",
            "notice_popup",
            "search_input",
            "shop_item_slot",
            "shop_popup",
            "toast_popup",
        };

        [MenuItem("Sindy/Examples/A - Modify Prefab Colors")]
        public static void Execute()
        {
            var results = new List<PrefabColorResult>();

            foreach (var name in PrefabNames)
            {
                string path = $"{PrefabFolder}/{name}.prefab";
                var prefabResult = new PrefabColorResult { prefab = name, changes = new List<ColorChange>() };

                using (var p = PrefabEdit.Open(path))
                {
                    if (p == null)
                    {
                        prefabResult.error = "로드 실패";
                        results.Add(prefabResult);
                        continue;
                    }

                    var allImages = p.RootObject.GetComponentsInChildren<Image>(true);

                    foreach (var img in allImages)
                    {
                        var so = new SerializedObject(img);
                        so.Update();

                        var colorProp = so.FindProperty("m_Color");
                        if (colorProp == null) continue;

                        Color orig = colorProp.colorValue;
                        Color brightened = new Color(
                            Mathf.Lerp(orig.r, 1f, 0.45f),
                            Mathf.Lerp(orig.g, 1f, 0.45f),
                            Mathf.Lerp(orig.b, 1f, 0.45f),
                            orig.a
                        );

                        colorProp.colorValue = brightened;
                        so.ApplyModifiedPropertiesWithoutUndo();

                        prefabResult.changes.Add(new ColorChange
                        {
                            gameObject = img.gameObject.name,
                            before = ColorToHex(orig),
                            after = ColorToHex(brightened),
                        });
                    }
                }

                results.Add(prefabResult);
                Debug.Log($"[ModifyPrefabColors] {name}: Image {prefabResult.changes.Count}개 밝게 처리 완료");
            }

            SaveJson(results);

            int totalChanges = 0;
            foreach (var r in results) if (r.changes != null) totalChanges += r.changes.Count;
            Debug.Log($"[ModifyPrefabColors] 완료 — 프리팹 {results.Count}개, 색상 변경 {totalChanges}건");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        // ── JSON 저장 ─────────────────────────────────────────────────────────

        private static void SaveJson(List<PrefabColorResult> results)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"task\": \"ModifyPrefabColors\",");
            sb.AppendLine("  \"prefabs\": [");

            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"prefab\": \"{Esc(r.prefab)}\",");
                if (!string.IsNullOrEmpty(r.error))
                {
                    sb.AppendLine($"      \"error\": \"{Esc(r.error)}\"");
                }
                else
                {
                    sb.AppendLine($"      \"changeCount\": {r.changes.Count},");
                    sb.AppendLine("      \"changes\": [");
                    for (int j = 0; j < r.changes.Count; j++)
                    {
                        var c = r.changes[j];
                        string comma = j < r.changes.Count - 1 ? "," : "";
                        sb.AppendLine($"        {{ \"go\": \"{Esc(c.gameObject)}\", \"before\": \"{c.before}\", \"after\": \"{c.after}\" }}{comma}");
                    }
                    sb.Append("      ]");
                    sb.AppendLine();
                }
                string prefabComma = i < results.Count - 1 ? "," : "";
                sb.AppendLine($"    }}{prefabComma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            string tempDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Temp"));
            string outputPath = Path.Combine(tempDir, "sindy_color_result.json");
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ModifyPrefabColors] JSON 저장: {outputPath}");
        }

        // ── 유틸 ─────────────────────────────────────────────────────────────

        private static string ColorToHex(Color c)
        {
            int r = Mathf.RoundToInt(c.r * 255);
            int g = Mathf.RoundToInt(c.g * 255);
            int b = Mathf.RoundToInt(c.b * 255);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static string Esc(string s)
            => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

        // ── 데이터 구조 ───────────────────────────────────────────────────────

        private class PrefabColorResult
        {
            public string prefab;
            public string error;
            public List<ColorChange> changes;
        }

        private class ColorChange
        {
            public string gameObject;
            public string before;
            public string after;
        }
    }
}
#endif
