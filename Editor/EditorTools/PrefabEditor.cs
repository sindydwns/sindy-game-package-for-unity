#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// 프리팹을 안전하게 편집하는 컨텍스트 래퍼.
    /// <para>
    /// using 블록 종료 시 자동으로 <c>PrefabUtility.SaveAsPrefabAsset</c>과
    /// <c>PrefabUtility.UnloadPrefabContents</c>를 호출합니다.
    /// </para>
    /// <example>
    /// <code>
    /// using (var p = PrefabEditor.Open("Assets/.../MyPrefab.prefab"))
    /// {
    ///     if (p == null) return;
    ///
    ///     // 루트 GO 직접 접근
    ///     p.Root()
    ///         .WithComp<MyComp>()
    ///         .SOFloat("speed", 5f)
    ///         .Apply();
    ///
    ///     // 자식 경로 접근
    ///     p.GO("Canvas.Panel.Button")
    ///         .AddComp<Button>()
    ///         .Apply();
    ///
    ///     // 없으면 생성하지 않는 탐색
    ///     p.GOFind("Canvas.Header")
    ///      ?.WithComp<Image>()
    ///      .SOColor("m_Color", Color.white)
    ///      .Apply();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class PrefabEditor : IDisposable
    {
        private readonly string _assetPath;
        private GameObject _root;
        private bool _disposed;

        // ── 프로퍼티 ──────────────────────────────────────────────────────────

        /// <summary>편집 중인 프리팹의 루트 GameObject</summary>
        public GameObject RootObject => _root;

        // ── 생성자 ────────────────────────────────────────────────────────────

        private PrefabEditor(string assetPath, GameObject root)
        {
            _assetPath = assetPath;
            _root = root;
        }

        // ── 팩토리 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 프리팹을 PrefabUtility.LoadPrefabContents로 로드합니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .prefab 파일 경로</param>
        /// <returns>편집 컨텍스트. 경로 오류 또는 로드 실패 시 null.</returns>
        public static PrefabEditor Open(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("오류", $"유효한 프리팹 경로가 아닙니다:\n{assetPath}", "확인");
                return null;
            }

            var root = PrefabUtility.LoadPrefabContents(assetPath);
            if (root == null)
            {
                EditorUtility.DisplayDialog("오류", $"프리팹을 로드할 수 없습니다:\n{assetPath}", "확인");
                return null;
            }

            Debug.Log($"[PrefabEditor] 프리팹 로드됨: {assetPath}");
            return new PrefabEditor(assetPath, root);
        }

        // ── 계층 탐색 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 루트 GO를 기준으로 경로에 해당하는 자식 GO를 탐색하거나 생성합니다.
        /// <para>(Hierarchy path) 없는 노드는 자동 생성됩니다.</para>
        /// </summary>
        public GameObjectEditor GO(string hierarchyPath)
            => GameObjectEditor.GetOrCreate(_root.transform, hierarchyPath);

        /// <summary>
        /// 루트 GO를 기준으로 경로에 해당하는 자식 GO를 탐색합니다. 없으면 null 반환.
        /// </summary>
        public GameObjectEditor GOFind(string hierarchyPath)
            => GameObjectEditor.FindOnly(_root.transform, hierarchyPath);

        /// <summary>
        /// 프리팹 루트 GO에 대한 GOEditor를 반환합니다.
        /// <para><c>p.GO(p.RootObject.name)</c>의 단축 표현입니다.</para>
        /// </summary>
        public GameObjectEditor Root()
            => GameObjectEditor.For(_root);

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// using 블록 종료 시 자동으로 호출됩니다.
        /// 프리팹을 저장(SaveAsPrefabAsset)하고 언로드(UnloadPrefabContents)합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_root == null) return;

            PrefabUtility.SaveAsPrefabAsset(_root, _assetPath);
            PrefabUtility.UnloadPrefabContents(_root);
            _root = null;
            Debug.Log($"[PrefabEditor] 프리팹 저장 및 언로드 완료: {_assetPath}");
        }
    }
}
#endif
