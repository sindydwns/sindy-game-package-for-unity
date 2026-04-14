// ────────────────────────────────────────────────────────────────────────────
// 예제 A — 씬 편집 (SceneEditor / GOEditor / AssetFinder)
//
// 구현 파일: Editor/SceneEditor/SceneEditor.cs, GOEditor.cs, AssetFinder.cs
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using Sindy.Editor.SceneTools;
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
    ///
    /// Menu: Sindy/Examples/A - Scene Edit
    /// </summary>
    public static class Example_SceneEdit
    {
        private const string ScenePath =
            "Assets/sindy-game-package-for-unity/Tests/Runtime/ComponentBuilderTest" +
            "/_test_component_builder_quick.unity";

        [MenuItem("Sindy/Examples/A - Scene Edit")]
        public static void Run()
        {
            // ── 1. 씬 열기 ──────────────────────────────────────────────────
            // 이미 열린 씬이면 재사용. 미저장 변경사항이 있으면 사용자에게 묻는다.
            // 취소 또는 실패 시 null 반환 → null 체크 필수.
            using (var ctx = SceneEditor.Open(ScenePath))
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
        /// ⚠ 어셈블리 경계:
        ///   ShowcaseRunner는 Sindy.Tests.Runtime 어셈블리에 있으므로
        ///   이 파일(Sindy.Editor)에서 AddComp&lt;ShowcaseRunner&gt;()를 직접 쓸 수 없다.
        ///   대신 타입 전체 이름 문자열을 전달하는 AddComp(string) 오버로드를 사용한다.
        /// </summary>
        private static void SetupShowcaseRunner(SceneEditor ctx)
        {
            // ── AssetFinder: 타입 전체 이름 + 힌트로 프리팹 컴포넌트 탐색 ──
            var label  = AssetFinder.Prefab("Sindy.View.Components.LabelComponent",  "label");
            var button = AssetFinder.Prefab("Sindy.View.Components.ButtonComponent", "button");
            var gauge  = AssetFinder.Prefab("Sindy.View.Components.GaugeComponent",  "gauge");
            var toggle = AssetFinder.Prefab("Sindy.View.Components.ToggleComponent", "toggle");
            var list   = AssetFinder.Prefab("Sindy.View.Components.ListComponent",   "list");
            var page   = AssetFinder.Prefab("Sindy.View.Components.PageComponent",   "page");
            var tab    = AssetFinder.Prefab("Sindy.View.Components.TabComponent",    "tab");
            var popup  = AssetFinder.Prefab("Sindy.View.Components.PopupComponent",  "popup");

            // ── GOEditor 체인 ─────────────────────────────────────────────────
            // GO("ShowcaseRunner") : 씬 루트에서 "ShowcaseRunner" 탐색, 없으면 생성
            // AddComp(typeFullName): 어셈블리 경계를 넘어 컴포넌트를 추가할 때 사용
            // SORef(path, value)   : objectReferenceValue 설정
            // Apply()              : ApplyModifiedProperties + SetDirty. 반드시 호출!
            ctx.GO("ShowcaseRunner")
                .AddComp("Sindy.Test.ShowcaseRunner") // 문자열 오버로드로 어셈블리 경계 우회
                .SORef("labelPrefab",  label,  ignoreNullWarning: true)
                .SORef("buttonPrefab", button, ignoreNullWarning: true)
                .SORef("gaugePrefab",  gauge,  ignoreNullWarning: true)
                .SORef("togglePrefab", toggle, ignoreNullWarning: true)
                .SORef("listPrefab",   list,   ignoreNullWarning: true)
                .SORef("pagePrefab",   page,   ignoreNullWarning: true)
                .SORef("tabPrefab",    tab,    ignoreNullWarning: true)
                .SORef("popupPrefab",  popup,  ignoreNullWarning: true)
                .SOFloat("cellWidth",   240f)
                .SOFloat("cellHeight",  200f)
                .SOInt("gridColumns",   3)
                .SOFloat("cycleSec",    3.0f)
                .SOColor("bgColor",   new Color(0.12f, 0.12f, 0.15f))
                .SOColor("cellColor", new Color(0.20f, 0.20f, 0.26f))
                .Apply();
        }

        // ─── (2) Canvas.HUD.* 계층 생성 ──────────────────────────────────────

        /// <summary>
        /// Canvas → HUD → Title / Background / Footer.VersionLabel 계층을 자동 생성한다.
        ///
        /// GO(path): 씬 루트 기준 계층 경로. 각 노드가 없으면 자동 생성됨.
        ///
        /// ⚠ Unity 내부 직렬화 필드명 (m_ 접두사):
        ///   TextMeshProUGUI.text     → "m_text"
        ///   TextMeshProUGUI.fontSize → "m_fontSize"
        ///   TextMeshProUGUI.color    → "m_fontColor"
        ///   Image.color              → "m_Color"
        ///
        ///   정확한 필드명을 모를 때: Sindy/Tools/Field Peeker Window (FieldPeeker 도구)
        ///   또는 코드에서: FieldPeeker.Print&lt;TextMeshProUGUI&gt;(gameObject);
        /// </summary>
        private static void SetupHUD(SceneEditor ctx)
        {
            // ── Canvas.HUD.Title ──────────────────────────────────────────────
            ctx.GO("Canvas.HUD.Title")
                .AddComp<TextMeshProUGUI>()
                .SOStr("m_text",        "ComponentBuilder Showcase")
                .SOFloat("m_fontSize",   28f)
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
                .SOStr("m_text",        "v1.0.0")
                .SOFloat("m_fontSize",   11f)
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
    }
}
#endif
