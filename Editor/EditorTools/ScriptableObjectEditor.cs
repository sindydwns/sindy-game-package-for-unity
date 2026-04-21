#if UNITY_EDITOR
using System;
using System.Runtime.ExceptionServices;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// ScriptableObject 에셋을 안전하게 편집하는 컨텍스트 래퍼.
    /// <para>
    /// PrefabEditor / SceneEditor와 동일한 IDisposable + SO* 체이닝 패턴을 사용합니다.
    /// Apply() 없이 Dispose되면 미저장 변경사항에 대해 LogWarning이 출력됩니다.
    /// Dispose 시 AssetDatabase.SaveAssets()가 자동으로 호출됩니다.
    /// </para>
    /// <example>
    /// <code>
    /// // 기존 에셋 열기
    /// using (var so = SOEditor<IntVariable>.Open("Assets/.../MyInt.asset"))
    /// {
    ///     if (so == null) return;
    ///     so.SOInt("Value", 42)
    ///       .SOStr("description", "테스트 값")
    ///       .Apply();
    /// }
    /// // Dispose → AssetDatabase.SaveAssets() 자동 호출
    ///
    /// // 새 에셋 생성
    /// using (var so = SOEditor<FloatVariable>.Create("Assets/.../NewFloat.asset"))
    /// {
    ///     so.SOFloat("Value", 0.5f).Apply();
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public sealed class ScriptableObjectEditor<T> : IDisposable where T : ScriptableObject
    {
        private readonly T _asset;
        private readonly SerializedObject _so;
        private bool _wasApplied;
        private bool _disposed;

        // ── 프로퍼티 ──────────────────────────────────────────────────────────

        /// <summary>편집 중인 ScriptableObject 에셋</summary>
        public T Asset => _asset;

        // ── 생성자 ────────────────────────────────────────────────────────────

        private ScriptableObjectEditor(T asset)
        {
            _asset = asset;
            _so = new SerializedObject(asset);
            _so.Update();
        }

        // ── 팩토리 ────────────────────────────────────────────────────────────

        /// <summary>
        /// 기존 ScriptableObject 에셋을 로드합니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .asset 파일 경로</param>
        /// <returns>편집 컨텍스트. 로드 실패 시 null.</returns>
        public static ScriptableObjectEditor<T> Open(string assetPath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[SOEditor] 에셋을 로드할 수 없습니다: {assetPath}\n" +
                               $"경로를 확인하거나 AssetFinder.Asset<{typeof(T).Name}>()을 사용하세요.");
                return null;
            }
            Debug.Log($"[SOEditor] 에셋 로드됨: {assetPath}");
            return new ScriptableObjectEditor<T>(asset);
        }

        /// <summary>
        /// 새 ScriptableObject 에셋을 생성합니다.
        /// 지정 경로에 파일이 이미 있으면 덮어씁니다.
        /// </summary>
        /// <param name="assetPath">Assets/ 로 시작하는 .asset 파일 경로</param>
        /// <returns>편집 컨텍스트.</returns>
        public static ScriptableObjectEditor<T> Create(string assetPath)
        {
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            Debug.Log($"[SOEditor] 에셋 생성됨: {assetPath}");
            return new ScriptableObjectEditor<T>(asset);
        }

        // ── 필드 세터 ─────────────────────────────────────────────────────────
        // (Serialized field path) '.'으로 구분된 SerializedProperty 경로.
        // public 필드는 선언명 그대로 사용 (예: "Value", "description").
        // [SerializeField] private 필드도 선언명 그대로 사용.

        public ScriptableObjectEditor<T> SetRef(string path, UnityEngine.Object value, bool ignoreNullWarning = false)
        {
            if (value == null && !ignoreNullWarning)
                Debug.LogWarning($"[SOEditor] '{_asset.name}':{path} — null 참조가 설정됩니다.");
            return SetProperty(path, p => p.objectReferenceValue = value);
        }

        public ScriptableObjectEditor<T> SetStr(string path, string value)
            => SetProperty(path, p => p.stringValue = value);

        public ScriptableObjectEditor<T> SetBool(string path, bool value)
            => SetProperty(path, p => p.boolValue = value);

        public ScriptableObjectEditor<T> SetInt(string path, int value)
            => SetProperty(path, p => p.intValue = value);

        public ScriptableObjectEditor<T> SetLong(string path, long value)
            => SetProperty(path, p => p.longValue = value);

        public ScriptableObjectEditor<T> SetFloat(string path, float value)
            => SetProperty(path, p => p.floatValue = value);

        public ScriptableObjectEditor<T> SetDouble(string path, double value)
            => SetProperty(path, p => p.doubleValue = value);

        public ScriptableObjectEditor<T> SetEnum(string path, int value)
            => SetProperty(path, p => p.enumValueIndex = value);

        public ScriptableObjectEditor<T> SetColor(string path, Color value)
            => SetProperty(path, p => p.colorValue = value);

        public ScriptableObjectEditor<T> SetVector2(string path, Vector2 value)
            => SetProperty(path, p => p.vector2Value = value);

        public ScriptableObjectEditor<T> SetVector3(string path, Vector3 value)
            => SetProperty(path, p => p.vector3Value = value);

        public ScriptableObjectEditor<T> SetVector4(string path, Vector4 value)
            => SetProperty(path, p => p.vector4Value = value);

        public ScriptableObjectEditor<T> SetQuaternion(string path, Quaternion value)
            => SetProperty(path, p => p.quaternionValue = value);

        // ── 커밋 ──────────────────────────────────────────────────────────────

        /// <summary>
        /// ApplyModifiedProperties() + SetDirty()를 호출합니다.
        /// Dispose 시 AssetDatabase.SaveAssets()로 디스크에 저장됩니다.
        /// 체인의 마지막에 반드시 호출하세요.
        /// </summary>
        public void Apply()
        {
            _wasApplied = true;
            _so.ApplyModifiedProperties();
            EditorUtility.SetDirty(_asset);
        }

        // ── IDisposable ───────────────────────────────────────────────────────

        /// <summary>
        /// using 블록 종료 시 자동으로 호출됩니다.
        /// Apply()가 호출된 경우 AssetDatabase.SaveAssets()로 디스크에 저장합니다.
        /// Apply() 없이 미저장 변경사항이 있으면 LogWarning을 출력합니다.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_so.hasModifiedProperties)
            {
                Debug.LogWarning(
                    $"[SOEditor] Apply() 호출 없이 SOEditor이 Dispose됨. " +
                    $"변경사항이 저장되지 않았습니다. " +
                    $"대상: '{_asset?.name ?? "null"}' ({typeof(T).Name})");
                return;
            }

            if (_wasApplied && _asset != null)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[SOEditor] 에셋 저장 완료: {AssetDatabase.GetAssetPath(_asset)}");
            }
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        private ScriptableObjectEditor<T> SetProperty(string path, Action<SerializedProperty> setter)
        {
            try
            {
                var prop = SOPropertyHelper.GetProperty(_so, path, _asset.name);
                setter(prop);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SOEditor] '{_asset.name}':{path} 설정 실패: {e.Message}");
                ExceptionDispatchInfo.Capture(e).Throw();
            }
            return this;
        }
    }
}
#endif
