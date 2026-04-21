// ────────────────────────────────────────────────────────────────────────────
// 예제 A — 씬 편집 (SceneEditor / SindyEdit / GOEditor / AssetFinder)
//
// 구현 파일: Editor/EditorTools/SindyEdit.cs, SceneEditor.cs, GOEditor.cs, AssetFinder.cs
//
// SindyEdit 변환 현황:
//   ❌ SetupShowcaseRunner: AddComp(string) 미지원으로 변환 불가 (SORef는 이제 지원됨)
//   ✅ CreateGOWithSindyEdit: CreateGO + SORef 신규 API 시연 메서드 추가 (SetupHUD의 SindyEdit 변환 버전)
//   ✅ ReadWithSindyEdit: 신규 API(GOFind, Root, Child, 값 읽기) 시연 전용 메서드 추가
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
        // ─── A-1: SceneEditor 기반 씬 편집 ───────────────────────────────────
        // SetupShowcaseRunner, SetupHUD 모두 GO 신규 생성 / SORef / AddComp(string)을
        // 사용하므로 AssetEditSession으로 변환할 수 없습니다.
        // 이 API들이 필요한 경우 SceneEditor / GOEditor를 직접 사용하세요.

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
        /// ❌ 변환 불가 이유:
        ///   - AddComp(string typeFullName): AssetEditSession 미지원 (이 메서드는 SceneEditor 전용 예제로 유지)
        ///   (SORef는 AssetEditSession에 추가되었습니다)
        ///
        /// ⚠ 어셈블리 경계:
        ///   ShowcaseRunner는 Sindy.Tests.Runtime 어셈블리에 있으므로
        ///   이 파일(Sindy.Editor)에서 AddComp<ShowcaseRunner>()를 직접 쓸 수 없다.
        ///   대신 타입 전체 이름 문자열을 전달하는 AddComp(string) 오버로드를 사용한다.
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
            // SORef(path, value)   : objectReferenceValue 설정
            // Apply()              : ApplyModifiedProperties + SetDirty. 반드시 호출!
            ctx.GO("ShowcaseRunner")
                .AddComp("Sindy.Test.ShowcaseRunner") // 문자열 오버로드로 어셈블리 경계 우회
                .SORef("labelPrefab", label, ignoreNullWarning: true)
                .SORef("buttonPrefab", button, ignoreNullWarning: true)
                .SORef("gaugePrefab", gauge, ignoreNullWarning: true)
                .SORef("togglePrefab", toggle, ignoreNullWarning: true)
                .SORef("listPrefab", list, ignoreNullWarning: true)
                .SORef("pagePrefab", page, ignoreNullWarning: true)
                .SORef("tabPrefab", tab, ignoreNullWarning: true)
                .SORef("popupPrefab", popup, ignoreNullWarning: true)
                .SOFloat("cellWidth", 240f)
                .SOFloat("cellHeight", 200f)
                .SOInt("gridColumns", 3)
                .SOFloat("cycleSec", 3.0f)
                .SOColor("bgColor", new Color(0.12f, 0.12f, 0.15f))
                .SOColor("cellColor", new Color(0.20f, 0.20f, 0.26f))
                .Apply();
        }

        // ─── (2) Canvas.HUD.* 계층 생성 ──────────────────────────────────────

        /// <summary>
        /// Canvas → HUD → Title / Background / Footer.VersionLabel 계층을 자동 생성한다.
        ///
        /// ✅ SindyEdit 변환 가능:
        ///   - CreateGO() 추가로 AssetEditSession에서도 GO 신규 생성이 가능합니다.
        ///   - CreateGOWithSindyEdit() 메서드에서 SindyEdit 변환 버전을 확인하세요.
        ///
        /// ⚠ Unity 내부 직렬화 필드명 (m_ 접두사):
        ///   TextMeshProUGUI.text     → "m_text"
        ///   TextMeshProUGUI.fontSize → "m_fontSize"
        ///   TextMeshProUGUI.color    → "m_fontColor"
        ///   Image.color              → "m_Color"
        ///
        ///   정확한 필드명을 모를 때: Sindy/Tools/Field Peeker Window (FieldPeeker 도구)
        ///   또는 코드에서: FieldPeeker.Print<TextMeshProUGUI>(gameObject);
        /// </summary>
        private static void SetupHUD(SceneEditor ctx)
        {
            // ── Canvas.HUD.Title ──────────────────────────────────────────────
            ctx.GO("Canvas.HUD.Title")
                .AddComp<TextMeshProUGUI>()
                .SOStr("m_text", "ComponentBuilder Showcase")
                .SOFloat("m_fontSize", 28f)
                .SOColor("m_fontColor", new Color(0.5f, 1f, 0.9f))
                .Apply();

            // ── Canvas.HUD.Background ────────────────────────────────────────
            ctx.GO("Canvas.HUD.Background")
                .AddComp<Image>()
                .SOColor("m_Color", new Color(0f, 0f, 0f, 0.6f))
                .Apply();

            // ── Canvas.HUD.Footer.VersionLabel ────────────────────────────────
            ctx.GO("Canvas.HUD.Footer.VersionLabel")
                .AddComp<TextMeshProUGUI>()
                .SOStr("m_text", "v1.0.0")
                .SOFloat("m_fontSize", 11f)
                .SOColor("m_fontColor", new Color(0.55f, 0.55f, 0.55f))
                .Apply();

            // ── Canvas.HUD.Footer.QuitButton ─────────────────────────────────
            // 같은 GO에 AddComp → WithComp 전환으로 두 컴포넌트를 순서대로 설정.
            var quitGO = ctx.GO("Canvas.HUD.Footer.QuitButton");

            quitGO.AddComp<Image>()
                  .SOColor("m_Color", new Color(0.7f, 0.2f, 0.2f))
                  .Apply();

            quitGO.AddComp<Button>()
                  .Apply();

            // ── Child(): 변수로 보관한 GO 기준으로 상대 경로 탐색/생성 ────────
            // GO()는 항상 씬 루트 기준. Child()는 현재 노드 기준.
            var hud = ctx.GO("Canvas.HUD");

            hud.Child("InfoPanel.Row1")
               .AddComp<TextMeshProUGUI>()
               .SOStr("m_text", "Row 1 정보")
               .Apply();

            hud.Child("InfoPanel.Row2")
               .AddComp<TextMeshProUGUI>()
               .SOStr("m_text", "Row 2 정보")
               .Apply();
        }

        // ─── A-2: SindyEdit CreateGO + SORef 시연 ───────────────────────────────

        /// <summary>
        /// ✅ CreateGO + SORef 신규 API 시연 (SetupHUD의 SindyEdit 변환 버전):
        ///   - CreateGO(): _currentGO가 null이면 씬 루트에, non-null이면 자식으로 GO 생성
        ///   - 체이닝: GO() 탐색 → CreateGO() → AddComp → SOString / SOColor 가능
        ///   - SORef(): SerializedProperty objectReferenceValue 세터
        ///   - WithComp 콜백 안에서도 SORef() 사용 가능 (ComponentEditScope.SORef)
        ///
        /// SceneEditor.GO()가 없으면 자동 생성했던 패턴을 SindyEdit으로 구현합니다.
        /// </summary>
        [MenuItem("Sindy/Examples/A - Scene Edit (CreateGO + SORef)")]
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

            // ── CreateGO: _currentGO null → 씬 루트에 생성 ──────────────────────
            // GO()로 탐색하지 않은 상태(null)에서 CreateGO를 호출하면 씬 루트에 배치됩니다.
            s.CreateGO("Canvas");

            // ── 체이닝: GO 탐색 후 자식 GO 생성 + AddComp + SO* ──────────────────
            // GO()로 기존 GO를 찾고, CreateGO()로 자식을 생성하면 _currentGO가 새 GO로 이동합니다.
            s.GO("Canvas").CreateGO("HUD").CreateGO("Title")
                .AddComp<TextMeshProUGUI>()
                .SOString("m_text", "ComponentBuilder Showcase")
                .SOFloat("m_fontSize", 28f)
                .SOColor("m_fontColor", new Color(0.5f, 1f, 0.9f));

            s.GO("Canvas/HUD").CreateGO("Background")
                .AddComp<Image>()
                .SOColor("m_Color", new Color(0f, 0f, 0f, 0.6f));

            s.GO("Canvas/HUD").CreateGO("Footer");

            s.GO("Canvas/HUD/Footer").CreateGO("VersionLabel")
                .AddComp<TextMeshProUGUI>()
                .SOString("m_text", "v1.0.0")
                .SOFloat("m_fontSize", 11f)
                .SOColor("m_fontColor", new Color(0.55f, 0.55f, 0.55f));

            // ── SORef: WithComp 콜백 안에서 objectReferenceValue 설정 ────────────
            // session 레벨: s.GOFind("Icon").SORef("m_Sprite", spriteAsset);
            // ComponentEditScope 레벨:
            // s.GOFind("Icon").WithComp<Image>(img => img.SORef("m_Sprite", mySprite));

            // Dispose 시 변경사항이 있으면 자동 저장됩니다.
        }

        // ─── A-4: SindyEdit 신규 API 시연 ────────────────────────────────────

        /// <summary>
        /// ✅ SindyEdit 신규 API 시연:
        ///   - SindyEdit.Open()으로 씬 열기
        ///   - GOFind(): 계층 어디에 있든 이름으로 재귀 탐색
        ///   - Root(): 씬 첫 번째 루트 GO 접근
        ///   - Child(): 직계 자식 인덱스 / 이름으로 탐색
        ///   - GetFloat() / GetString() / GetColor() 등 값 읽기
        ///   - WithComp<T>(Action): 콜백에서 특정 컴포넌트 편집
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

            // ── GOFind: 계층 전체를 재귀 탐색 (경로 없이 이름만으로 찾기) ────
            float titleFontSize = s.GOFind("Title").GetFloat("m_fontSize");
            string versionText = s.GOFind("VersionLabel").GetString("m_text");
            Debug.Log($"[Example A] Title fontSize: {titleFontSize}, VersionLabel: \"{versionText}\"");

            // ── WithComp<T>: 콜백 방식으로 특정 컴포넌트 편집 ───────────────
            // 콜백 종료 후 ApplyModifiedPropertiesWithoutUndo() 자동 호출.
            s.GOFind("Title").WithComp<TextMeshProUGUI>(tmp =>
                tmp.Set("m_fontColor", new Color(1f, 0.9f, 0.5f)));

            // ── Root(): 씬 첫 번째 루트 GO로 이동 ────────────────────────────
            s.Root();
            Debug.Log($"[Example A] 첫 번째 루트 GO: {s.GetComp<Transform>()?.gameObject.name ?? "null"}");

            // ── Child(string): 직계 자식을 이름으로 탐색 ─────────────────────
            // GO()가 씬 루트 기준 경로 탐색이라면, Child()는 현재 GO 기준 직계 자식 탐색입니다.
            s.GO("Canvas").Child("HUD").Child("Title").WithComp<TextMeshProUGUI>(tmp =>
                tmp.Set("m_text", "SindyEdit으로 수정됨"));

            // ── Child(int): 인덱스로 직계 자식 접근 ──────────────────────────
            // HUD의 첫 번째 자식(인덱스 0)으로 이동한 뒤 이름을 읽습니다.
            s.GO("Canvas").Child("HUD").Child(0);
            var firstHudChild = s.GetComp<Transform>();
            if (firstHudChild != null)
                Debug.Log($"[Example A] HUD 첫 번째 자식: {firstHudChild.gameObject.name}");

            // ── AddComp<T>: 현재 GO에 컴포넌트 추가 (없을 때만) ──────────────
            s.GO("Canvas").AddComp<CanvasGroup>();

            // ── GetComp<T>: 컴포넌트 인스턴스 직접 획득 ─────────────────────
            var canvasGroup = s.GO("Canvas").GetComp<CanvasGroup>();
            if (canvasGroup != null)
                Debug.Log($"[Example A] CanvasGroup alpha: {canvasGroup.alpha}");
        }
    }
}
#endif
