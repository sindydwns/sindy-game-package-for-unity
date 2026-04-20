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
//   ✅ GOFind + WithComp<T>(Action) 패턴 → SindyEdit으로 완전 변환
//   ✅ 일괄 편집(BatchEdit) → SindyEdit으로 완전 변환
//   ❌ GO() 생성 패턴 → AssetEditSession.GO()는 탐색 전용이므로 PrefabEditor 직접 사용 필요
//   ❌ AddComp<T>() 후 SOColor 체이닝 + Apply() → GOEditor 체인 패턴은 PrefabEditor 직접 사용 필요
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
        ///   - WithComp&lt;Image&gt;(callback): 콜백에서 Set()으로 프로퍼티 편집 후 자동 Apply
        ///   - Root(): 프리팹 루트 GO 접근
        ///
        /// ❌ 변환 불가: GO("Background.Border")
        ///   AssetEditSession.GO()는 탐색 전용입니다.
        ///   GO가 없을 때 신규 생성이 필요한 경우 PrefabEditor.Open()을 직접 사용하세요.
        ///
        /// ⚠ Unity 내부 직렬화 필드명:
        ///   Image.color → "m_Color"
        /// </summary>
        private static void EditGaugePrefab(string prefabPath)
        {
            // SindyEdit.Open()으로 프리팹을 열면 Dispose 시 자동 저장됩니다.
            using var s = SindyEdit.Open(prefabPath);
            if (s == null) return;

            // ── Root(): 프리팹 루트 GO 접근 + GetComp<T>()로 컴포넌트 유무 확인 ──
            var gc = s.Root().GetComp<GaugeComponent>();
            if (gc != null)
                Debug.Log($"[Example B] GaugeComponent 확인됨: {gc.name}");

            // ── GOFind: 이름으로 재귀 탐색 (계층 깊이에 무관하게 찾음) ───────
            // ?.에 해당하는 null 안전 처리는 GOFind 내부에서 LogWarning으로 대체됩니다.
            s.GOFind("Fill").WithComp<Image>(img =>
                img.Set("m_Color", new Color(0.9f, 0.25f, 0.25f)));  // 빨간색

            // ── 변환 불가 케이스 ──────────────────────────────────────────────
            // 아래 패턴은 "Background.Border"가 없으면 새로 생성하는 의도입니다.
            // AssetEditSession.GO()는 탐색만 하므로, 생성이 필요하면 아래처럼 PrefabEditor를 사용하세요:
            //
            // using (var p = PrefabEditor.Open(prefabPath))
            // {
            //     p.GO("Background.Border")
            //         .AddComp<Image>()
            //         .SOColor("m_Color", new Color(0.3f, 0.3f, 0.3f))
            //         .Apply();
            // }
        }

        // ─── LabelComponent 프리팹 편집 ─────────────────────────────────────

        /// <summary>
        /// LabelComponent 프리팹 내부의 TMP_Text 필드를 편집합니다.
        ///
        /// ✅ SindyEdit 변환:
        ///   - GOFind("Label"): 재귀 탐색
        ///   - WithComp&lt;TextMeshProUGUI&gt;(callback): 여러 프로퍼티를 콜백 안에서 Set()으로 설정
        ///   - Root().Child("Label"): Root 기준 직계 자식 탐색
        ///
        /// ❌ 변환 불가: GO("Overlay").AddComp&lt;Image&gt;()
        ///   신규 GO 생성 + AddComp + SOColor + Apply 체이닝은 PrefabEditor.GO()를 사용하세요.
        ///
        /// ⚠ TMP 내부 직렬화 필드명:
        ///   text → "m_text" / fontSize → "m_fontSize" / color → "m_fontColor"
        /// </summary>
        private static void EditLabelPrefab(string prefabPath)
        {
            using var s = SindyEdit.Open(prefabPath);
            if (s == null) return;

            // ── GOFind: 계층 전체에서 "Label" 재귀 탐색 후 TMP 편집 ───────────
            s.GOFind("Label").WithComp<TextMeshProUGUI>(tmp =>
            {
                tmp.Set("m_text",      "Default Label");
                tmp.Set("m_fontSize",  18f);
                tmp.Set("m_fontColor", Color.white);
            });

            // ── 변환 불가: "Overlay" 신규 생성 + AddComp<Image> ──────────────
            // 아래처럼 PrefabEditor를 직접 사용하세요:
            //
            // using (var p = PrefabEditor.Open(prefabPath))
            // {
            //     p.GO("Overlay").AddComp<Image>().SOColor("m_Color", new Color(1f, 1f, 1f, 0.05f)).Apply();
            // }

            // ── Child(): Root 기준 직계 자식 탐색 ────────────────────────────
            // GOFind와 달리 직계 자식만 탐색합니다 (재귀 없음).
            // Root() 후 Child()로 이동; 다른 GO로 이동하려면 Root()를 다시 호출합니다.
            s.Root().Child("Label").WithComp<TextMeshProUGUI>(tmp => {});

            // Root()로 위치를 프리팹 루트로 리셋한 뒤 다른 자식으로 이동
            s.Root().Child("Overlay"); // Overlay가 이미 존재하는 경우에만 _currentGO가 설정됩니다.
        }

        // ─── 여러 프리팹 일괄 편집 ────────────────────────────────────────────

        /// <summary>
        /// ✅ SindyEdit으로 완전 변환: GOFind + WithComp 패턴만 사용하므로 변환 가능
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
                s.GOFind("Fill").WithComp<Image>(img =>
                    img.Set("m_Color", new Color(0.2f, 0.8f, 0.4f)));  // 초록색으로 통일
            }

            Debug.Log($"[Example B] GaugeComponent 프리팹 {allGaugePrefabs.Count}개 색상 통일 완료.");
        }
    }
}
#endif
