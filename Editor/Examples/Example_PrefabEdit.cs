// ────────────────────────────────────────────────────────────────────────────
// 예제 B — 프리팹 편집 (PrefabEditor / GOEditor / AssetFinder)
//
// 구현 파일: Editor/EditorTools/PrefabEditor.cs, GOEditor.cs, AssetFinder.cs
//
// 전제: 아래 경로에 편집 대상 프리팹이 존재해야 합니다.
//   GaugePrefabPath — GaugeComponent가 붙은 프리팹
//   LabelPrefabPath — LabelComponent가 붙은 프리팹
//
// 실제 경로는 프로젝트 구조에 맞게 수정하세요.
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using Sindy.Editor.EditorTools;
using Sindy.View.Components;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Editor.Examples
{
    /// <summary>
    /// 예제 B — PrefabEditor, GOEditor 사용법
    ///
    /// 시나리오:
    ///   (1) AssetFinder로 GaugeComponent 프리팹을 찾아 fill 색상을 변경
    ///   (2) 경로를 직접 지정해 LabelComponent 프리팹 내부 구조를 편집
    ///   (3) 모든 GaugeComponent 프리팹을 일괄 편집
    ///
    /// Menu: Sindy/Examples/B - Prefab Edit
    /// </summary>
    public static class Example_PrefabEdit
    {
        private static string GaugePrefabPath =>
            PackagePathHelper.Resolve("Tests/Runtime/ComponentBuilderTest/Prefabs/Gauge.prefab");

        private static string LabelPrefabPath =>
            PackagePathHelper.Resolve("Tests/Runtime/ComponentBuilderTest/Prefabs/Label.prefab");

        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Sindy/Examples/B - Prefab Edit (AssetFinder 탐색)")]
        public static void RunWithAssetFinder()
        {
            var prefabGOs = AssetFinder.AllPrefabs<GaugeComponent>();
            if (prefabGOs.Count == 0)
            {
                EditorUtility.DisplayDialog("예제 B", "GaugeComponent 프리팹을 찾을 수 없습니다.", "확인");
                return;
            }

            string path = AssetDatabase.GetAssetPath(prefabGOs[0]);
            EditGaugePrefab(path);
        }

        [MenuItem("Sindy/Examples/B - Prefab Edit (경로 직접 지정)")]
        public static void RunWithDirectPath()
        {
            var gaugePath = GaugePrefabPath;
            var labelPath = LabelPrefabPath;

            if (!System.IO.File.Exists(gaugePath))
            {
                Debug.LogError(
                    $"[Example B] 프리팹을 찾을 수 없습니다: {gaugePath}\n" +
                    "UPM 설치 방식(Git/Local/Embedded)에 따라 경로가 다를 수 있습니다. " +
                    "PackagePathHelper.Resolve()를 확인하세요.");
                return;
            }

            EditGaugePrefab(gaugePath);
            EditLabelPrefab(labelPath);
        }

        // ─── GaugeComponent 프리팹 편집 ──────────────────────────────────────

        /// <summary>
        /// GaugeComponent 프리팹의 fill Image 색상을 변경한다.
        ///
        /// ⚠ Unity 내부 직렬화 필드명:
        ///   Image.color → "m_Color"
        ///
        ///   정확한 필드명 확인 방법:
        ///   Sindy/Tools/Field Peeker Window → 컴포넌트 드래그 → 경로 확인
        ///   또는: FieldPeeker.Print&lt;Image&gt;(gameObject);
        /// </summary>
        private static void EditGaugePrefab(string prefabPath)
        {
            // Dispose 시 SaveAsPrefabAsset + UnloadPrefabContents 자동 실행.
            using (var p = PrefabEditor.Open(prefabPath))
            {
                if (p == null) return;

                // ── 루트 GO의 GaugeComponent 편집 ────────────────────────────
                // p.Root(): PrefabEditor.RootObject에 대한 GOEditor 단축 표현.
                p.Root()
                    .WithComp<GaugeComponent>()
                    .Apply();

                // ── GOFind: 존재하는 자식 GO 탐색 (없으면 null + LogWarning) ──
                // fill 자식은 프리팹에 이미 있어야 하므로 GOFind 사용.
                p.GOFind("Fill")
                    ?.WithComp<Image>()
                    .SOColor("m_Color", new Color(0.9f, 0.25f, 0.25f)) // 빨간색
                    .Apply();

                // ── GO: 없으면 자동 생성 (새 자식 추가 의도일 때 사용) ─────────
                p.GO("Background.Border")
                    .WithComp<Image>()
                    .SOColor("m_Color", new Color(0.3f, 0.3f, 0.3f))
                    .Apply();
            }
        }

        // ─── LabelComponent 프리팹 편집 ─────────────────────────────────────

        /// <summary>
        /// LabelComponent 프리팹 내부의 TMP_Text 필드를 편집하고,
        /// 새 자식 GO를 생성하여 컴포넌트를 추가한다.
        ///
        /// ⚠ TMP 내부 직렬화 필드명:
        ///   text     → "m_text"
        ///   fontSize → "m_fontSize"
        ///   color    → "m_fontColor"
        /// </summary>
        private static void EditLabelPrefab(string prefabPath)
        {
            using (var p = PrefabEditor.Open(prefabPath))
            {
                if (p == null) return;

                // ── 루트 GO의 LabelComponent 편집 ────────────────────────────
                p.Root()
                    .WithComp<LabelComponent>()
                    .Apply();

                // ── 자식 "Label" GO의 TMP_Text 편집 (GOFind: 없으면 경고) ─────
                p.GOFind("Label")
                    ?.WithComp<TextMeshProUGUI>()
                    .SOStr("m_text",        "Default Label")
                    .SOFloat("m_fontSize",   18f)
                    .SOColor("m_fontColor", Color.white)
                    .Apply();

                // ── 새 자식 GO 생성 + 컴포넌트 추가 (GO: 없으면 자동 생성) ───
                p.GO("Overlay")
                    .AddComp<Image>()
                    .SOColor("m_Color", new Color(1f, 1f, 1f, 0.05f))
                    .Apply();

                // ── Child(): 보관한 GO 기준으로 상대 경로 탐색/생성 ───────────
                // GO()가 항상 루트 기준인 것과 달리, Child()는 현재 노드 기준.
                var root = p.Root();

                root.ChildFind("Label")
                    ?.WithComp<TextMeshProUGUI>()
                    .Apply();

                root.Child("Overlay")
                    .WithComp<Image>()
                    .Apply();
            }
        }

        // ─── 여러 프리팹 일괄 편집 ────────────────────────────────────────────

        [MenuItem("Sindy/Examples/B - Prefab Batch Edit")]
        public static void RunBatchEdit()
        {
            var allGaugePrefabs = AssetFinder.AllPrefabs<GaugeComponent>();

            foreach (var go in allGaugePrefabs)
            {
                string path = AssetDatabase.GetAssetPath(go);

                using (var p = PrefabEditor.Open(path))
                {
                    if (p == null) continue;

                    p.GOFind("Fill")
                        ?.WithComp<Image>()
                        .SOColor("m_Color", new Color(0.2f, 0.8f, 0.4f)) // 초록색으로 통일
                        .Apply();
                }
            }

            Debug.Log($"[Example B] GaugeComponent 프리팹 {allGaugePrefabs.Count}개 색상 통일 완료.");
        }
    }
}
#endif
