// ────────────────────────────────────────────────────────────────────────────
// 예제 C — ScriptableObject 편집 (SOEditor / AssetFinder)
//
// 구현 파일: Editor/EditorTools/SOEditor.cs, AssetFinder.cs
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using Sindy.Editor.EditorTools;
using Sindy.Scriptables;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.Examples
{
    /// <summary>
    /// 예제 C — SindyEdit / SOEditor / AssetFinder SO 편집 사용법
    ///
    /// 시나리오:
    ///   (1) SOEditor.Create()로 ScriptableObject 에셋을 새로 생성하고 필드 설정
    ///       (에셋 생성은 SindyEdit 미지원 → SOEditor.Create() 직접 사용)
    ///   (2) SindyEdit.Open()으로 기존 에셋을 로드하여 편집
    ///   (3) AssetFinder.AllAssets<T>()로 탐색 후 SindyEdit으로 일괄 편집
    ///   (4) 중첩 경로(dot notation) 사용 예시
    ///
    /// Menu: Sindy/Examples/C - SO Edit
    /// </summary>
    public static class Example_SOEditor
    {
        private static string SOOutputFolder =>
            PackagePathHelper.Resolve("Tests/Runtime");

        // ─────────────────────────────────────────────────────────────────────
        // (1) 새 ScriptableObject 에셋 생성 + SOEditor으로 필드 설정
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Sindy/Examples/C - SO Create & Edit")]
        public static void CreateAndEdit()
        {
            // ── SOEditor.Create(): CreateInstance + CreateAsset + SerializedObject.Update 자동 처리 ──
            // Dispose 시 AssetDatabase.SaveAssets() 자동 호출.
            // Apply() 없이 Dispose하면 LogWarning 출력 (미저장 경고).

            using (var so = ScriptableObjectEditor<IntVariable>.Create($"{SOOutputFolder}/Example_IntVariable.asset"))
            {
                // ScriptableObjectVariable<T>.Value  → 직렬화 이름: "Value"  (대문자, public)
                // ScriptableObjectVariable<T>.description → "description" (소문자, public)
                so.SetInt("Value", 42)
                  .SetStr("description", "예제용 카운터 변수")
                  .Apply();
            }

            using (var so = ScriptableObjectEditor<FloatVariable>.Create($"{SOOutputFolder}/Example_FloatVariable.asset"))
            {
                so.SetFloat("Value", 0.75f)
                  .SetStr("description", "예제용 퍼센트 값 (0.0 ~ 1.0)")
                  .Apply();
            }

            using (var so = ScriptableObjectEditor<BoolVariable>.Create($"{SOOutputFolder}/Example_BoolVariable.asset"))
            {
                so.SetBool("Value", true)
                  .SetStr("description", "예제용 플래그")
                  .Apply();
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "예제 C 완료",
                $"ScriptableObject 에셋 3개 생성 완료:\n" +
                $"  {SOOutputFolder}/Example_IntVariable.asset\n" +
                $"  {SOOutputFolder}/Example_FloatVariable.asset\n" +
                $"  {SOOutputFolder}/Example_BoolVariable.asset",
                "확인");
        }

        // ─────────────────────────────────────────────────────────────────────
        // (2) 기존 SO 에셋 로드 + SindyEdit으로 편집
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Sindy/Examples/C - SO Load & Edit")]
        public static void LoadAndEdit()
        {
            string path = $"{SOOutputFolder}/Example_IntVariable.asset";

            // ── SindyEdit.Open(): 에셋 로드. 로드 실패 시 null 반환 → null 체크 필수.
            // Dispose 시 AssetDatabase.SaveAssets() 자동 호출. Apply() 불필요.
            using (var s = SindyEdit.Open(path))
            {
                if (s == null)
                {
                    EditorUtility.DisplayDialog(
                        "에셋 없음",
                        $"먼저 'C - SO Create & Edit'를 실행하여 에셋을 생성하세요.\n경로: {path}",
                        "확인");
                    return;
                }

                s.SetInt("Value", 100)
                 .SetString("description", "값이 수정되었습니다.");
            }

            Debug.Log("[Example C] IntVariable 로드 및 수정 완료.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // (3) AssetFinder.AllAssets<T>()로 SO 탐색 + 일괄 편집
        // ─────────────────────────────────────────────────────────────────────

        [MenuItem("Sindy/Examples/C - SO Batch Edit via AssetFinder")]
        public static void BatchEditViaAssetFinder()
        {
            // AssetFinder.AllAssets<T>(): 지정 폴더 내 모든 T 타입 SO 에셋 반환.
            // 결과를 에디터 세션 동안 캐싱하므로 새 에셋 생성 후에는 ClearCache() 호출.
            var allFloats = AssetFinder.AllAssets<FloatVariable>(SOOutputFolder);

            foreach (var floatVar in allFloats)
            {
                string assetPath = AssetDatabase.GetAssetPath(floatVar);

                using (var s = SindyEdit.Open(assetPath))
                {
                    if (s == null) continue;

                    s.SetFloat("Value", 0f); // 모든 FloatVariable 초기화
                }
            }

            Debug.Log($"[Example C] FloatVariable {allFloats.Count}개 초기화 완료.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // (4) 중첩 경로(dot notation) 사용 예시
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// ScriptableObjectReference 처럼 중첩 구조를 가진 필드를 편집할 때
        /// "필드명.서브필드명" 경로를 사용한다.
        ///
        /// SOPropertyHelper.GetProperty()가 '.'으로 분리된 경로를
        /// FindProperty → FindPropertyRelative 체인으로 탐색한다.
        ///
        /// ※ 정확한 경로를 모를 때: Sindy/Tools/Field Peeker Window
        /// </summary>
        private static void EditNestedReference(IntVariable targetSO)
        {
            // targetSO에 "healthRef"라는 ScriptableObjectReference 필드가 있다고 가정.
            //
            // ScriptableObjectReference 내부 구조:
            //   UseConstant   : bool
            //   ConstantValue : T
            //   Variable      : V
            //
            // "필드.서브필드" 경로로 한 줄에 접근 가능:
            string assetPath = AssetDatabase.GetAssetPath((UnityEngine.Object)targetSO);
            using (var s = SindyEdit.Open(assetPath))
            {
                if (s == null) return;

                s.SetBool("healthRef.UseConstant", true)
                 .SetFloat("healthRef.ConstantValue", 100f);
            }
        }
    }
}
#endif
