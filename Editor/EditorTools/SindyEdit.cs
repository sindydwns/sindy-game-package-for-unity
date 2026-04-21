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
    /// using var s = SindyEdit.Create<FloatVariable>("Assets/Data/Speed.asset");
    /// s.SOFloat("value", 5f);
    ///
    /// // 이름으로 자동 탐색
    /// using var s = SindyEdit.Find("GaugeBar");
    /// s.GOFind("Fill").WithComp<Image>(img => img.Set("m_Color", Color.green));
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
    /// using 블록 종료(Dispose) 시 미저장 변경사항을 자동으로 저장합니다.
    /// 명시적으로 <see cref="Save"/>를 호출할 수도 있습니다.
    /// </para>
    /// </summary>
    public sealed class AssetEditSession : IDisposable
    {
        // ── 내부 타입 ─────────────────────────────────────────────────────────

        private enum AssetMode { Scene, Prefab, Asset }

        // ── 상태 ──────────────────────────────────────────────────────────────

        private readonly AssetMode _mode;
        private readonly string _assetPath;

        // 위임 객체 (모드별로 하나만 사용)
        private readonly SceneEditor _sceneEditor;
        private readonly PrefabEditor _prefabEditor;
        private readonly UnityEngine.Object _soAsset;

        // 현재 탐색 중인 GO (Scene / Prefab 모드)
        private GameObject _currentGO;

        // 수정된 SerializedObject 캐시: targetObject → SerializedObject
        private readonly Dictionary<UnityEngine.Object, SerializedObject> _soCache = new();

        private bool _changesMade;
        private bool _disposed;

        // ── 생성자 ────────────────────────────────────────────────────────────

        private AssetEditSession(
            AssetMode mode, string path,
            SceneEditor se = null, PrefabEditor pe = null, UnityEngine.Object soAsset = null)
        {
            _mode = mode;
            _assetPath = path;
            _sceneEditor = se;
            _prefabEditor = pe;
            _soAsset = soAsset;
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
            return new AssetEditSession(AssetMode.Scene, path, se: se);
        }

        internal static AssetEditSession ForPrefab(string path)
        {
            var pe = PrefabEditor.Open(path);
            if (pe == null)
            {
                Debug.LogError($"[SindyEdit] 프리팹을 열 수 없습니다: {path}");
                return null;
            }
            return new AssetEditSession(AssetMode.Prefab, path, pe: pe);
        }

        internal static AssetEditSession ForAsset(string path)
        {
            // ScriptableObject 우선, 실패 시 일반 Object로 폴백
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)
                                    ?? AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                Debug.LogError($"[SindyEdit] 에셋을 로드할 수 없습니다: {path}");
                return null;
            }
            return new AssetEditSession(AssetMode.Asset, path, soAsset: asset);
        }

        // ── GO 탐색 ───────────────────────────────────────────────────────────

        /// <summary>
        /// '/' 또는 '.' 구분자로 지정한 계층 경로에서 GameObject를 탐색합니다.
        /// <para>
        /// 씬: 씬 루트 기준 경로 (예: "Canvas/Panel/Title")<br/>
        /// 프리팹: 프리팹 루트의 자식 기준 경로 (예: "Fill/Image")<br/>
        /// .asset: 경고만 출력하고 무시됩니다.
        /// </para>
        /// </summary>
        /// <param name="goPath">계층 경로. '/' 또는 '.' 둘 다 구분자로 허용.</param>
        public AssetEditSession GO(string goPath)
        {
            if (_disposed) return this;

            _currentGO = null;

            if (_mode == AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GO()는 .asset 파일에서 사용할 수 없습니다. ({_assetPath})");
                return this;
            }

            string normalized = NormalizePath(goPath);
            GOEditor goEditor = null;

            if (_mode == AssetMode.Scene)
                goEditor = GOEditor.FindOnly(_sceneEditor.Scene, normalized);
            else if (_mode == AssetMode.Prefab && _prefabEditor?.RootObject != null)
                goEditor = GOEditor.FindOnly(_prefabEditor.RootObject.transform, normalized);

            if (goEditor == null)
            {
                Debug.LogWarning($"[SindyEdit] GO를 찾을 수 없습니다: {goPath} (에셋: {_assetPath})");
                return this;
            }

            _currentGO = goEditor.GameObject;
            return this;
        }

        /// <summary>
        /// 씬의 첫 번째 루트 GO 또는 프리팹 루트 GO로 이동합니다.
        /// </summary>
        public AssetEditSession Root()
        {
            if (_disposed) return this;

            _currentGO = null;

            if (_mode == AssetMode.Scene)
            {
                var roots = _sceneEditor.Scene.GetRootGameObjects();
                if (roots.Length == 0)
                    Debug.LogWarning($"[SindyEdit] 씬에 루트 GO가 없습니다. ({_assetPath})");
                else
                    _currentGO = roots[0];
            }
            else if (_mode == AssetMode.Prefab)
            {
                _currentGO = _prefabEditor?.RootObject;
                if (_currentGO == null)
                    Debug.LogWarning($"[SindyEdit] 프리팹 루트 GO가 null입니다. ({_assetPath})");
            }
            else
            {
                Debug.LogWarning($"[SindyEdit] Root()는 .asset 파일에서 사용할 수 없습니다. ({_assetPath})");
            }

            return this;
        }

        /// <summary>
        /// 이름으로 GO를 재귀 탐색합니다. 계층 어디에 있든 이름으로 찾습니다.
        /// <para>
        /// 씬: 모든 루트 GO를 기준으로 재귀 탐색<br/>
        /// 프리팹: 프리팹 루트를 기준으로 재귀 탐색
        /// </para>
        /// </summary>
        /// <param name="name">탐색할 GO 이름 (정확히 일치)</param>
        public AssetEditSession GOFind(string name)
        {
            if (_disposed) return this;

            _currentGO = null;

            if (_mode == AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] GOFind()는 .asset 파일에서 사용할 수 없습니다. ({_assetPath})");
                return this;
            }

            if (_mode == AssetMode.Scene)
            {
                foreach (var root in _sceneEditor.Scene.GetRootGameObjects())
                {
                    _currentGO = FindRecursive(root.transform, name);
                    if (_currentGO != null) break;
                }
            }
            else if (_mode == AssetMode.Prefab && _prefabEditor?.RootObject != null)
            {
                _currentGO = FindRecursive(_prefabEditor.RootObject.transform, name);
            }

            if (_currentGO == null)
                Debug.LogWarning($"[SindyEdit] GOFind: '{name}'을 찾을 수 없습니다. ({_assetPath})");

            return this;
        }

        // ── GO 신규 생성 ──────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 GO의 자식으로 새 GameObject를 생성합니다.
        /// <para>
        /// _currentGO가 null이면 씬/프리팹 루트에 생성합니다.<br/>
        /// 씬 모드: SceneManager.MoveGameObjectToScene으로 씬에 배치<br/>
        /// 프리팹 모드: 프리팹 루트 또는 현재 GO의 자식으로 배치<br/>
        /// .asset 모드: 경고 출력 후 무시됩니다.
        /// </para>
        /// <para>생성 후 탐색 컨텍스트가 새 GO로 이동하므로 체이닝이 계속 가능합니다.</para>
        /// </summary>
        /// <param name="name">생성할 GameObject 이름</param>
        public AssetEditSession CreateGO(string name)
        {
            if (_disposed) return this;

            if (_mode == AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] CreateGO()는 .asset 파일에서 사용할 수 없습니다. ({_assetPath})");
                return this;
            }

            var newGO = new GameObject(name);

            if (_mode == AssetMode.Scene)
            {
                if (_currentGO != null)
                    newGO.transform.SetParent(_currentGO.transform, false);
                else
                    SceneManager.MoveGameObjectToScene(newGO, _sceneEditor.Scene);
            }
            else // Prefab
            {
                var parent = _currentGO != null ? _currentGO.transform : _prefabEditor.RootObject.transform;
                newGO.transform.SetParent(parent, false);
            }

            _currentGO = newGO;
            _changesMade = true;
            Debug.Log($"[SindyEdit] GO 생성됨: '{name}' (에셋: {_assetPath})");
            return this;
        }

        // ── Child 탐색 ────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 GO의 인덱스로 직계 자식 GO로 이동합니다.
        /// </summary>
        /// <param name="index">자식 인덱스 (0부터 시작)</param>
        public AssetEditSession Child(int index)
        {
            if (_disposed) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] Child({index}): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return this;
            }

            if (index < 0 || index >= _currentGO.transform.childCount)
            {
                Debug.LogWarning(
                    $"[SindyEdit] Child({index}): 인덱스 범위를 벗어났습니다. " +
                    $"('{_currentGO.name}' 자식 수: {_currentGO.transform.childCount})");
                return this;
            }

            _currentGO = _currentGO.transform.GetChild(index).gameObject;
            return this;
        }

        /// <summary>
        /// 현재 GO의 직계 자식 중 이름이 일치하는 GO로 이동합니다.
        /// </summary>
        /// <param name="name">직계 자식 GO 이름</param>
        public AssetEditSession Child(string name)
        {
            if (_disposed) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] Child('{name}'): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return this;
            }

            var child = _currentGO.transform.Find(name);
            if (child == null)
            {
                Debug.LogWarning(
                    $"[SindyEdit] Child('{name}'): '{_currentGO.name}'의 직계 자식에서 찾을 수 없습니다.");
                return this;
            }

            _currentGO = child.gameObject;
            return this;
        }

        // ── 컴포넌트 접근 및 추가 ────────────────────────────────────────────

        /// <summary>
        /// 현재 GO에서 컴포넌트를 가져옵니다. 없으면 null을 반환합니다.
        /// </summary>
        public T GetComp<T>() where T : Component
        {
            if (_disposed || _currentGO == null) return null;
            return _currentGO.GetComponent<T>();
        }

        /// <summary>
        /// 현재 GO에 컴포넌트가 없을 때만 추가합니다.
        /// 추가 시 Undo에 등록됩니다.
        /// </summary>
        public AssetEditSession AddComp<T>() where T : Component
        {
            if (_disposed) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] AddComp<{typeof(T).Name}>: GO가 선택되지 않았습니다.");
                return this;
            }

            if (_currentGO.GetComponent<T>() == null)
            {
                Undo.AddComponent<T>(_currentGO);
                _changesMade = true;
                Debug.Log($"[SindyEdit] 컴포넌트 추가됨: {typeof(T).Name} on '{_currentGO.name}'");
            }

            return this;
        }

        /// <summary>
        /// 현재 컨텍스트 GO를 제거합니다.
        /// 부모 GO가 있으면 부모로 컨텍스트를 이동하고, 없으면 null로 설정합니다.
        /// </summary>
        public AssetEditSession DeleteGO()
        {
            if (_disposed) return this;

            if (_mode == AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] DeleteGO()는 .asset 파일에서 사용할 수 없습니다. ({_assetPath})");
                return this;
            }

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] DeleteGO(): GO가 선택되지 않았습니다. GO()를 먼저 호출하세요.");
                return this;
            }

            var parent = _currentGO.transform.parent?.gameObject;
            GameObject.DestroyImmediate(_currentGO);
            _currentGO = parent;
            _changesMade = true;
            return this;
        }

        /// <summary>
        /// 현재 GO에서 지정한 타입의 컴포넌트를 제거합니다.
        /// </summary>
        public AssetEditSession RemoveComp<T>() where T : Component
        {
            if (_disposed) return this;

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
            _changesMade = true;
            return this;
        }

        /// <summary>
        /// 현재 GO의 컴포넌트를 SerializedObject로 편집합니다.
        /// 콜백 실행 후 즉시 ApplyModifiedPropertiesWithoutUndo()가 호출됩니다.
        /// </summary>
        /// <param name="action">편집 콜백. <see cref="ComponentEditScope"/>를 통해 프로퍼티를 편집하세요.</param>
        public AssetEditSession WithComp<T>(Action<ComponentEditScope> action) where T : Component
        {
            if (_disposed) return this;

            if (_currentGO == null)
            {
                Debug.LogWarning($"[SindyEdit] WithComp<{typeof(T).Name}>: GO가 선택되지 않았습니다.");
                return this;
            }

            var comp = _currentGO.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogWarning(
                    $"[SindyEdit] WithComp<{typeof(T).Name}>: '{_currentGO.name}'에서 " +
                    $"{typeof(T).Name} 컴포넌트를 찾을 수 없습니다.");
                return this;
            }

            var so = GetOrCreateSO(comp);
            action(new ComponentEditScope(so));
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(comp);
            _changesMade = true;

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

        // ── 범용 Set ──────────────────────────────────────────────────────────

        /// <summary>
        /// 타입을 자동 판별하여 SerializedProperty를 설정합니다.
        /// HTTP IPC의 <c>/edit</c> 엔드포인트에서 주로 사용됩니다.
        /// <para>
        /// 지원 타입: string, bool, int, float, Color, Vector3, Vector2
        /// </para>
        /// </summary>
        public AssetEditSession Set(string prop, object value)
        {
            if (_disposed) return this;

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
            if (_disposed) return;
            ApplyAll();
            PersistToDisk();
        }

        /// <summary>
        /// 현재 .asset 파일을 디스크에서 삭제하고 세션을 무효화합니다.
        /// 미저장 변경사항은 폐기됩니다.
        /// </summary>
        public void DeleteAsset()
        {
            if (_disposed) return;

            if (_mode != AssetMode.Asset)
            {
                Debug.LogWarning($"[SindyEdit] DeleteAsset()는 .asset 파일에서만 사용할 수 있습니다. ({_assetPath})");
                return;
            }

            _soCache.Clear();
            _changesMade = false;
            AssetDatabase.DeleteAsset(_assetPath);
            Debug.Log($"[SindyEdit] 에셋 삭제됨: {_assetPath}");
            Dispose();
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// using 블록 종료 시 자동으로 호출됩니다.
        /// 미저장 변경사항을 적용하고 디스크에 저장한 뒤 내부 리소스를 정리합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 미적용 변경사항 반영
            ApplyAll();

            // 변경사항이 있으면 저장
            PersistToDisk();

            // 내부 리소스 정리
            switch (_mode)
            {
                case AssetMode.Scene:
                    // SceneEditor.Dispose()는 씬을 닫지 않으므로 호출해도 안전
                    // MarkDirty가 호출된 경우 SaveScene을 한 번 더 수행함
                    _sceneEditor?.Dispose();
                    break;
                case AssetMode.Prefab:
                    // PrefabEditor.Dispose()는 SaveAsPrefabAsset + UnloadPrefabContents
                    // PersistToDisk에서 이미 저장했으므로 중복 저장이 발생하지만 무해함
                    _prefabEditor?.Dispose();
                    break;
            }
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
            if (_disposed)
            {
                Debug.LogWarning("[SindyEdit] 이미 Dispose된 세션입니다.");
                return this;
            }

            if (_internalProps.Contains(prop))
            {
                Debug.LogWarning($"[SindyEdit] '{prop}'은 Unity 내부 프로퍼티로 편집할 수 없습니다.");
                return this;
            }

            var sp = FindProperty(prop, out var owner);
            if (sp == null) return this;

            setter(sp);
            _changesMade = true;
            return this;
        }

        /// <summary>
        /// 현재 타깃(GO 모드: 모든 컴포넌트 순회 / Asset 모드: SO 직접 탐색)에서
        /// SerializedProperty를 찾아 반환합니다.
        /// </summary>
        private SerializedProperty FindProperty(string prop, out SerializedObject owner)
        {
            owner = null;

            if (_mode == AssetMode.Asset)
            {
                if (_soAsset == null)
                {
                    Debug.LogWarning($"[SindyEdit] SO 에셋이 null입니다. prop={prop}");
                    return null;
                }
                owner = GetOrCreateSO(_soAsset);
                var p = owner.FindProperty(prop);
                if (p == null)
                    Debug.LogWarning($"[SindyEdit] Property '{prop}'을 찾을 수 없습니다. ({_assetPath})");
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
            if (!_soCache.TryGetValue(target, out var so))
            {
                so = new SerializedObject(target);
                so.Update();
                _soCache[target] = so;
            }
            return so;
        }

        private TVal ReadProperty<TVal>(string prop, Func<SerializedProperty, TVal> getter, TVal fallback)
        {
            var sp = FindProperty(prop, out _);
            return sp != null ? getter(sp) : fallback;
        }

        /// <summary>캐시에 있는 모든 SerializedObject의 변경사항을 반영합니다.</summary>
        private void ApplyAll()
        {
            foreach (var kvp in _soCache)
            {
                if (kvp.Value.hasModifiedProperties)
                {
                    kvp.Value.ApplyModifiedProperties();
                    EditorUtility.SetDirty(kvp.Key);
                }
            }
        }

        /// <summary>변경사항을 디스크에 저장합니다.</summary>
        private void PersistToDisk()
        {
            if (!_changesMade) return;

            switch (_mode)
            {
                case AssetMode.Scene:
                    if (_sceneEditor != null)
                    {
                        _sceneEditor.MarkDirty();
                        EditorSceneManager.SaveScene(_sceneEditor.Scene);
                        Debug.Log($"[SindyEdit] 씬 저장됨: {_assetPath}");
                    }
                    break;

                case AssetMode.Prefab:
                    if (_prefabEditor?.RootObject != null)
                    {
                        PrefabUtility.SaveAsPrefabAsset(_prefabEditor.RootObject, _assetPath);
                        Debug.Log($"[SindyEdit] 프리팹 저장됨: {_assetPath}");
                    }
                    break;

                case AssetMode.Asset:
                    if (_soAsset != null)
                    {
                        AssetDatabase.SaveAssets();
                        Debug.Log($"[SindyEdit] 에셋 저장됨: {_assetPath}");
                    }
                    break;
            }
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
    }

    // ────────────────────────────────────────────────────────────────────────────
    // ComponentEditScope
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="AssetEditSession.WithComp{T}"/> 콜백에서 사용하는 컴포넌트 편집 컨텍스트.
    /// <para>
    /// 특정 컴포넌트의 SerializedObject에 직접 접근하여 프로퍼티를 편집합니다.
    /// 콜백 종료 후 자동으로 ApplyModifiedPropertiesWithoutUndo()가 호출됩니다.
    /// </para>
    /// <example>
    /// <code>
    /// session.GO("Player").WithComp<Image>(img =>
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
}
#endif
