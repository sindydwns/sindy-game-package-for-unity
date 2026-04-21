#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// 씬을 안전하게 열고 저장하는 컨텍스트 래퍼.
    /// <para>
    /// using 블록 종료 시 <see cref="MarkDirty"/>가 호출된 경우 자동으로 씬을 저장합니다.
    /// </para>
    /// <example>
    /// <code>
    /// var ctx = SceneEditor.Open("Assets/.../MyScene.unity");
    /// if (ctx == null) return;   // 사용자 취소 또는 열기 실패
    ///
    /// using (ctx)
    /// {
    ///     ctx.GO("ShowcaseRunner")
    ///         .AddComp<ShowcaseRunner>()
    ///         .SOFloat("cellWidth", 240f)
    ///         .Apply();
    ///
    ///     ctx.MarkDirty();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class SceneEditor : IDisposable
    {
        private readonly Scene _scene;
        private bool _dirty;
        private bool _disposed;

        // ── 프로퍼티 ──────────────────────────────────────────────────────────

        /// <summary>현재 열린 씬</summary>
        public Scene Scene => _scene;

        // ── 생성자 ────────────────────────────────────────────────────────────

        private SceneEditor(Scene scene)
        {
            _scene = scene;
        }

        // ── 팩토리 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 씬을 열거나, 이미 열려있으면 재사용합니다.
        /// 현재 씬에 미저장 변경사항이 있으면 사용자에게 저장 여부를 묻습니다.
        /// </summary>
        /// <param name="scenePath">Assets/ 로 시작하는 씬 경로</param>
        /// <returns>씬 컨텍스트. 사용자 취소 또는 열기 실패 시 null.</returns>
        public static SceneEditor Open(string scenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                if (s.path == scenePath && File.Exists(scenePath))
                {
                    Debug.Log($"[SceneEditor] 씬이 이미 열려있어 재사용합니다: {scenePath}");
                    return new SceneEditor(s);
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return null;

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("오류", $"씬을 열 수 없습니다:\n{scenePath}", "확인");
                return null;
            }

            Debug.Log($"[SceneEditor] 씬을 열었습니다: {scenePath}");
            return new SceneEditor(scene);
        }

        // ── 계층 탐색 ─────────────────────────────────────────────────────────

        /// <summary>
        /// "Canvas.Panel.Button" 형태의 경로로 GameObject를 탐색하거나 생성합니다.
        /// <para>
        /// (Hierarchy path) 각 세그먼트를 부모→자식 순서로 탐색하고, 없으면 생성합니다.
        /// 생성 시 Undo에 등록됩니다.
        /// </para>
        /// </summary>
        /// <param name="hierarchyPath">점(.)으로 구분된 계층 경로. 예: "Canvas.Panel.Button"</param>
        public GOEditor GO(string hierarchyPath)
            => GOEditor.GetOrCreate(_scene, hierarchyPath);

        /// <summary>
        /// 경로로 GameObject를 탐색합니다. 없으면 생성하지 않고 null을 반환합니다.
        /// <para>
        /// 경로 탐색 실패 시 어느 노드에서 끊겼는지 LogWarning으로 알려줍니다.
        /// </para>
        /// </summary>
        /// <param name="hierarchyPath">점(.)으로 구분된 계층 경로</param>
        /// <returns>GOEditor. 경로를 찾지 못한 경우 null.</returns>
        public GOEditor GOFind(string hierarchyPath)
            => GOEditor.FindOnly(_scene, hierarchyPath);

        // ── 씬 저장 ───────────────────────────────────────────────────────────

        /// <summary>
        /// Dispose 시 EditorSceneManager.SaveScene을 호출하도록 표시합니다.
        /// 변경사항이 있을 때만 호출하세요.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
            EditorSceneManager.MarkSceneDirty(_scene);
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// using 블록 종료 시 자동으로 호출됩니다.
        /// <see cref="MarkDirty"/>가 호출된 경우 씬을 저장합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (!_dirty) return;

            bool saved = EditorSceneManager.SaveScene(_scene);
            if (saved)
                Debug.Log($"[SceneEditor] 씬 저장 완료: {_scene.path}");
            else
                Debug.LogWarning($"[SceneEditor] 씬 저장 실패: {_scene.path}");
        }
    }
}
#endif
