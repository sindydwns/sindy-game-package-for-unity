#if UNITY_EDITOR
using System;
using System.Runtime.ExceptionServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// GameObject와 컴포넌트를 메서드 체이닝으로 편집하는 빌더.
    /// <para>
    /// <see cref="AddComp{T}"/> 또는 <see cref="WithComp{T}"/> 로 컴포넌트를 타게팅한 후
    /// <c>SO*</c> 세터로 필드를 설정하고, 마지막에 <see cref="Apply"/>를 호출합니다.
    /// </para>
    /// <para>
    /// IDisposable을 구현하므로 using 패턴을 사용할 수 있습니다.
    /// Apply() 없이 Dispose되면 미저장 변경사항에 대해 LogWarning이 출력됩니다.
    /// </para>
    /// <example>
    /// <code>
    /// // 기본 패턴
    /// ctx.GO("ShowcaseRunner")
    ///     .AddComp<ShowcaseRunner>()
    ///     .SOFloat("cellWidth", 240f)
    ///     .SOColor("bgColor", Color.black)
    ///     .Apply();
    ///
    /// // using 패턴 (Apply 누락 시 경고)
    /// using var editor = ctx.GO("Canvas").AddComp<CanvasGroup>();
    /// editor.SOFloat("m_Alpha", 0.8f);
    /// editor.Apply();
    ///
    /// // null-safe 탐색 패턴
    /// ctx.GOFind("Canvas.Panel.Button")
    ///    ?.WithComp<Image>()
    ///    .SOColor("m_Color", Color.red)
    ///    .Apply();
    /// </code>
    /// </example>
    /// </summary>
    public sealed class GOEditor : IDisposable
    {
        private readonly GameObject _go;
        private SerializedObject _so;
        private bool _disposed;

        // ── 프로퍼티 ──────────────────────────────────────────────────────────

        public GameObject GameObject => _go;

        // ── 생성자 (내부 전용) ────────────────────────────────────────────────

        private GOEditor(GameObject go)
        {
            _go = go;
        }

        // ── 내부 팩토리: SceneEditor / PrefabEditor / Child() 에서만 사용 ─────

        /// <summary>씬 루트 기준 경로로 GO를 탐색하거나 생성합니다.</summary>
        internal static GOEditor GetOrCreate(Scene scene, string path)
        {
            var parts = SplitPath(path);
            var current = FindOrCreateRootGO(scene, parts[0]);
            for (int i = 1; i < parts.Length; i++)
                current = FindOrCreateChildGO(current.transform, parts[i]);
            return new GOEditor(current);
        }

        /// <summary>씬 루트 기준 경로로 GO를 탐색합니다. 없으면 null 반환.</summary>
        internal static GOEditor FindOnly(Scene scene, string path)
        {
            var parts = SplitPath(path);
            var current = FindRootGO(scene, parts[0]);
            if (current == null)
            {
                Debug.LogWarning($"[GOEditor] GOFind 실패: 씬 루트에서 '{parts[0]}'를 찾을 수 없습니다. (경로: {path})");
                return null;
            }
            for (int i = 1; i < parts.Length; i++)
            {
                var child = current.transform.Find(parts[i]);
                if (child == null)
                {
                    string reached = string.Join(".", parts[..i]);
                    Debug.LogWarning(
                        $"[GOEditor] GOFind 실패: '{reached}' 까지는 찾았으나 '{parts[i]}'를 찾을 수 없습니다. (전체 경로: {path})");
                    return null;
                }
                current = child.gameObject;
            }
            return new GOEditor(current);
        }

        /// <summary>Transform 기준 상대 경로로 GO를 탐색하거나 생성합니다.</summary>
        internal static GOEditor GetOrCreate(Transform parent, string path)
        {
            var parts = SplitPath(path);
            var current = parent;
            foreach (var part in parts)
                current = FindOrCreateChildGO(current, part).transform;
            return new GOEditor(current.gameObject);
        }

        /// <summary>Transform 기준 상대 경로로 GO를 탐색합니다. 없으면 null 반환.</summary>
        internal static GOEditor FindOnly(Transform parent, string path)
        {
            var parts = SplitPath(path);
            var current = parent;
            for (int i = 0; i < parts.Length; i++)
            {
                var child = current.Find(parts[i]);
                if (child == null)
                {
                    string reached = i == 0 ? parent.name : string.Join(".", parts[..i]);
                    Debug.LogWarning(
                        $"[GOEditor] GOFind 실패: '{reached}' 에서 '{parts[i]}'를 찾을 수 없습니다. (전체 경로: {path})");
                    return null;
                }
                current = child;
            }
            return new GOEditor(current.gameObject);
        }

        /// <summary>기존 GameObject를 직접 래핑합니다. (PrefabEditor.Root() 용)</summary>
        internal static GOEditor For(GameObject go)
        {
            if (go == null) throw new ArgumentNullException(nameof(go));
            return new GOEditor(go);
        }

        // ── 계층 이동 ─────────────────────────────────────────────────────────

        /// <summary>
        /// 현재 GO 기준 상대 경로로 자식 GO를 탐색하거나 생성합니다.
        /// <para>(Hierarchy path) GO() 와 달리 씬/프리팹 루트가 아니라 현재 GO 기준입니다.</para>
        /// </summary>
        public GOEditor Child(string relativePath)
            => GetOrCreate(_go.transform, relativePath);

        /// <summary>
        /// 현재 GO 기준 상대 경로로 자식 GO를 탐색합니다. 없으면 null 반환.
        /// </summary>
        public GOEditor ChildFind(string relativePath)
            => FindOnly(_go.transform, relativePath);

        // ── 컴포넌트 타게팅 ───────────────────────────────────────────────────

        /// <summary>
        /// 컴포넌트가 없으면 추가하고, 있으면 재사용합니다.
        /// 이후 SO* 호출의 대상이 이 컴포넌트로 설정됩니다.
        /// 추가 시 Undo에 등록됩니다.
        /// </summary>
        public GOEditor AddComp<T>() where T : Component
        {
            var comp = _go.GetComponent<T>();
            if (comp == null)
            {
                comp = Undo.AddComponent<T>(_go);
                Debug.Log($"[GOEditor] 컴포넌트 추가됨: {typeof(T).Name} on '{_go.name}'");
            }
            SetTarget(comp);
            return this;
        }

        /// <summary>
        /// 기존 컴포넌트를 SO* 편집 대상으로 전환합니다.
        /// 컴포넌트가 없으면 LogWarning 후 이전 대상을 유지합니다.
        /// </summary>
        public GOEditor WithComp<T>() where T : Component
        {
            var comp = _go.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogWarning($"[GOEditor] '{_go.name}': {typeof(T).Name} 컴포넌트를 찾을 수 없습니다.");
                return this;
            }
            SetTarget(comp);
            return this;
        }

        /// <summary>
        /// 타입 전체 이름(FullName)으로 컴포넌트를 추가하거나 재사용합니다.
        /// 어셈블리 경계로 인해 제네릭을 쓸 수 없을 때 사용합니다.
        /// </summary>
        /// <param name="typeFullName">예: "Sindy.Test.ShowcaseRunner"</param>
        public GOEditor AddComp(string typeFullName)
        {
            var type = FindType(typeFullName);
            if (type == null)
            {
                Debug.LogError($"[GOEditor] 타입을 찾을 수 없습니다: {typeFullName}");
                return this;
            }
            var existing = _go.GetComponent(type);
            if (existing != null)
            {
                SetTarget((Component)existing);
                return this;
            }
            // ObjectFactory는 Undo를 자동 등록하므로 권장
            var comp = (Component)ObjectFactory.AddComponent(_go, type);
            Debug.Log($"[GOEditor] 컴포넌트 추가됨: {type.Name} on '{_go.name}'");
            SetTarget(comp);
            return this;
        }

        /// <summary>
        /// 타입 전체 이름(FullName)으로 기존 컴포넌트를 SO* 편집 대상으로 전환합니다.
        /// </summary>
        public GOEditor WithComp(string typeFullName)
        {
            var type = FindType(typeFullName);
            if (type == null)
            {
                Debug.LogError($"[GOEditor] 타입을 찾을 수 없습니다: {typeFullName}");
                return this;
            }
            var comp = _go.GetComponent(type);
            if (comp == null)
            {
                Debug.LogWarning($"[GOEditor] '{_go.name}': {typeFullName} 컴포넌트를 찾을 수 없습니다.");
                return this;
            }
            SetTarget((Component)comp);
            return this;
        }

        // ── 필드 세터 ─────────────────────────────────────────────────────────
        // (Serialized field path) SerializedObjectEditor와 동일한 서명, GOEditor 반환.
        // path는 '.'으로 구분된 SerializedProperty 경로입니다.
        // 예: "cellWidth", "settings.theme.color"
        //
        // ⚠ Unity 빌트인 컴포넌트는 내부 이름이 다릅니다.
        //   TMP text → "m_text" / fontSize → "m_fontSize" / color → "m_fontColor"
        //   Image.color → "m_Color"
        //   확인 방법: Inspector → Debug 모드 or FieldPeeker (Sindy/Tools/Field Peeker)

        public GOEditor SORef(string path, UnityEngine.Object value, bool ignoreNullWarning = false)
        {
            if (value == null && !ignoreNullWarning)
                Debug.LogWarning($"[GOEditor] '{_go.name}':{path} — null 참조가 설정됩니다.");
            return SetProperty(path, p => p.objectReferenceValue = value);
        }

        public GOEditor SOStr(string path, string value)
            => SetProperty(path, p => p.stringValue = value);

        public GOEditor SOBool(string path, bool value)
            => SetProperty(path, p => p.boolValue = value);

        public GOEditor SOInt(string path, int value)
            => SetProperty(path, p => p.intValue = value);

        public GOEditor SOLong(string path, long value)
            => SetProperty(path, p => p.longValue = value);

        public GOEditor SOFloat(string path, float value)
            => SetProperty(path, p => p.floatValue = value);

        public GOEditor SODouble(string path, double value)
            => SetProperty(path, p => p.doubleValue = value);

        public GOEditor SOEnum(string path, int value)
            => SetProperty(path, p => p.enumValueIndex = value);

        public GOEditor SOColor(string path, Color value)
            => SetProperty(path, p => p.colorValue = value);

        public GOEditor SOVector2(string path, Vector2 value)
            => SetProperty(path, p => p.vector2Value = value);

        public GOEditor SOVector3(string path, Vector3 value)
            => SetProperty(path, p => p.vector3Value = value);

        public GOEditor SOVector4(string path, Vector4 value)
            => SetProperty(path, p => p.vector4Value = value);

        public GOEditor SOQuaternion(string path, Quaternion value)
            => SetProperty(path, p => p.quaternionValue = value);

        // ── 커밋 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// ApplyModifiedProperties() + SetDirty()를 호출합니다.
        /// 체인의 마지막에 반드시 호출하세요.
        /// </summary>
        public void Apply()
        {
            if (_so == null) return;
            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_so.targetObject);
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// Apply() 없이 Dispose되면 미저장 변경사항에 대해 LogWarning을 출력합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_so != null && _so.hasModifiedProperties)
            {
                Debug.LogWarning(
                    $"[GOEditor] Apply() 호출 없이 GOEditor가 Dispose됨. " +
                    $"변경사항이 저장되지 않았습니다. " +
                    $"대상: '{_go?.name ?? "null"}' / 컴포넌트: {_so.targetObject?.GetType().Name ?? "null"}");
            }
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        private void SetTarget(Component comp)
        {
            _so = new SerializedObject(comp);
            _so.Update();
        }

        private void EnsureTarget()
        {
            if (_so == null)
                throw new InvalidOperationException(
                    $"[GOEditor] '{_go.name}': SO* 메서드 호출 전에 " +
                    $"AddComp<T>() 또는 WithComp<T>()를 먼저 호출하세요.");
        }

        private GOEditor SetProperty(string path, Action<SerializedProperty> setter)
        {
            EnsureTarget();
            try
            {
                var prop = SOPropertyHelper.GetProperty(_so, path, _go.name);
                setter(prop);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GOEditor] '{_go.name}':{path} 설정 실패: {e.Message}");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }

        // ── 계층 탐색 정적 헬퍼 ──────────────────────────────────────────────

        private static string[] SplitPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("[GOEditor] 경로가 비어있습니다.", nameof(path));
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new ArgumentException("[GOEditor] 유효한 경로 세그먼트가 없습니다.", nameof(path));
            return parts;
        }

        private static GameObject FindOrCreateRootGO(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            SceneManager.MoveGameObjectToScene(go, scene);
            Debug.Log($"[GOEditor] 루트 GO 생성됨: '{name}'");
            return go;
        }

        private static GameObject FindRootGO(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static GameObject FindOrCreateChildGO(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            Debug.Log($"[GOEditor] 자식 GO 생성됨: '{name}' (부모: '{parent.name}')");
            return go;
        }

        // ── 타입 탐색 ─────────────────────────────────────────────────────────

        private static Type FindType(string typeFullName)
        {
            var type = Type.GetType(typeFullName);
            if (type != null) return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeFullName);
                if (type != null) return type;
            }
            return null;
        }
    }

    // ── 내부 공유 유틸: GOEditor / SOEditor 모두 사용 ──────────────────────────

    /// <summary>SerializedProperty 경로 탐색 공유 유틸</summary>
    internal static class SOPropertyHelper
    {
        /// <summary>
        /// '.'으로 구분된 경로를 따라 SerializedProperty를 탐색합니다.
        /// 경로를 찾지 못하면 예외를 던집니다.
        /// </summary>
        internal static SerializedProperty GetProperty(
            SerializedObject so, string path, string targetName)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                throw new Exception($"[SOPropertyHelper] '{targetName}': 경로가 비어있습니다.");

            var prop = so.FindProperty(parts[0]);
            for (int i = 1; i < parts.Length; i++)
            {
                if (prop == null) break;
                prop = prop.FindPropertyRelative(parts[i]);
            }

            if (prop == null)
                throw new Exception(
                    $"[SOPropertyHelper] '{targetName}': 경로 '{path}'의 SerializedProperty를 찾을 수 없습니다.\n" +
                    $"힌트: Inspector를 Debug 모드로 전환하거나 Sindy/Tools/Field Peeker 를 사용하세요.");

            return prop;
        }
    }
}
#endif
