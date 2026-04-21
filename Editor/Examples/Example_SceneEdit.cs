// ────────────────────────────────────────────────────────────────────────────
// 예제 A — 씬 편집 (SceneEditor / SindyEdit / GOEditor / AssetFinder)
//
// 구현 파일: Editor/EditorTools/SindyEdit.cs, SceneEditor.cs, GOEditor.cs, AssetFinder.cs
//
// 메서드별 구현 방식:
//   SetupShowcaseRunner : SceneEditor/GOEditor 직접 사용 (AddComp(string) 필요 — 어셈블리 경계 우회)
//   SetupHUD            : SceneEditor/GOEditor 직접 사용 (GO 탐색+생성 패턴)
//   CreateGOWithSindyEdit: SindyEdit — CreateGameObject + AddComponent + SetProperty/SetRef 시연
//   ReadWithSindyEdit   : SindyEdit — FindGameObject / Root / Child / GetComponent / 값 읽기 시연
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using Sindy.Editor.EditorTools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Sindy.Editor.Examples
{
    /// <summary>
    /// 예제 A — SceneEditor, GOEditor, AssetFinder 사용법
    ///
    /// 시나리오:
    ///   _test_component_builder_quick 씬에
    ///   (1) ShowcaseRunner GO에 컴포넌트를 추가하고 AssetFinder로 프리팹 슬롯을 채운다.
    ///   (2) Canvas.HUD.* 계층을 생성하여 TMP / Image 컴포넌트를 설정한다.
    ///   (3) (SindyEdit) 씬의 기존 GO를 GOFind/Root/Child로 탐색하고 값을 읽는다.
    ///
    /// Menu: Sindy/Examples/A - Scene Edit
    /// </summary>
    public static class Example_SceneEdit
    {
        // ─── A-1: SceneEditor/GOEditor 직접 사용 ────────────────────────────
        // SetupShowcaseRunner: AddComp(string typeFullName) — 어셈블리 경계 우회 필요
        // SetupHUD: GO 탐색+생성 패턴. SindyEdit 버전은 CreateGOWithSindyEdit() 참고.
        // 위 패턴이 필요한 경우 SceneEditor / GOEditor를 직접 사용하세요.

        [MenuItem("Sindy/Examples/A - Scene Edit")]
        public static void Run()
        {
            var scenePath = PackagePathHelper.Resolve(
                "Tests/Runtime/ComponentBuilderTest/_test_component_builder_quick.unity");

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError(
                    $"[Example A] 씬을 찾을 수 없습니다: {scenePath}\n" +
                    "UPM 설치 방식(Git/Local/Embedded)에 따라 경로가 다를 수 있습니다. " +
                    "PackagePathHelper.Resolve()를 확인하세요.");
                return;
            }

            // ── 1. 씬 열기 ──────────────────────────────────────────────────
            // 이미 열린 씬이면 재사용. 미저장 변경사항이 있으면 사용자에게 묻는다.
            // 취소 또는 실패 시 null 반환 → null 체크 필수.
            using (var ctx = SceneEditor.Open(scenePath))
            {
                if (ctx == null) return;

                AssetFinder.ClearCache();

                SetupShowcaseRunner(ctx);
                SetupHUD(ctx);

                ctx.MarkDirty(); // 변경사항이 있을 때만 호출. 미호출 시 씬 저장 안 됨.
            }
            // Dispose → MarkDirty()가 호출된 경우 EditorSceneManager.SaveScene 자동 실행.
        }

        // ─── (1) ShowcaseRunner GO 설정 ───────────────────────────────────────

        /// <summary>
        /// "ShowcaseRunner" GO를 찾거나 만들고, 컴포넌트를 추가한 뒤 AssetFinder로
        /// 각 프리팹 슬롯을 채운다.
        ///
        /// SceneEditor/GOEditor 직접 사용:
        ///   ShowcaseRunner는 Sindy.Tests.Runtime 어셈블리에 있으므로
        ///   AddComp(string typeFullName) 오버로드로 어셈블리 경계를 우회한다.
        ///   SindyEdit(AssetEditSession)은 이 오버로드를 지원하지 않으므로
        ///   이 메서드는 SceneEditor/GOEditor 패턴으로 유지한다.
        /// </summary>
        private static void SetupShowcaseRunner(SceneEditor ctx)
        {
            // ── AssetFinder: 타입 전체 이름 + 힌트로 프리팹 컴포넌트 탐색 ──
            var label = AssetFinder.Prefab("Sindy.View.Components.LabelComponent", "label");
            var button = AssetFinder.Prefab("Sindy.View.Components.ButtonComponent", "button");
            var gauge = AssetFinder.Prefab("Sindy.View.Components.GaugeComponent", "gauge");
            var toggle = AssetFinder.Prefab("Sindy.View.Components.ToggleComponent", "toggle");
            var list = AssetFinder.Prefab("Sindy.View.Components.ListComponent", "list");
            var page = AssetFinder.Prefab("Sindy.View.Components.PageComponent", "page");
            var tab = AssetFinder.Prefab("Sindy.View.Components.TabComponent", "tab");
            var popup = AssetFinder.Prefab("Sindy.View.Components.PopupComponent", "popup");

            // ── GOEditor 체인 ─────────────────────────────────────────────────
            // GO("ShowcaseRunner") : 씬 루트에서 "ShowcaseRunner" 탐색, 없으면 생성
            // AddComp(typeFullName): 어셈블리 경계를 넘어 컴포넌트를 추가할 때 사용
            // SetRef(path, value)   : objectReferenceValue 설정
            // Apply()              : ApplyModifiedProperties + SetDirty. 반드시 호출!
            ctx.GetGameObject("ShowcaseRunner")
                .AddComp("Sindy.Test.ShowcaseRunner") // 문자열 오버로드로 어셈블리 경계 우회
                .SetRef("labelPrefab", label, ignoreNullWarning: true)
                .SetRef("buttonPrefab", button, ignoreNullWarning: true)
                .SetRef("gaugePrefab", gauge, ignoreNullWarning: true)
                .SetRef("togglePrefab", toggle, ignoreNullWarning: true)
                .SetRef("listPrefab", list, ignoreNullWarning: true)
                .SetRef("pagePrefab", page, ignoreNullWarning: true)
                .SetRef("tabPrefab", tab, ignoreNullWarning: true)
                .SetRef("popupPrefab", popup, ignoreNullWarning: true)
                .SetFloat("cellWidth", 240f)
                .SetFloat("cellHeight", 200f)
                .SetInt("gridColumns", 3)
                .SetFloat("cycleSec", 3.0f)
                .SetColor("bgColor", new Color(0.12f, 0.12f, 0.15f))
                .SetColor("cellColor", new Color(0.20f, 0.20f, 0.26f))
                .Apply();
        }

        // ─── (2) Canvas.HUD.* 계층 생성 ──────────────────────────────────────

        /// <summary>
        /// Canvas → HUD → Title / Background / Footer.VersionLabel 계층을 자동 생성한다.
        ///
        /// SindyEdit 버전은 CreateGOWithSindyEdit()을 참고하세요.
        /// (CreateGameObject + AddComponent + SetProperty 패턴)
        ///
        /// Unity 내부 직렬화 필드명 (m_ 접두사):
        ///   TextMeshProUGUI.text     → "m_text"
        ///   TextMeshProUGUI.fontSize → "m_fontSize"
        ///   TextMeshProUGUI.color    → "m_fontColor"
        ///   Image.color              → "m_Color"
        ///
        ///   정확한 필드명을 모를 때: Sindy/Tools/Field Peeker Window
        ///   또는 코드에서: FieldPeeker.Print&lt;TextMeshProUGUI&gt;(gameObject)
        /// </summary>
        private static void SetupHUD(SceneEditor ctx)
        {
            // ── Canvas.HUD.Title ──────────────────────────────────────────────
            ctx.GetGameObject("Canvas.HUD.Title")
                .AddComp<TextMeshProUGUI>()
                .SetStr("m_text", "ComponentBuilder Showcase")
                .SetFloat("m_fontSize", 28f)
                .SetColor("m_fontColor", new Color(0.5f, 1f, 0.9f))
                .Apply();

            // ── Canvas.HUD.Background ────────────────────────────────────────
            ctx.GetGameObject("Canvas.HUD.Background")
                .AddComp<Image>()
                .SetColor("m_Color", new Color(0f, 0f, 0f, 0.6f))
                .Apply();

            // ── Canvas.HUD.Footer.VersionLabel ────────────────────────────────
            ctx.GetGameObject("Canvas.HUD.Footer.VersionLabel")
                .AddComp<TextMeshProUGUI>()
                .SetStr("m_text", "v1.0.0")
                .SetFloat("m_fontSize", 11f)
                .SetColor("m_fontColor", new Color(0.55f, 0.55f, 0.55f))
                .Apply();

            // ── Canvas.HUD.Footer.QuitButton ─────────────────────────────────
            // GOEditor: AddComp → EditComp 전환으로 같은 GO에 두 컴포넌트를 순서대로 설정.
            var quitGO = ctx.GetGameObject("Canvas.HUD.Footer.QuitButton");

            quitGO.AddComp<Image>()
                  .SetColor("m_Color", new Color(0.7f, 0.2f, 0.2f))
                  .Apply();

            quitGO.AddComp<Button>()
                  .Apply();

            // ── Child(): 변수로 보관한 GO 기준으로 상대 경로 탐색/생성 ────────
            // GO()는 항상 씬 루트 기준. Child()는 현재 노드 기준.
            var hud = ctx.GetGameObject("Canvas.HUD");

            hud.Child("InfoPanel.Row1")
               .AddComp<TextMeshProUGUI>()
               .SetStr("m_text", "Row 1 정보")
               .Apply();

            hud.Child("InfoPanel.Row2")
               .AddComp<TextMeshProUGUI>()
               .SetStr("m_text", "Row 2 정보")
               .Apply();
        }

        // ─── A-2: SindyEdit CreateGameObject + SetRef 시연 ──────────────────────

        /// <summary>
        /// SindyEdit으로 GO 계층을 생성하고 컴포넌트를 설정하는 예시 (SetupHUD의 SindyEdit 버전).
        ///
        ///   - CreateGameObject(): _currentGO가 null이면 씬 루트에, non-null이면 자식으로 GO 생성
        ///   - 체이닝: GO() 탐색 → CreateGameObject() → AddComponent() → SetProperty() 가능
        ///     AddComponent&lt;T&gt;()는 ComponentScope를 반환하므로 이후 SetProperty로 체이닝
        ///   - SetRef(): SerializedProperty objectReferenceValue 세터 (세션 레벨)
        ///   - ComponentScope.SetRef(): GetComponent 콜백 안에서 objectReferenceValue 설정
        /// </summary>
        [MenuItem("Sindy/Examples/A - Scene Edit (CreateGO + SetRef)")]
        public static void CreateGOWithSindyEdit()
        {
            var scenePath = PackagePathHelper.Resolve(
                "Tests/Runtime/ComponentBuilderTest/_test_component_builder_quick.unity");

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[Example A] 씬을 찾을 수 없습니다: {scenePath}");
                return;
            }

            using var s = SindyEdit.Open(scenePath);
            if (s == null) return;

            // ── CreateGameObject: _currentGO null → 씬 루트에 생성 ───────────────
            // GO()로 탐색하지 않은 상태(null)에서 CreateGameObject를 호출하면 씬 루트에 배치됩니다.
            s.CreateGameObject("Canvas");

            // ── 체이닝: GO 탐색 후 자식 GO 생성 + AddComponent + SetProperty ──────
            // GO()로 기존 GO를 찾고, CreateGameObject()로 자식을 생성하면 _currentGO가 새 GO로 이동합니다.
            // AddComponent<T>()는 ComponentScope를 반환하므로 SetProperty로 이어서 체이닝합니다.
            s.GO("Canvas").CreateGameObject("HUD").CreateGameObject("Title")
                .AddComponent<TextMeshProUGUI>()
                .SetProperty("m_text", "ComponentBuilder Showcase")
                .SetProperty("m_fontSize", 28f)
                .SetProperty("m_fontColor", new Color(0.5f, 1f, 0.9f));

            s.GO("Canvas/HUD").CreateGameObject("Background")
                .AddComponent<Image>()
                .SetProperty("m_Color", new Color(0f, 0f, 0f, 0.6f));

            s.GO("Canvas/HUD").CreateGameObject("Footer");

            s.GO("Canvas/HUD/Footer").CreateGameObject("VersionLabel")
                .AddComponent<TextMeshProUGUI>()
                .SetProperty("m_text", "v1.0.0")
                .SetProperty("m_fontSize", 11f)
                .SetProperty("m_fontColor", new Color(0.55f, 0.55f, 0.55f));

            // ── SetRef: GetComponent 콜백 안에서 objectReferenceValue 설정 ────────
            // 세션 레벨:  s.FindGameObject("Icon").SetRef("m_Sprite", spriteAsset);
            // ComponentScope 레벨:
            // s.FindGameObject("Icon").GetComponent<Image>(img => img.SetRef("m_Sprite", mySprite));

            // Dispose 시 변경사항이 있으면 자동 저장됩니다.
        }

        // ─── A-4: SindyEdit API 시연 ─────────────────────────────────────────

        /// <summary>
        /// SindyEdit API 사용 시연:
        ///   - SindyEdit.Open()으로 씬 열기
        ///   - FindGameObject(): 계층 어디에 있든 이름으로 재귀 탐색
        ///   - Root(): 씬 첫 번째 루트 GO를 가리키는 새 세션 반환
        ///   - Child(): 직계 자식 인덱스 / 이름으로 새 세션 반환
        ///   - GetFloat() / GetString() 등 값 읽기
        ///   - GetComponent&lt;T&gt;(Action): 콜백에서 특정 컴포넌트 편집
        ///
        /// 이 메서드는 씬에 "A - Scene Edit" 메뉴로 이미 생성된 HUD 계층이 있다고 가정합니다.
        /// </summary>
        [MenuItem("Sindy/Examples/A - Scene Edit (SindyEdit 탐색 및 읽기)")]
        public static void ReadWithSindyEdit()
        {
            var scenePath = PackagePathHelper.Resolve(
                "Tests/Runtime/ComponentBuilderTest/_test_component_builder_quick.unity");

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogError($"[Example A] 씬을 찾을 수 없습니다: {scenePath}");
                return;
            }

            // Dispose 시 변경사항이 있으면 자동 저장됩니다.
            using var s = SindyEdit.Open(scenePath);
            if (s == null) return;

            // ── FindGameObject: 계층 전체를 재귀 탐색 (경로 없이 이름만으로 찾기) ─
            float titleFontSize = s.FindGameObject("Title").GetFloat("m_fontSize");
            string versionText = s.FindGameObject("VersionLabel").GetString("m_text");
            Debug.Log($"[Example A] Title fontSize: {titleFontSize}, VersionLabel: \"{versionText}\"");

            // ── GetComponent<T>: 콜백 방식으로 특정 컴포넌트 편집 ──────────────
            // SetProperty() 호출 시 즉시 ApplyModifiedPropertiesWithoutUndo() 실행.
            s.FindGameObject("Title").GetComponent<TextMeshProUGUI>(tmp =>
                tmp.SetProperty("m_fontColor", new Color(1f, 0.9f, 0.5f)));

            // ── Root(): 씬 첫 번째 루트 GO를 가리키는 새 세션 반환 ────────────
            // FP 스타일: Root()는 새 세션을 반환 — 반환값을 변수에 받아야 합니다.
            var root = s.Root();
            Debug.Log($"[Example A] 첫 번째 루트 GO Transform 있음: {root.HasComponent<Transform>()}");

            // ── Child(string): 직계 자식을 이름으로 탐색 — 새 세션 반환 ─────────
            // GO()가 씬 루트 기준 경로 탐색이라면, Child()는 현재 GO 기준 직계 자식 탐색입니다.
            s.GO("Canvas").Child("HUD").Child("Title").GetComponent<TextMeshProUGUI>(tmp =>
                tmp.SetProperty("m_text", "SindyEdit으로 수정됨"));

            // ── Child(int): 인덱스로 직계 자식 접근 — 새 세션 반환 ──────────────
            // FP 스타일: 체인 결과를 변수로 받아야 합니다 — s 자체는 변경되지 않습니다.
            var firstHudChild = s.GO("Canvas").Child("HUD").Child(0);
            if (firstHudChild.HasComponent<Transform>())
                Debug.Log("[Example A] HUD 첫 번째 자식 확인됨");

            // ── AddComponent<T>: 현재 GO에 컴포넌트 추가 — ComponentScope 반환 ──
            s.GO("Canvas").AddComponent<CanvasGroup>();

            // ── GetComponent<T>: ComponentScope로 프로퍼티 접근 ───────────────
            var cgScope = s.GO("Canvas").GetComponent<CanvasGroup>();
            if (cgScope != null)
                Debug.Log($"[Example A] CanvasGroup alpha: {cgScope.GetFloat("m_Alpha")}");
        }
    }
}
#endif
