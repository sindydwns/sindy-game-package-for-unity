#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
    /// // 이름으로 자동 탐색
    /// using var s = SindyEdit.Find("GaugeBar");
    /// s.GO("Fill/Image").SOColor("m_Color", Color.green);
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
                ".unity"  => AssetEditSession.ForScene(assetPath),
                ".prefab" => AssetEditSession.ForPrefab(assetPath),
                _         => AssetEditSession.ForAsset(assetPath),
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

        private readonly AssetMode          _mode;
        private readonly string             _assetPath;

        // 위임 객체 (모드별로 하나만 사용)
        private readonly SceneEditor        _sceneEditor;
        private readonly PrefabEditor       _prefabEditor;
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
            _mode        = mode;
            _assetPath   = path;
            _sceneEditor = se;
            _prefabEditor = pe;
            _soAsset     = soAsset;
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

        // ── SO* 세터 ──────────────────────────────────────────────────────────

        /// <summary>SerializedProperty stringValue 세터</summary>
        public AssetEditSession SOString(string prop, string value)
            => SetProperty(prop, p => p.stringValue = value);

        /// <summary>SerializedProperty intValue 세터</summary>
        public AssetEditSession SOInt(string prop, int value)
            => SetProperty(prop, p => p.intValue = value);

        /// <summary>SerializedProperty floatValue 세터</summary>
        public AssetEditSession SOFloat(string prop, float value)
            => SetProperty(prop, p => p.floatValue = value);

        /// <summary>SerializedProperty boolValue 세터</summary>
        public AssetEditSession SOBool(string prop, bool value)
            => SetProperty(prop, p => p.boolValue = value);

        /// <summary>SerializedProperty colorValue 세터</summary>
        public AssetEditSession SOColor(string prop, Color value)
            => SetProperty(prop, p => p.colorValue = value);

        /// <summary>SerializedProperty vector3Value 세터</summary>
        public AssetEditSession SOVector3(string prop, Vector3 value)
            => SetProperty(prop, p => p.vector3Value = value);

        /// <summary>SerializedProperty vector2Value 세터</summary>
        public AssetEditSession SOVector2(string prop, Vector2 value)
            => SetProperty(prop, p => p.vector2Value = value);

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
                string s   => SOString(prop, s),
                bool b     => SOBool(prop, b),
                Color c    => SOColor(prop, c),
                Vector3 v3 => SOVector3(prop, v3),
                Vector2 v2 => SOVector2(prop, v2),
                int i      => SetIntOrFloat(prop, i),
                float f    => SOFloat(prop, f),
                null       => LogAndReturn($"[SindyEdit] Set: value가 null입니다. prop={prop}"),
                _          => LogAndReturn($"[SindyEdit] Set: 지원하지 않는 타입 {value.GetType().Name}. prop={prop}"),
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

        private AssetEditSession SetProperty(string prop, Action<SerializedProperty> setter)
        {
            if (_disposed)
            {
                Debug.LogWarning("[SindyEdit] 이미 Dispose된 세션입니다.");
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
                var p  = so.FindProperty(prop);
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
    }
}
#endif
