// ────────────────────────────────────────────────────────────────────────────
// 예제 B — 프리팹 편집 (SindyEdit / PrefabEditor / GOEditor / AssetFinder)
//
// 구현 파일: Editor/EditorTools/SindyEdit.cs, PrefabEditor.cs, GOEditor.cs, AssetFinder.cs
//
// 전제: 아래 경로에 편집 대상 프리팹이 존재해야 합니다.
//   GaugePrefabPath — GaugeComponent가 붙은 프리팹
//   LabelPrefabPath — LabelComponent가 붙은 프리팹
//
// SindyEdit 변환 현황:
//   ✅ GOFind + GetComp<T>(Action) 패턴 → SindyEdit으로 완전 변환
//   ✅ 일괄 편집(BatchEdit) → SindyEdit으로 완전 변환
//   ✅ GO() 생성 패턴 → CreateGO API 추가로 SindyEdit 변환 완료
//   ✅ AddComp<T>() 후 SOColor 체이닝 → CreateGO + AddComp + SOColor 패턴으로 SindyEdit 변환 완료
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
    /// 예제 B — SindyEdit, PrefabEditor, GOEditor 사용법
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
        /// ✅ SindyEdit 변환:
        ///   - GOFind("Fill"): 계층 어디에 있든 이름으로 재귀 탐색
        ///   - GetComp<Image>(callback): 콜백에서 Set()으로 프로퍼티 편집 후 즉시 Apply
        ///   - Root(): 프리팹 루트 GO 접근
        ///
        /// ✅ CreateGO로 변환 가능: GO("Background.Border")
        ///   GOFind("Background").CreateGO("Border")로 새 GO를 생성하고 AddComp + SOColor 체이닝 가능합니다.
        ///
        /// ⚠ Unity 내부 직렬화 필드명:
        ///   Image.color → "m_Color"
        /// </summary>
        private static void EditGaugePrefab(string prefabPath)
        {
            // SindyEdit.Open()으로 프리팹을 열면 Dispose 시 자동 저장됩니다.
            using var s = SindyEdit.Open(prefabPath);
            if (s == null) return;

            // ── Root(): 프리팹 루트 GO 접근 + HasComp<T>()로 컴포넌트 유무 확인 ──
            if (s.Root().HasComponent<GaugeComponent>())
                Debug.Log("[Example B] GaugeComponent 확인됨");

            // ── GOFind: 이름으로 재귀 탐색 (계층 깊이에 무관하게 찾음) ───────
            // ?.에 해당하는 null 안전 처리는 GOFind 내부에서 LogWarning으로 대체됩니다.
            s.FindGameObject("Fill").GetComponent<Image>(img =>
                img.SetProperty("m_Color", new Color(0.9f, 0.25f, 0.25f)));  // 빨간색

            // ── CreateGO: "Background" 아래에 "Border" 신규 생성 ─────────────────────────
            // GOFind로 기존 GO를 찾은 뒤 CreateGO로 자식을 생성합니다.
            // 생성 후 _currentGO가 새 GO로 이동하므로 AddComp + SOColor 체이닝이 바로 가능합니다.
            s.FindGameObject("Background").CreateGameObject("Border")
                .AddComponent<Image>()
                .SetProperty("m_Color", new Color(0.3f, 0.3f, 0.3f));
        }

        // ─── LabelComponent 프리팹 편집 ─────────────────────────────────────

        /// <summary>
        /// LabelComponent 프리팹 내부의 TMP_Text 필드를 편집합니다.
        ///
        /// ✅ SindyEdit 변환:
        ///   - GOFind("Label"): 재귀 탐색
        ///   - GetComp<TextMeshProUGUI>(callback): 여러 프로퍼티를 콜백 안에서 Set()으로 설정
        ///   - Root().Child("Label"): Root 기준 직계 자식 탐색
        ///
        /// ✅ CreateGO로 변환 가능: GO("Overlay").AddComp<Image>()
        ///   Root().CreateGO("Overlay")로 프리팹 루트 자식으로 생성하고 AddComp + SOColor 체이닝 가능합니다.
        ///
        /// ⚠ TMP 내부 직렬화 필드명:
        ///   text → "m_text" / fontSize → "m_fontSize" / color → "m_fontColor"
        /// </summary>
        private static void EditLabelPrefab(string prefabPath)
        {
            using var s = SindyEdit.Open(prefabPath);
            if (s == null) return;

            // ── GOFind: 계층 전체에서 "Label" 재귀 탐색 후 TMP 편집 ───────────
            s.FindGameObject("Label").GetComponent<TextMeshProUGUI>(tmp =>
            {
                tmp.SetProperty("m_text", "Default Label");
                tmp.SetProperty("m_fontSize", 18f);
                tmp.SetProperty("m_fontColor", Color.white);
            });

            // ── CreateGO: 프리팹 루트에 "Overlay" 신규 생성 ──────────────────────────────
            // Root()로 프리팹 루트를 _currentGO로 설정한 뒤 CreateGO()로 자식을 생성합니다.
            s.Root().CreateGameObject("Overlay")
                .AddComponent<Image>()
                .SetProperty("m_Color", new Color(1f, 1f, 1f, 0.05f));

            // ── Child(): Root 기준 직계 자식 탐색 ────────────────────────────
            // GOFind와 달리 직계 자식만 탐색합니다 (재귀 없음).
            // FP 스타일: Root()·Child()는 각각 새 세션을 반환 — s 자체는 변경되지 않습니다.
            s.Root().Child("Label").GetComponent<TextMeshProUGUI>(tmp => { });

            // 다른 자식 세션이 필요하면 반환값을 변수로 받습니다.
            var overlay = s.Root().Child("Overlay"); // Overlay가 있으면 해당 GO 세션, 없으면 null GO 세션
        }

        // ─── 여러 프리팹 일괄 편집 ────────────────────────────────────────────

        /// <summary>
        /// ✅ SindyEdit으로 완전 변환: GOFind + GetComp 패턴만 사용하므로 변환 가능
        /// </summary>
        [MenuItem("Sindy/Examples/B - Prefab Batch Edit")]
        public static void RunBatchEdit()
        {
            var allGaugePrefabs = AssetFinder.AllPrefabs<GaugeComponent>();

            foreach (var go in allGaugePrefabs)
            {
                string path = AssetDatabase.GetAssetPath(go);

                // SindyEdit.Open()으로 열면 Dispose 시 자동 저장됩니다.
                using var s = SindyEdit.Open(path);
                if (s == null) continue;

                // GOFind: 계층 어디에 있든 "Fill" GO를 찾아 색상 변경
                s.FindGameObject("Fill").GetComponent<Image>(img =>
                    img.SetProperty("m_Color", new Color(0.2f, 0.8f, 0.4f)));  // 초록색으로 통일
            }

            Debug.Log($"[Example B] GaugeComponent 프리팹 {allGaugePrefabs.Count}개 색상 통일 완료.");
        }
    }
}
#endif
