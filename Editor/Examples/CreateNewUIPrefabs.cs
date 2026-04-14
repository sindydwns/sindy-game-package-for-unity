// ────────────────────────────────────────────────────────────────────────────
// 작업 B — 새 UI 프리팹 8개 생성
//
// IPC 실행:
//   bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.CreateNewUIPrefabs.Execute"
//
// 결과:
//   Temp/sindy_created_prefabs.json
//
// 주의: Image 와 TextMeshProUGUI 는 같은 Graphic 기반이므로
//       동일 GameObject에 동시에 AddComponent 불가. 텍스트는 별도 자식 GO에 배치.
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Editor.Examples
{
    public static class CreateNewUIPrefabs
    {
        private const string PrefabFolder =
            "Assets/sindy-game-package-for-unity/Tests/Runtime/ViewComponentTest/Prefabs";

        private static readonly Color BrightBg    = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color BrightPanel = new Color(0.92f, 0.93f, 0.95f, 1f);
        private static readonly Color AccentBlue  = new Color(0.55f, 0.75f, 0.98f, 1f);
        private static readonly Color AccentGreen = new Color(0.55f, 0.90f, 0.70f, 1f);
        private static readonly Color AccentRed   = new Color(0.98f, 0.55f, 0.55f, 1f);
        private static readonly Color TextColor   = new Color(0.20f, 0.20f, 0.22f, 1f);
        private static readonly Color FillColor   = new Color(0.40f, 0.75f, 0.95f, 1f);

        [MenuItem("Sindy/Examples/B - Create New UI Prefabs")]
        public static void Execute()
        {
            var created = new List<string>();

            created.Add(CreateTabBar());
            created.Add(CreateProgressBar());
            created.Add(CreateDialogBox());
            created.Add(CreateTooltip());
            created.Add(CreateBadge());
            created.Add(CreateScrollList());
            created.Add(CreateDropdownMenu());
            created.Add(CreateCardItem());

            AssetDatabase.Refresh();
            SaveJson(created);

            Debug.Log($"[CreateNewUIPrefabs] 완료 — 프리팹 {created.Count}개 생성");

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        // ── (1) tab_bar ───────────────────────────────────────────────────────

        private static string CreateTabBar()
        {
            var root = NewRect("tab_bar", new Vector2(600f, 60f));
            AddImage(root, BrightPanel);

            var layout = root.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 4, 4);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (int i = 1; i <= 3; i++)
            {
                // 배경 Image 용 GO
                var item = NewRect($"tab_item_{i}", Vector2.zero, root.transform);
                AddImage(item, i == 1 ? AccentBlue : BrightBg);
                // 텍스트 전용 자식 GO (Image와 동일 GO에 TMP 불가)
                var lbl = NewRect("Label", Vector2.zero, item.transform);
                StretchFull(lbl);
                AddTMPText(lbl, $"Tab {i}", 14f, TextColor);
            }

            return Save(root, "tab_bar");
        }

        // ── (2) progress_bar ──────────────────────────────────────────────────

        private static string CreateProgressBar()
        {
            var root = NewRect("progress_bar", new Vector2(400f, 30f));
            // 루트에는 Image 없음 — 배경/Fill이 자식으로 분리
            var bg = NewRect("Background", Vector2.zero, root.transform);
            AddImage(bg, new Color(0.85f, 0.86f, 0.88f, 1f));
            StretchFull(bg);

            var fill = NewRect("Fill", Vector2.zero, root.transform);
            AddImage(fill, FillColor);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = new Vector2(0f, 0f);
            fillRT.anchorMax = new Vector2(0.5f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = Vector2.zero;

            return Save(root, "progress_bar");
        }

        // ── (3) dialog_box ────────────────────────────────────────────────────

        private static string CreateDialogBox()
        {
            var root = NewRect("dialog_box", new Vector2(420f, 260f));
            AddImage(root, BrightBg);

            // 제목 — 텍스트만
            var title = NewRect("Title", new Vector2(380f, 40f), root.transform);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 90f);
            AddTMPText(title, "알림", 20f, TextColor);

            // 확인 버튼 — Image 자식 + Label 자식
            var btnOk = NewRect("Button_OK", new Vector2(140f, 44f), root.transform);
            btnOk.GetComponent<RectTransform>().anchoredPosition = new Vector2(80f, -90f);
            AddImage(btnOk, AccentBlue);
            var lblOk = NewRect("Label", new Vector2(140f, 44f), btnOk.transform);
            StretchFull(lblOk);
            AddTMPText(lblOk, "확인", 16f, TextColor);

            // 취소 버튼
            var btnCancel = NewRect("Button_Cancel", new Vector2(140f, 44f), root.transform);
            btnCancel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-80f, -90f);
            AddImage(btnCancel, new Color(0.88f, 0.88f, 0.90f, 1f));
            var lblCancel = NewRect("Label", new Vector2(140f, 44f), btnCancel.transform);
            StretchFull(lblCancel);
            AddTMPText(lblCancel, "취소", 16f, TextColor);

            return Save(root, "dialog_box");
        }

        // ── (4) tooltip ───────────────────────────────────────────────────────

        private static string CreateTooltip()
        {
            var root = NewRect("tooltip", new Vector2(220f, 50f));
            AddImage(root, new Color(0.20f, 0.22f, 0.26f, 0.92f));

            // 텍스트 전용 자식
            var lbl = NewRect("Text", Vector2.zero, root.transform);
            StretchFull(lbl);
            AddTMPText(lbl, "툴팁 내용", 13f, new Color(0.95f, 0.95f, 0.95f, 1f));

            return Save(root, "tooltip");
        }

        // ── (5) badge ─────────────────────────────────────────────────────────

        private static string CreateBadge()
        {
            var root = NewRect("badge", new Vector2(32f, 32f));
            AddImage(root, AccentRed);

            var lbl = NewRect("Count", Vector2.zero, root.transform);
            StretchFull(lbl);
            AddTMPText(lbl, "0", 14f, new Color(1f, 1f, 1f, 1f));

            return Save(root, "badge");
        }

        // ── (6) scroll_list ───────────────────────────────────────────────────

        private static string CreateScrollList()
        {
            var root = NewRect("scroll_list", new Vector2(340f, 400f));
            AddImage(root, BrightPanel);

            var viewport = NewRect("Viewport", Vector2.zero, root.transform);
            StretchFull(viewport);
            viewport.AddComponent<RectMask2D>();

            var content = NewRect("Content", new Vector2(340f, 800f), viewport.transform);
            var contentRT = content.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.offsetMin = Vector2.zero;
            contentRT.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 4f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            for (int i = 1; i <= 3; i++)
            {
                var item = NewRect($"item_{i}", new Vector2(332f, 60f), content.transform);
                AddImage(item, BrightBg);
                var itemLbl = NewRect("Label", Vector2.zero, item.transform);
                StretchFull(itemLbl);
                AddTMPText(itemLbl, $"항목 {i}", 14f, TextColor);
            }

            // ScrollRect: 직접 프로퍼티 세터 대신 SerializedObject 사용
            var sr = root.AddComponent<ScrollRect>();
            var srSO = new SerializedObject(sr);
            srSO.Update();
            srSO.FindProperty("m_Viewport").objectReferenceValue = viewport.GetComponent<RectTransform>();
            srSO.FindProperty("m_Content").objectReferenceValue  = content.GetComponent<RectTransform>();
            srSO.FindProperty("m_Horizontal").boolValue          = false;
            srSO.FindProperty("m_Vertical").boolValue            = true;
            srSO.ApplyModifiedPropertiesWithoutUndo();

            return Save(root, "scroll_list");
        }

        // ── (7) dropdown_menu ─────────────────────────────────────────────────

        private static string CreateDropdownMenu()
        {
            var root = NewRect("dropdown_menu", new Vector2(260f, 44f));
            AddImage(root, BrightBg);

            // 선택된 텍스트 — TMP만
            var selected = NewRect("Selected", new Vector2(220f, 36f), root.transform);
            selected.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10f, 0f);
            AddTMPText(selected, "항목 선택", 14f, TextColor);

            // 화살표 아이콘 — Image만
            var arrow = NewRect("Arrow", new Vector2(24f, 24f), root.transform);
            var arrowRT = arrow.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1f, 0.5f);
            arrowRT.anchorMax = new Vector2(1f, 0.5f);
            arrowRT.anchoredPosition = new Vector2(-16f, 0f);
            AddImage(arrow, new Color(0.50f, 0.55f, 0.65f, 1f));

            // 드롭다운 목록 패널
            var list = NewRect("List", new Vector2(260f, 132f), root.transform);
            var listRT = list.GetComponent<RectTransform>();
            listRT.anchorMin = new Vector2(0f, 0f);
            listRT.anchorMax = new Vector2(1f, 0f);
            listRT.pivot     = new Vector2(0.5f, 1f);
            listRT.anchoredPosition = new Vector2(0f, -2f);
            AddImage(list, BrightBg);
            list.SetActive(false);

            var vlg = list.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 2f;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            for (int i = 1; i <= 3; i++)
            {
                var opt = NewRect($"Option_{i}", new Vector2(252f, 36f), list.transform);
                AddImage(opt, BrightPanel);
                var optLbl = NewRect("Label", Vector2.zero, opt.transform);
                StretchFull(optLbl);
                AddTMPText(optLbl, $"옵션 {i}", 13f, TextColor);
            }

            return Save(root, "dropdown_menu");
        }

        // ── (8) card_item ─────────────────────────────────────────────────────

        private static string CreateCardItem()
        {
            var root = NewRect("card_item", new Vector2(280f, 340f));
            AddImage(root, BrightBg);

            // 썸네일 — Image만
            var thumb = NewRect("Thumbnail", Vector2.zero, root.transform);
            var thumbRT = thumb.GetComponent<RectTransform>();
            thumbRT.anchorMin = new Vector2(0f, 1f);
            thumbRT.anchorMax = new Vector2(1f, 1f);
            thumbRT.pivot     = new Vector2(0.5f, 1f);
            thumbRT.offsetMin = Vector2.zero;
            thumbRT.offsetMax = Vector2.zero;
            thumbRT.sizeDelta = new Vector2(0f, 180f);
            AddImage(thumb, new Color(0.80f, 0.85f, 0.92f, 1f));

            // 제목 — TMP만
            var titleGO = NewRect("Title", new Vector2(256f, 32f), root.transform);
            titleGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -196f);
            AddTMPText(titleGO, "카드 제목", 16f, TextColor);

            // 설명 — TMP만
            var desc = NewRect("Description", new Vector2(256f, 60f), root.transform);
            desc.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -248f);
            AddTMPText(desc, "카드 설명 텍스트가 여기에 표시됩니다.", 12f,
                new Color(0.45f, 0.45f, 0.50f, 1f));

            return Save(root, "card_item");
        }

        // ── 공통 헬퍼 ─────────────────────────────────────────────────────────

        private static GameObject NewRect(string name, Vector2 size, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            return go;
        }

        private static void StretchFull(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void AddImage(GameObject go, Color color)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
        }

        /// <summary>
        /// TextMeshProUGUI 전용 GO에만 호출. Image가 이미 붙은 GO에는 사용 금지.
        /// (Image 와 TMP 는 모두 Graphic 파생 — 동일 GO에 AddComponent 시 null 반환)
        /// </summary>
        private static void AddTMPText(GameObject go, string text, float fontSize, Color color)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                Debug.LogError($"[CreateNewUIPrefabs] TextMeshProUGUI AddComponent 실패: {go.name}");
                return;
            }

            var so = new SerializedObject(tmp);
            so.Update();

            var textProp  = so.FindProperty("m_text");
            var sizeProp  = so.FindProperty("m_fontSize");
            var colorProp = so.FindProperty("m_fontColor");

            if (textProp  != null) textProp.stringValue = text;
            if (sizeProp  != null) sizeProp.floatValue  = fontSize;
            if (colorProp != null) colorProp.colorValue = color;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string Save(GameObject root, string prefabName)
        {
            string assetPath = $"{PrefabFolder}/{prefabName}.prefab";
            bool success = PrefabUtility.SaveAsPrefabAsset(root, assetPath) != null;
            Object.DestroyImmediate(root);

            if (success)
                Debug.Log($"[CreateNewUIPrefabs] 저장: {assetPath}");
            else
                Debug.LogError($"[CreateNewUIPrefabs] 저장 실패: {assetPath}");

            return success ? assetPath : $"FAILED:{assetPath}";
        }

        // ── JSON 저장 ─────────────────────────────────────────────────────────

        private static void SaveJson(List<string> paths)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"task\": \"CreateNewUIPrefabs\",");
            sb.AppendLine("  \"count\": " + paths.Count + ",");
            sb.AppendLine("  \"prefabs\": [");

            for (int i = 0; i < paths.Count; i++)
            {
                string comma = i < paths.Count - 1 ? "," : "";
                sb.AppendLine($"    \"{Esc(paths[i])}\"{comma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            string tempDir = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Temp"));
            string outputPath = Path.Combine(tempDir, "sindy_created_prefabs.json");
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[CreateNewUIPrefabs] JSON 저장: {outputPath}");
        }

        private static string Esc(string s)
            => s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
    }
}
#endif
