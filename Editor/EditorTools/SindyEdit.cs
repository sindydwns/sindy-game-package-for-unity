#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// 씬(.unity), 프리팹(.prefab), ScriptableObject(.asset)을 동일한 API로 편집하는 통합 파사드.
    /// <para>
    /// <see cref="Open"/>으로 경로를 지정하거나 <see cref="Find"/>로 이름 자동 탐색 후
    /// <see cref="AssetEditSession"/>을 통해 GO 탐색 및 SerializedProperty 편집을 수행합니다.
    /// </para>
    /// <example>
    /// <code>
    /// // 씬 편집
    /// using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    /// s.GO("Canvas/Panel/Title").SOString("m_text", "Hello").SOColor("m_Color", Color.white);
    ///
    /// // 프리팹 편집
    /// using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    /// s.GO("Fill/Image").SOColor("m_Color", Color.green);
    ///
    /// // SO 편집
    /// using var s = SindyEdit.Open("Assets/Config/Game.asset");
    /// s.SOInt("maxHealth", 200).SOFloat("gravity", 9.81f);
    ///
    /// // SO 신규 생성 후 편집
    /// using var s = SindyEdit.Create&lt;FloatVariable&gt;("Assets/Data/Speed.asset");
    /// s.SOFloat("value", 5f);
    ///
    /// // 이름으로 자동 탐색
    /// using var s = SindyEdit.Find("GaugeBar");
    /// s.GOFind("Fill").EditComp&lt;Image&gt;(img =&gt; img.Set("m_Color", Color.green));
    ///
    /// // FP 스타일: 탐색은 새 인스턴스 반환 — s는 변경되지 않음
    /// var player = s.GOFind("Player");
    /// var hp = player.Child("HpBar");
    /// hp.SOFloat("value", 100f);
    /// s.Root().Child("UI").GOFind("Button").SOString("label", "Start");
    /// </code>
    /// </example>
    /// </summary>
    public static class SindyEdit
    {
        /// <summary>
        /// 에셋 경로로 편집 세션을 엽니다.
        /// 확장자가 .unity이면 SceneEditor, .prefab이면 PrefabEditor,
        /// 그 외(예: .asset)이면 SerializedObject를 직접 사용합니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 에셋 경로</param>
        /// <returns>편집 세션. 로드 실패 시 null.</returns>
        public static AssetEditSession Open(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return null;
            }

            string ext = Path.GetExtension(assetPath).ToLowerInvariant();
            return ext switch
            {
                ".unity" => AssetEditSession.ForScene(assetPath),
                ".prefab" => AssetEditSession.ForPrefab(assetPath),
                _ => AssetEditSession.ForAsset(assetPath),
            };
        }

        /// <summary>
        /// 에셋 이름 또는 경로로 편집 세션을 찾아 엽니다.
        /// <para>
        /// "Assets/" 로 시작하면 <see cref="Open"/>과 동일하게 동작합니다.
        /// 그 외에는 AssetFinder / AssetDatabase 탐색으로 에셋을 찾습니다.
        /// 탐색 우선순위: 프리팹 → 씬 → ScriptableObject
        /// </para>
        /// </summary>
        /// <param name="nameOrPath">에셋 이름(예: "GaugeBar") 또는 전체 경로</param>
        /// <returns>편집 세션. 탐색 실패 시 null.</returns>
        public static AssetEditSession Find(string nameOrPath)
        {
            if (string.IsNullOrEmpty(nameOrPath))
            {
                Debug.LogError("[SindyEdit] 이름이 비어있습니다.");
                return null;
            }

            // 경로처럼 보이면 Open으로 위임
            if (nameOrPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                nameOrPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return Open(nameOrPath);

            // 프리팹 탐색
            var prefabGO = AssetFinder.PrefabByName(nameOrPath);
            if (prefabGO != null)
            {
                string path = AssetDatabase.GetAssetPath(prefabGO);
                if (!string.IsNullOrEmpty(path))
                {
                    Debug.Log($"[SindyEdit] 프리팹 발견: {path}");
                    return Open(path);
                }
            }

            // 씬 탐색 — 이름이 정확히 일치하는 것 우선
            string[] sceneGuids = AssetDatabase.FindAssets($"{nameOrPath} t:Scene");
            foreach (string guid in sceneGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p)
                    .Equals(nameOrPath, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[SindyEdit] 씬 발견: {p}");
                    return Open(p);
                }
            }
            if (sceneGuids.Length > 0)
                return Open(AssetDatabase.GUIDToAssetPath(sceneGuids[0]));

            // ScriptableObject 탐색
            string[] soGuids = AssetDatabase.FindAssets($"{nameOrPath} t:ScriptableObject");
            foreach (string guid in soGuids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(p)
                    .Equals(nameOrPath, StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log($"[SindyEdit] ScriptableObject 발견: {p}");
                    return Open(p);
                }
            }
            if (soGuids.Length > 0)
                return Open(AssetDatabase.GUIDToAssetPath(soGuids[0]));

            Debug.LogWarning($"[SindyEdit] '{nameOrPath}' 에셋을 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 새 ScriptableObject를 생성하고 편집 세션을 반환합니다.
        /// 지정 경로에 파일이 이미 있으면 덮어씁니다.
        /// </summary>
        /// <typeparam name="T">생성할 ScriptableObject 타입</typeparam>
        /// <param name="assetPath">Assets/ 로 시작하는 .asset 파일 경로</param>
        /// <returns>편집 세션. 생성 실패 시 null.</returns>
        public static AssetEditSession Create<T>(string assetPath) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return null;
            }

            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[SindyEdit] ScriptableObject 생성됨: {assetPath}");
            return AssetEditSession.ForAsset(assetPath);
        }

        /// <summary>
        /// 새 씬 파일을 생성하고 편집 세션을 반환합니다.
        /// 지정 경로에 파일이 이미 있으면 덮어씁니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .unity 파일 경로</param>
        /// <returns>편집 세션. 생성 실패 시 null.</returns>
        public static AssetEditSession NewScene(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return null;
            }

            if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                assetPath = "Assets/" + assetPath;
                Debug.LogWarning($"[SindyEdit] 경로가 'Assets/'로 시작하지 않아 자동으로 붙였습니다: {assetPath}");
            }

            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            bool saved = EditorSceneManager.SaveScene(scene, assetPath);
            if (!saved)
            {
                Debug.LogError($"[SindyEdit] 씬 저장 실패: {assetPath}");
                return null;
            }

            AssetDatabase.Refresh();

            if (!File.Exists(assetPath))
            {
                Debug.LogError($"[SindyEdit] 씬 파일이 생성되지 않았습니다: {assetPath}");
                return null;
            }

            Debug.Log($"[SindyEdit] 씬 생성됨: {assetPath}");
            return AssetEditSession.ForScene(assetPath);
        }

        /// <summary>
        /// 빈 프리팹 파일을 생성하고 편집 세션을 반환합니다.
        /// 지정 경로에 파일이 이미 있으면 덮어씁니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .prefab 파일 경로</param>
        /// <param name="rootName">프리팹 루트 GameObject 이름</param>
        /// <returns>편집 세션. 생성 실패 시 null.</returns>
        public static AssetEditSession NewPrefab(string assetPath, string rootName = "Root")
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return null;
            }

            string dir = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var go = new GameObject(rootName);
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            GameObject.DestroyImmediate(go);
            AssetDatabase.Refresh();
            Debug.Log($"[SindyEdit] 프리팹 생성됨: {assetPath}");
            return AssetEditSession.ForPrefab(assetPath);
        }

        /// <summary>
        /// 씬 파일(.unity)을 디스크에서 삭제합니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .unity 파일 경로</param>
        public static void DeleteScene(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return;
            }

            if (!Path.GetExtension(assetPath).Equals(".unity", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[SindyEdit] DeleteScene: .unity 파일이 아닙니다. ({assetPath})");
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
            Debug.Log($"[SindyEdit] 씬 삭제됨: {assetPath}");
        }

        /// <summary>
        /// 프리팹 파일(.prefab)을 디스크에서 삭제합니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .prefab 파일 경로</param>
        public static void DeletePrefab(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError("[SindyEdit] 경로가 비어있습니다.");
                return;
            }

            if (!Path.GetExtension(assetPath).Equals(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[SindyEdit] DeletePrefab: .prefab 파일이 아닙니다. ({assetPath})");
                return;
            }

            AssetDatabase.DeleteAsset(assetPath);
            Debug.Log($"[SindyEdit] 프리팹 삭제됨: {assetPath}");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // AssetEditSession
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 씬·프리팹·ScriptableObject를 동일한 API로 편집하는 컨텍스트 세션.
    /// <para>
    /// <b>FP 설계:</b> <see cref="Root"/>, <see cref="GOFind"/>, <see cref="Child(int)"/>,
    /// <see cref="GO"/> 등 탐색 메서드는 <c>this</c>를 변경하지 않고 새로운
    /// <see cref="AssetEditSession"/> 인스턴스를 반환합니다.
    /// 세터(<see cref="SOFloat"/> 등)와 <see cref="EditComp{T}"/> 는 <c>this</c>를 반환합니다.
    /// </para>
    /// <para>
    /// <b>소유권:</b> 팩토리(<see cref="SindyEdit.Open"/> 등)가 반환한 루트 세션만
    /// <see cref="Dispose"/> 시 저장 및 정리를 수행합니다.
    /// 탐색으로 파생된 세션의 <see cref="Dispose"/>는 no-op입니다.
    /// </para>
    /// <para>
    /// using 블록 종료(Dispose) 시 미저장 변경사항을 자동으로 저장합니다.
    /// 명시적으로 <see cref="Save"/>를 호출할 수도 있습니다.
    /// </para>
    /// </summary>
    public sealed class AssetEditSession : IDisposable
    {
        // ── SessionContext: 세션 간 공유되는 가변 상태 ───────────────────────

        private sealed class SessionContext
        {
            internal enum AssetMode { Scene, Prefab, Asset }

            internal readonly AssetMode Mode;
            internal readonly string AssetPath;
            internal readonly SceneEditor SceneEditor;
            internal readonly PrefabEditor PrefabEditor;
            internal readonly UnityEngine.Object SoAsset;
            internal readonly Dictionary<UnityEngine.Object, SerializedObject> SoCache = new();
            internal bool ChangesMade;
            internal bool Cleaned;

            internal SessionContext(
                AssetMode mode, string path,
                SceneEditor se = null, PrefabEditor pe = null, UnityEngine.Object soAsset = null)
            {
                Mode = mode;
                AssetPath = path;
                SceneEditor = se;
                PrefabEditor = pe;
                SoAsset = soAsset;
            }

            internal void ApplyAll()
            {
                foreach (var kvp in SoCache)
                {
                    if (kvp.Value.hasModifiedProperties)
                    {
                        kvp.Value.ApplyModifiedProperties();
                        EditorUtility.SetDirty(kvp.Key);
                    }
                }
            }

            internal void PersistToDisk()
            {
                if (!ChangesMade) return;

                switch (Mode)
                {
                    case AssetMode.Scene:
                        if (SceneEditor != null)
                        {
                            SceneEditor.MarkDirty();
                            EditorSceneManager.SaveScene(SceneEditor.Scene);
                            Debug.Log($"[SindyEdit] 씬 저장됨: {AssetPath}");
                        }
                        break;

                    case AssetMode.Prefab:
                        if (PrefabEditor?.RootObject != null)
                        {
                            PrefabUtility.SaveAsPrefabAsset(PrefabEditor.RootObject, AssetPath);
                            Debug.Log($"[SindyEdit] 프리팹 저장됨: {AssetPath}");
                        }
                        break;

                    case AssetMode.Asset:
                        if (SoAsset != null)
                        {
                            AssetDatabase.SaveAssets();
                            Debug.Log($"[SindyEdit] 에셋 저장됨: {AssetPath}");
                        }
                        break;
                }
            }

            internal void Cleanup()
            {
                Cleaned = true;
                switch (Mode)
                {
                    case AssetMode.Scene:
                        SceneEditor?.Dispose();
                        break;
                    case AssetMode.Prefab:
                        PrefabEditor?.Dispose();
                        break;
                }
            }
        }

        // ── 필드 ──────────────────────────────────────────────────────────────

        private readonly SessionContext _ctx;
        private readonly GameObject _currentGO;
        private readonly bool _isOwner;
        private bool _disposed;

        // ── 생성자 ────────────────────────────────────────────────────────────

        private AssetEditSession(SessionContext ctx, GameObject go, bool isOwner = false)
        {
            _ctx = ctx;
            _currentGO = go;
            _isOwner = isOwner;
        }

        // ── 내부 팩토리 ───────────────────────────────────────────────────────

        internal static AssetEditSession ForScene(string path)
        {
            var se = SceneEditor.Open(path);
            if (se == null)
            {
                Debug.LogError($"[SindyEdit] 씬을 열 수 없습니다: {path}");
                return null;
            }
            var ctx = new SessionContext(SessionContext.AssetMode.Scene, path, se: se);
            return new AssetEditSession(ctx, null, isOwner: true);
        }

        internal static AssetEditSession ForPrefab(string path)
        {
            var pe = PrefabEditor.Open(path);
            if (pe == null)
            {
                Debug.LogError($"[SindyEdit] 프리팹을 열 수 없습니다: {path}");
                return null;
            }
            var ctx = new SessionContext(SessionContext.AssetMode.Prefab, path, pe: pe);
            return new AssetEditSession(ctx, null, isOwner: true);
        }

        internal static AssetEditSession ForAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)
                                    ?? AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                Debug.LogError($"[SindyEdit] 에셋을 로드할 수 없습니다: {path}");
                return null;
            }
            var ctx = new SessionContext(SessionContext.AssetMode.Asset, path, soAsset: asset);
            return new AssetEditSession(ctx, null, isOwner: true);
        }

        // ── Guard ─────────────────────────────────────────────────────────────

        // Cleanup() 호출 이후 파생 세션 포함 모든 메서드 호출을 막습니다.
        private bool IsInvalid => _ctx.Cleaned;

        // ── GO 탐색 (새 AssetEditSession 인스턴스 반환) ───────────────────────

        /// <summary>
        /// '/' 또는 '.' 구분자로 지정한 계층 경로에서 GameObject를 탐색합니다.
        /// <para>
        /// 씬: 씬 루트 기준 경로 (예: "Canvas/Panel/Title")<br/>
        /// 프리팹: 프리팹 루트의 자식 기준 경로 (예: "Fill/Image")<br/>
        /// .asset: 경고만 출력하고 null GO 세션을 반환합니다.
        /// </para>
        /// </summary>
        /// <param name="goPath">계층 경로. '/' 또는 '.' 둘 다 구분자로 허용.</param>
        /// <returns>지정한 GO를 가리키는 새 세션. 탐색 실패 시 null GO 세션.</returns>
        public AssetEditSession GO(string goPath)
        {
            if (IsInvalid) return this;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GO()는 .asset 파일에서 사용할 수 없습니다. ({_ctx.AssetPath})");
                return new AssetEditSession(_ctx, null);
            }

            string normalized = NormalizePath(goPath);
            GOEditor goEditor = null;

            if (_ctx.Mode == SessionContext.AssetMode.Scene)
                goEditor = GOEditor.FindOnly(_ctx.SceneEditor.Scene, normalized);
            else if (_ctx.Mode == SessionContext.AssetMode.Prefab && _ctx.PrefabEditor?.RootObject != null)
                goEditor = GOEditor.FindOnly(_ctx.PrefabEditor.RootObject.transform, normalized);

            if (goEditor == null)
            {
                Debug.LogWarning($"[SindyEdit] GO를 찾을 수 없습니다: {goPath} (에셋: {_ctx.AssetPath})");
                return new AssetEditSession(_ctx, null);
            }

            return new AssetEditSession(_ctx, goEditor.GameObject);
        }

        /// <summary>
        /// 씬의 첫 번째 루트 GO 또는 프리팹 루트 GO를 가리키는 새 세션을 반환합니다.
        /// </summary>
        /// <returns>루트 GO를 가리키는 새 세션. 실패 시 null GO 세션.</returns>
        public AssetEditSession Root()
        {
            if (IsInvalid) return this;

            GameObject go = null;
            if (_ctx.Mode == SessionContext.AssetMode.Scene)
            {
                var roots = _ctx.SceneEditor.Scene.GetRootGameObjects();
                if (roots.Length == 0)
                    Debug.LogWarning($"[SindyEdit] 씬에 루트 GO가 없습니다. ({_ctx.AssetPath})");
                else
                    go = roots[0];
            }
            else if (_ctx.Mode == SessionContext.AssetMode.Prefab)
            {
                go = _ctx.PrefabEditor?.RootObject;
                if (go == null)
                    Debug.LogWarning($"[SindyEdit] 프리팹 루트 GO가 null입니다. ({_ctx.AssetPath})");
            }
            else
            {
                Debug.LogWarning($"[SindyEdit] Root()는 .asset 파일에서 사용할 수 없습니다. ({_ctx.AssetPath})");
            }

            return new AssetEditSession(_ctx, go);
        }

        /// <summary>
        /// 이름으로 GO를 재귀 탐색합니다. 계층 어디에 있든 이름으로 찾습니다.
        /// <para>
        /// 씬: 모든 루트 GO를 기준으로 재귀 탐색<br/>
        /// 프리팹: 프리팹 루트를 기준으로 재귀 탐색
        /// </para>
        /// </summary>
        /// <param name="name">탐색할 GO 이름 (정확히 일치)</param>
        /// <returns>찾은 GO를 가리키는 새 세션. 탐색 실패 시 null GO 세션.</returns>
        public AssetEditSession GOFind(string name)
        {
            if (IsInvalid) return this;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GOFind()는 .asset 파일에서 사용할 수 없습니다. ({_ctx.AssetPath})");
                return new AssetEditSession(_ctx, null);
            }

            GameObject found = null;
            if (_ctx.Mode == SessionContext.AssetMode.Scene)
            {
                foreach (var root in _ctx.SceneEditor.Scene.GetRootGameObjects())
                {
                    found = FindRecursive(root.transform, name);
                    if (found != null) break;
                }
            }
            else if (_ctx.Mode == SessionContext.AssetMode.Prefab && _ctx.PrefabEditor?.RootObject != null)
            {
                found = FindRecursive(_ctx.PrefabEditor.RootObject.transform, name);
            }

            if (found == null)
                Debug.LogWarning($"[SindyEdit] GOFind: '{name}'을 찾을 수 없습니다. ({_ctx.AssetPath})");

            return new AssetEditSession(_ctx, found);
        }

        // ── Child 탐색 ────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 GO의 인덱스로 직계 자식 GO를 가리키는 새 세션을 반환합니다.
        /// </summary>
        /// <param name="index">자식 인덱스 (0부터 시작)</param>
        /// <returns>자식 GO를 가리키는 새 세션. 실패 시 null GO 세션.</returns>
        public AssetEditSession Child(int index)
        {
            if (IsInvalid) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] Child({index}): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return new AssetEditSession(_ctx, null);
            }

            if (index < 0 || index >= _currentGO.transform.childCount)
            {
                Debug.LogWarning(
                    $"[SindyEdit] Child({index}): 인덱스 범위를 벗어났습니다. " +
                    $"('{_currentGO.name}' 자식 수: {_currentGO.transform.childCount})");
                return new AssetEditSession(_ctx, null);
            }

            return new AssetEditSession(_ctx, _currentGO.transform.GetChild(index).gameObject);
        }

        /// <summary>
        /// 현재 GO의 직계 자식 중 이름이 일치하는 GO를 가리키는 새 세션을 반환합니다.
        /// </summary>
        /// <param name="name">직계 자식 GO 이름</param>
        /// <returns>자식 GO를 가리키는 새 세션. 실패 시 null GO 세션.</returns>
        public AssetEditSession Child(string name)
        {
            if (IsInvalid) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] Child('{name}'): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return new AssetEditSession(_ctx, null);
            }

            var child = _currentGO.transform.Find(name);
            if (child == null)
            {
                Debug.LogWarning(
                    $"[SindyEdit] Child('{name}'): '{_currentGO.name}'의 직계 자식에서 찾을 수 없습니다.");
                return new AssetEditSession(_ctx, null);
            }

            return new AssetEditSession(_ctx, child.gameObject);
        }

        // ── GO 신규 생성 ──────────────────────────────────────────────────────

        /// <summary>
        /// 현재 GO의 자식으로 새 GameObject를 생성하고 그 GO를 가리키는 새 세션을 반환합니다.
        /// <para>
        /// _currentGO가 null이면 씬/프리팹 루트에 생성합니다.<br/>
        /// 씬 모드: SceneManager.MoveGameObjectToScene으로 씬에 배치<br/>
        /// 프리팹 모드: 프리팹 루트 또는 현재 GO의 자식으로 배치<br/>
        /// .asset 모드: 경고 출력 후 null GO 세션을 반환합니다.
        /// </para>
        /// <para>반환된 새 세션으로 체이닝을 계속할 수 있습니다.</para>
        /// </summary>
        /// <param name="name">생성할 GameObject 이름</param>
        /// <returns>새 GO를 가리키는 새 세션.</returns>
        public AssetEditSession CreateGO(string name)
        {
            if (IsInvalid) return this;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] CreateGO()는 .asset 파일에서 사용할 수 없습니다. ({_ctx.AssetPath})");
                return new AssetEditSession(_ctx, null);
            }

            var newGO = new GameObject(name);

            if (_ctx.Mode == SessionContext.AssetMode.Scene)
            {
                if (_currentGO != null)
                    newGO.transform.SetParent(_currentGO.transform, false);
                else
                    SceneManager.MoveGameObjectToScene(newGO, _ctx.SceneEditor.Scene);
            }
            else // Prefab
            {
                var parent = _currentGO != null ? _currentGO.transform : _ctx.PrefabEditor.RootObject.transform;
                newGO.transform.SetParent(parent, false);
            }

            _ctx.ChangesMade = true;
            Debug.Log($"[SindyEdit] GO 생성됨: '{name}' (에셋: {_ctx.AssetPath})");
            return new AssetEditSession(_ctx, newGO);
        }

        // ── 컴포넌트 접근 및 추가 ────────────────────────────────────────────

        /// <summary>
        /// 현재 GO에서 컴포넌트를 가져옵니다. 없으면 null을 반환합니다.
        /// </summary>
        public T GetComp<T>() where T : Component
        {
            if (IsInvalid || _currentGO == null) return null;
            return _currentGO.GetComponent<T>();
        }

        /// <summary>
        /// 현재 GO에 컴포넌트가 없을 때만 추가합니다.
        /// 추가 시 Undo에 등록됩니다.
        /// </summary>
        public AssetEditSession AddComp<T>() where T : Component
        {
            if (IsInvalid) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] AddComp<{typeof(T).Name}>: GO가 선택되지 않았습니다.");
                return this;
            }

            if (_currentGO.GetComponent<T>() == null)
            {
                Undo.AddComponent<T>(_currentGO);
                _ctx.ChangesMade = true;
                Debug.Log($"[SindyEdit] 컴포넌트 추가됨: {typeof(T).Name} on '{_currentGO.name}'");
            }

            return this;
        }

        /// <summary>
        /// 현재 GO를 씬/프리팹에서 제거합니다.
        /// 부모 GO가 있으면 부모를 가리키는 새 세션을 반환하고, 없으면 null GO 세션을 반환합니다.
        /// </summary>
        public AssetEditSession DeleteGO()
        {
            if (IsInvalid) return this;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] DeleteGO()는 .asset 파일에서 사용할 수 없습니다. ({_ctx.AssetPath})");
                return new AssetEditSession(_ctx, null);
            }

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] DeleteGO(): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return new AssetEditSession(_ctx, null);
            }

            var parentGO = _currentGO.transform.parent?.gameObject;
            GameObject.DestroyImmediate(_currentGO);
            _ctx.ChangesMade = true;
            return new AssetEditSession(_ctx, parentGO);
        }

        /// <summary>
        /// 현재 GO에서 지정한 타입의 컴포넌트를 제거합니다.
        /// </summary>
        public AssetEditSession RemoveComp<T>() where T : Component
        {
            if (IsInvalid) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] RemoveComp<{typeof(T).Name}>: GO가 선택되지 않았습니다.");
                return this;
            }

            var comp = _currentGO.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogWarning(
                    $"[SindyEdit] RemoveComp<{typeof(T).Name}>: '{_currentGO.name}'에서 " +
                    $"{typeof(T).Name} 컴포넌트를 찾을 수 없습니다.");
                return this;
            }

            GameObject.DestroyImmediate(comp);
            _ctx.ChangesMade = true;
            return this;
        }

        /// <summary>
        /// 현재 GO의 컴포넌트를 SerializedObject로 편집합니다.
        /// 콜백 실행 후 즉시 ApplyModifiedPropertiesWithoutUndo()가 호출됩니다.
        /// </summary>
        /// <param name="action">편집 콜백. <see cref="ComponentEditScope"/>를 통해 프로퍼티를 편집하세요.</param>
        public AssetEditSession EditComp<T>(Action<ComponentEditScope> action) where T : Component
        {
            if (IsInvalid) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] EditComp<{typeof(T).Name}>: GO가 선택되지 않았습니다.");
                return this;
            }

            var comp = _currentGO.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogWarning(
                    $"[SindyEdit] EditComp<{typeof(T).Name}>: '{_currentGO.name}'에서 " +
                    $"{typeof(T).Name} 컴포넌트를 찾을 수 없습니다.");
                return this;
            }

            var so = GetOrCreateSO(comp);
            action(new ComponentEditScope(so));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            _ctx.ChangesMade = true;

            return this;
        }

        /// <summary>
        /// 지정한 타입 T의 n번째 컴포넌트를 읽기 전용 스코프로 접근합니다.
        /// </summary>
        /// <param name="action">읽기 콜백. <see cref="ComponentReadScope"/>를 통해 프로퍼티를 읽으세요.</param>
        /// <param name="index">동일 타입 컴포넌트 중 접근할 인덱스 (0부터 시작)</param>
        public AssetEditSession ReadComp<T>(Action<ComponentReadScope> action, int index = 0)
            where T : Component
        {
            if (IsInvalid) return this;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] ReadComp<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return this;
            }

            var so = GetCompSO<T>(index, $"ReadComp<{typeof(T).Name}>");
            if (so == null) return this;

            action(new ComponentReadScope(so));
            return this;
        }

        // ── SO* 세터 ──────────────────────────────────────────────────────────

        /// <summary>SerializedProperty stringValue 세터</summary>
        public AssetEditSession SOString(string prop, string value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.String)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: String, 실제: {sp.propertyType})");
                return;
            }
            sp.stringValue = value;
        });

        /// <summary>SerializedProperty intValue 세터</summary>
        public AssetEditSession SOInt(string prop, int value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Integer)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Integer, 실제: {sp.propertyType})");
                return;
            }
            sp.intValue = value;
        });

        /// <summary>SerializedProperty floatValue 세터</summary>
        public AssetEditSession SOFloat(string prop, float value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Float)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Float, 실제: {sp.propertyType})");
                return;
            }
            sp.floatValue = value;
        });

        /// <summary>SerializedProperty boolValue 세터</summary>
        public AssetEditSession SOBool(string prop, bool value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Boolean)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Boolean, 실제: {sp.propertyType})");
                return;
            }
            sp.boolValue = value;
        });

        /// <summary>SerializedProperty colorValue 세터</summary>
        public AssetEditSession SOColor(string prop, Color value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Color)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Color, 실제: {sp.propertyType})");
                return;
            }
            sp.colorValue = value;
        });

        /// <summary>SerializedProperty vector3Value 세터</summary>
        public AssetEditSession SOVector3(string prop, Vector3 value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Vector3)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Vector3, 실제: {sp.propertyType})");
                return;
            }
            sp.vector3Value = value;
        });

        /// <summary>SerializedProperty vector2Value 세터</summary>
        public AssetEditSession SOVector2(string prop, Vector2 value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.Vector2)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Vector2, 실제: {sp.propertyType})");
                return;
            }
            sp.vector2Value = value;
        });

        /// <summary>SerializedProperty objectReferenceValue 세터</summary>
        public AssetEditSession SORef(string prop, UnityEngine.Object value) => SetProperty(prop, sp =>
        {
            if (sp.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: ObjectReference, 실제: {sp.propertyType})");
                return;
            }
            sp.objectReferenceValue = value;
        });

        // ── 값 읽기 ───────────────────────────────────────────────────────────

        /// <summary>현재 타깃에서 float 프로퍼티 값을 읽습니다.</summary>
        public float GetFloat(string prop) => ReadProperty(prop, sp => sp.floatValue, 0f);

        /// <summary>현재 타깃에서 string 프로퍼티 값을 읽습니다.</summary>
        public string GetString(string prop) => ReadProperty(prop, sp => sp.stringValue, string.Empty);

        /// <summary>현재 타깃에서 int 프로퍼티 값을 읽습니다.</summary>
        public int GetInt(string prop) => ReadProperty(prop, sp => sp.intValue, 0);

        /// <summary>현재 타깃에서 bool 프로퍼티 값을 읽습니다.</summary>
        public bool GetBool(string prop) => ReadProperty(prop, sp => sp.boolValue, false);

        /// <summary>현재 타깃에서 Color 프로퍼티 값을 읽습니다.</summary>
        public Color GetColor(string prop) => ReadProperty(prop, sp => sp.colorValue, Color.clear);

        /// <summary>지정한 프로퍼티의 objectReferenceValue를 T 타입으로 반환합니다.</summary>
        public T GetRef<T>(string prop) where T : UnityEngine.Object =>
            ReadProperty(prop, sp =>
            {
                if (sp.propertyType != SerializedPropertyType.ObjectReference)
                {
                    Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: ObjectReference, 실제: {sp.propertyType})");
                    return null;
                }
                return sp.objectReferenceValue as T;
            }, null);

        // ── 타입 지정 읽기 오버로드 (컴포넌트 인덱스 지정) ───────────────────

        /// <summary>지정한 타입 T의 n번째 컴포넌트에서 float 프로퍼티 값을 읽습니다.</summary>
        public float GetFloat<T>(string prop, int index = 0) where T : Component
        {
            if (IsInvalid) return 0f;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetFloat<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return 0f;
            }
            var so = GetCompSO<T>(index, $"GetFloat<{typeof(T).Name}>");
            if (so == null) return 0f;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return 0f; }
            if (sp.propertyType != SerializedPropertyType.Float) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Float, 실제: {sp.propertyType})"); return 0f; }
            return sp.floatValue;
        }

        /// <summary>지정한 타입 T의 n번째 컴포넌트에서 string 프로퍼티 값을 읽습니다.</summary>
        public string GetString<T>(string prop, int index = 0) where T : Component
        {
            if (IsInvalid) return string.Empty;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetString<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return string.Empty;
            }
            var so = GetCompSO<T>(index, $"GetString<{typeof(T).Name}>");
            if (so == null) return string.Empty;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return string.Empty; }
            if (sp.propertyType != SerializedPropertyType.String) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: String, 실제: {sp.propertyType})"); return string.Empty; }
            return sp.stringValue;
        }

        /// <summary>지정한 타입 T의 n번째 컴포넌트에서 int 프로퍼티 값을 읽습니다.</summary>
        public int GetInt<T>(string prop, int index = 0) where T : Component
        {
            if (IsInvalid) return 0;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetInt<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return 0;
            }
            var so = GetCompSO<T>(index, $"GetInt<{typeof(T).Name}>");
            if (so == null) return 0;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return 0; }
            if (sp.propertyType != SerializedPropertyType.Integer) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Integer, 실제: {sp.propertyType})"); return 0; }
            return sp.intValue;
        }

        /// <summary>지정한 타입 T의 n번째 컴포넌트에서 bool 프로퍼티 값을 읽습니다.</summary>
        public bool GetBool<T>(string prop, int index = 0) where T : Component
        {
            if (IsInvalid) return false;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetBool<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return false;
            }
            var so = GetCompSO<T>(index, $"GetBool<{typeof(T).Name}>");
            if (so == null) return false;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return false; }
            if (sp.propertyType != SerializedPropertyType.Boolean) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Boolean, 실제: {sp.propertyType})"); return false; }
            return sp.boolValue;
        }

        /// <summary>지정한 타입 T의 n번째 컴포넌트에서 Color 프로퍼티 값을 읽습니다.</summary>
        public Color GetColor<T>(string prop, int index = 0) where T : Component
        {
            if (IsInvalid) return Color.clear;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetColor<{typeof(T).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return Color.clear;
            }
            var so = GetCompSO<T>(index, $"GetColor<{typeof(T).Name}>");
            if (so == null) return Color.clear;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return Color.clear; }
            if (sp.propertyType != SerializedPropertyType.Color) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: Color, 실제: {sp.propertyType})"); return Color.clear; }
            return sp.colorValue;
        }

        /// <summary>지정한 타입 TComp의 n번째 컴포넌트에서 ObjectReference 프로퍼티 값을 TRef 타입으로 읽습니다.</summary>
        public TRef GetRef<TComp, TRef>(string prop, int index = 0)
            where TComp : Component where TRef : UnityEngine.Object
        {
            if (IsInvalid) return null;
            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GetRef<{typeof(TComp).Name}, {typeof(TRef).Name}>: Scene/Prefab 모드에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return null;
            }
            var so = GetCompSO<TComp>(index, $"GetRef<{typeof(TComp).Name}, {typeof(TRef).Name}>");
            if (so == null) return null;
            var sp = so.FindProperty(prop);
            if (sp == null) { Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})"); return null; }
            if (sp.propertyType != SerializedPropertyType.ObjectReference) { Debug.LogWarning($"[SindyEdit] 타입 불일치: '{prop}' (기대: ObjectReference, 실제: {sp.propertyType})"); return null; }
            return sp.objectReferenceValue as TRef;
        }

        // ── 범용 Set ──────────────────────────────────────────────────────────

        /// <summary>
        /// 타입을 자동 판별하여 SerializedProperty를 설정합니다.
        /// <para>
        /// 지원 타입: string, bool, int, float, Color, Vector3, Vector2
        /// </para>
        /// </summary>
        public AssetEditSession Set(string prop, object value)
        {
            if (IsInvalid) return this;

            return value switch
            {
                string s => SOString(prop, s),
                bool b => SOBool(prop, b),
                Color c => SOColor(prop, c),
                Vector3 v3 => SOVector3(prop, v3),
                Vector2 v2 => SOVector2(prop, v2),
                int i => SetIntOrFloat(prop, i),
                float f => SOFloat(prop, f),
                null => LogAndReturn($"[SindyEdit] Set: value가 null입니다. prop={prop}"),
                _ => LogAndReturn($"[SindyEdit] Set: 지원하지 않는 타입 {value.GetType().Name}. prop={prop}"),
            };
        }

        // ── 저장 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// 현재까지의 변경사항을 디스크에 저장합니다.
        /// Dispose 시에도 자동 저장되므로 명시적으로 호출하지 않아도 됩니다.
        /// </summary>
        public void Save()
        {
            if (IsInvalid) return;
            _ctx.ApplyAll();
            _ctx.PersistToDisk();
        }

        /// <summary>
        /// 현재 .asset 파일을 디스크에서 삭제하고 세션을 무효화합니다.
        /// 미저장 변경사항은 폐기됩니다.
        /// </summary>
        public void DeleteAsset()
        {
            if (IsInvalid) return;

            if (_ctx.Mode != SessionContext.AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] DeleteAsset()는 .asset 파일에서만 사용할 수 있습니다. ({_ctx.AssetPath})");
                return;
            }

            _ctx.SoCache.Clear();
            _ctx.ChangesMade = false;
            AssetDatabase.DeleteAsset(_ctx.AssetPath);
            Debug.Log($"[SindyEdit] 에셋 삭제됨: {_ctx.AssetPath}");
            _ctx.Cleanup();
            if (_isOwner) _disposed = true;
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// 루트 세션(팩토리 생성)에서만 미저장 변경사항을 적용하고 디스크에 저장합니다.
        /// 탐색으로 파생된 세션의 Dispose는 no-op입니다.
        /// </summary>
        public void Dispose()
        {
            if (!_isOwner || _disposed) return;
            _disposed = true;

            _ctx.ApplyAll();
            _ctx.PersistToDisk();
            _ctx.Cleanup();
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        private AssetEditSession SetIntOrFloat(string prop, int value)
        {
            return SetProperty(prop, p =>
            {
                if (p.propertyType == SerializedPropertyType.Float)
                    p.floatValue = value;
                else
                    p.intValue = value;
            });
        }

        // Unity 내부 직렬화 프로퍼티 — 사용자가 실수로 넘겨도 건너뜀
        private static readonly HashSet<string> _internalProps = new()
        {
            "m_Script", "m_ObjectHideFlags", "m_PrefabInstance",
            "m_PrefabAsset", "m_CorrespondingSourceObject",
        };

        private AssetEditSession SetProperty(string prop, Action<SerializedProperty> setter)
        {
            if (IsInvalid)
            {
                Debug.LogWarning("[SindyEdit] 이미 정리된 세션입니다.");
                return this;
            }

            if (_internalProps.Contains(prop))
            {
                Debug.LogWarning($"[SindyEdit] '{prop}'은 Unity 내부 프로퍼티로 편집할 수 없습니다.");
                return this;
            }

            var sp = FindProperty(prop, out _);
            if (sp == null) return this;

            setter(sp);
            _ctx.ChangesMade = true;
            return this;
        }

        /// <summary>
        /// 현재 타깃(GO 모드: 모든 컴포넌트 순회 / Asset 모드: SO 직접 탐색)에서
        /// SerializedProperty를 찾아 반환합니다.
        /// </summary>
        private SerializedProperty FindProperty(string prop, out SerializedObject owner)
        {
            owner = null;

            if (_ctx.Mode == SessionContext.AssetMode.Asset)
            {
                if (_ctx.SoAsset == null)
                {
                    Debug.LogWarning($"[SindyEdit] SO 에셋이 null입니다. prop={prop}");
                    return null;
                }
                owner = GetOrCreateSO(_ctx.SoAsset);
                var p = owner.FindProperty(prop);
                if (p == null)
                    Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_ctx.AssetPath})");
                return p;
            }

            // Scene / Prefab 모드
            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] GO가 선택되지 않았습니다. GO()를 먼저 호출하세요. prop={prop}");
                return null;
            }

            // 모든 컴포넌트를 순회하여 프로퍼티를 가진 컴포넌트를 찾음
            foreach (var comp in _currentGO.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = GetOrCreateSO(comp);
                var p = so.FindProperty(prop);
                if (p != null)
                {
                    owner = so;
                    return p;
                }
            }

            Debug.LogWarning(
                $"[SindyEdit] Property '{prop}'을 '{_currentGO.name}'의 어떤 컴포넌트에서도 찾을 수 없습니다.\n" +
                $"힌트: FieldPeeker(Sindy/Tools/Field Peeker)로 정확한 직렬화 경로를 확인하세요.");
            return null;
        }

        private SerializedObject GetOrCreateSO(UnityEngine.Object target)
        {
            if (!_ctx.SoCache.TryGetValue(target, out var so))
            {
                so = new SerializedObject(target);
                so.Update();
                _ctx.SoCache[target] = so;
            }
            return so;
        }

        private TVal ReadProperty<TVal>(string prop, Func<SerializedProperty, TVal> getter, TVal fallback)
        {
            var sp = FindProperty(prop, out _);
            return sp != null ? getter(sp) : fallback;
        }

        private AssetEditSession LogAndReturn(string msg)
        {
            Debug.LogWarning(msg);
            return this;
        }

        /// <summary>'/' 구분자를 GOEditor 호환 '.' 구분자로 변환합니다.</summary>
        private static string NormalizePath(string path) => path?.Replace('/', '.');

        /// <summary>Transform 계층을 재귀 탐색하여 이름이 일치하는 GameObject를 찾습니다.</summary>
        private static GameObject FindRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindRecursive(parent.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }

        private SerializedObject GetCompSO<T>(int index, string context) where T : Component
        {
            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] GetCompSO<{typeof(T).Name}>: GO가 선택되지 않았습니다. ({context})");
                return null;
            }
            var comps = _currentGO.GetComponents<T>();
            if (index < 0 || index >= comps.Length)
            {
                Debug.LogWarning($"[SindyEdit] {typeof(T).Name}[{index}] 범위 초과. 총 {comps.Length}개. ({context})");
                return null;
            }
            return GetOrCreateSO(comps[index]);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ComponentEditScope
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="AssetEditSession.EditComp{T}"/> 콜백에서 사용하는 컴포넌트 편집 컨텍스트.
    /// <para>
    /// 특정 컴포넌트의 SerializedObject에 직접 접근하여 프로퍼티를 편집합니다.
    /// 콜백 종료 후 자동으로 ApplyModifiedPropertiesWithoutUndo()가 호출됩니다.
    /// </para>
    /// <example>
    /// <code>
    /// session.GO("Player").EditComp&lt;Image&gt;(img =>
    /// {
    ///     img.Set("m_Color", new Color(1, 0, 0, 1));
    /// });
    /// </code>
    /// </example>
    /// </summary>
    public sealed class ComponentEditScope
    {
        private readonly SerializedObject _so;

        internal ComponentEditScope(SerializedObject so) => _so = so;

        /// <summary>
        /// 타입을 자동 판별하여 SerializedProperty를 설정합니다.
        /// <para>지원 타입: string, bool, int, float, Color, Vector3, Vector2</para>
        /// </summary>
        public ComponentEditScope Set(string prop, object value)
        {
            var sp = _so.FindProperty(prop);
            if (sp == null)
            {
                Debug.LogWarning($"[SindyEdit] ComponentEditScope: '{prop}' 프로퍼티를 찾을 수 없습니다.");
                return this;
            }

            switch (value)
            {
                case string s: sp.stringValue = s; break;
                case bool b: sp.boolValue = b; break;
                case Color c: sp.colorValue = c; break;
                case Vector3 v3: sp.vector3Value = v3; break;
                case Vector2 v2: sp.vector2Value = v2; break;
                case int i:
                    if (sp.propertyType == SerializedPropertyType.Float)
                        sp.floatValue = i;
                    else
                        sp.intValue = i;
                    break;
                case float f: sp.floatValue = f; break;
                case null:
                    Debug.LogWarning($"[SindyEdit] ComponentEditScope.Set: null 값. prop={prop}");
                    break;
                default:
                    Debug.LogWarning(
                        $"[SindyEdit] ComponentEditScope.Set: 지원하지 않는 타입 " +
                        $"{value.GetType().Name}. prop={prop}");
                    break;
            }

            return this;
        }

        /// <summary>SerializedProperty objectReferenceValue 세터</summary>
        public ComponentEditScope SORef(string prop, UnityEngine.Object value)
        {
            var sp = _so.FindProperty(prop);
            if (sp == null)
            {
                Debug.LogWarning($"[SindyEdit] ComponentEditScope: '{prop}' 프로퍼티를 찾을 수 없습니다.");
                return this;
            }
            if (sp.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning($"[SindyEdit] ComponentEditScope: '{prop}' 타입 불일치 (기대: ObjectReference, 실제: {sp.propertyType})");
                return this;
            }
            sp.objectReferenceValue = value;
            return this;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ComponentReadScope
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="AssetEditSession.ReadComp{T}"/> 콜백에서 사용하는 컴포넌트 읽기 컨텍스트.
    /// <para>
    /// 특정 컴포넌트의 SerializedObject에서 프로퍼티 값을 읽습니다.
    /// </para>
    /// <example>
    /// <code>
    /// session.GO("Player").ReadComp&lt;Image&gt;(img =>
    /// {
    ///     var color = img.GetColor("m_Color");
    ///     var label = img.GetString("m_text");
    /// });
    /// </code>
    /// </example>
    /// </summary>
    public sealed class ComponentReadScope
    {
        private readonly SerializedObject _so;

        internal ComponentReadScope(SerializedObject so) => _so = so;

        private TVal Read<TVal>(string prop, SerializedPropertyType expected, Func<SerializedProperty, TVal> getter, TVal fallback)
        {
            var sp = _so.FindProperty(prop);
            if (sp == null)
            {
                Debug.LogWarning($"[SindyEdit] ComponentReadScope: '{prop}' 프로퍼티를 찾을 수 없습니다.");
                return fallback;
            }
            if (sp.propertyType != expected)
            {
                Debug.LogWarning($"[SindyEdit] ComponentReadScope: '{prop}' 타입 불일치 (기대: {expected}, 실제: {sp.propertyType})");
                return fallback;
            }
            return getter(sp);
        }

        /// <summary>float 프로퍼티 값을 읽습니다.</summary>
        public float GetFloat(string prop) =>
            Read(prop, SerializedPropertyType.Float, sp => sp.floatValue, 0f);

        /// <summary>string 프로퍼티 값을 읽습니다.</summary>
        public string GetString(string prop) =>
            Read(prop, SerializedPropertyType.String, sp => sp.stringValue, string.Empty);

        /// <summary>int 프로퍼티 값을 읽습니다.</summary>
        public int GetInt(string prop) =>
            Read(prop, SerializedPropertyType.Integer, sp => sp.intValue, 0);

        /// <summary>bool 프로퍼티 값을 읽습니다.</summary>
        public bool GetBool(string prop) =>
            Read(prop, SerializedPropertyType.Boolean, sp => sp.boolValue, false);

        /// <summary>Color 프로퍼티 값을 읽습니다.</summary>
        public Color GetColor(string prop) =>
            Read(prop, SerializedPropertyType.Color, sp => sp.colorValue, Color.clear);

        /// <summary>ObjectReference 프로퍼티 값을 T 타입으로 읽습니다.</summary>
        public T GetRef<T>(string prop) where T : UnityEngine.Object
        {
            var sp = _so.FindProperty(prop);
            if (sp == null)
            {
                Debug.LogWarning($"[SindyEdit] ComponentReadScope: '{prop}' 프로퍼티를 찾을 수 없습니다.");
                return null;
            }
            if (sp.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogWarning($"[SindyEdit] ComponentReadScope: '{prop}' 타입 불일치 (기대: ObjectReference, 실제: {sp.propertyType})");
                return null;
            }
            return sp.objectReferenceValue as T;
        }
    }
}
#endif
