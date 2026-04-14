#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.SceneTools
{
    /// <summary>
    /// 컴포넌트의 SerializedProperty 경로 목록을 출력하는 진단 유틸리티.
    /// <para>
    /// SO* 메서드에 전달할 정확한 경로명을 모를 때 사용합니다.
    /// 특히 Unity 빌트인 컴포넌트(TextMeshProUGUI, Image 등)의 내부 필드명 확인에 유용합니다.
    /// </para>
    /// <para>
    /// 사용법:
    ///   1. 코드: <c>FieldPeeker.Print&lt;TextMeshProUGUI&gt;(gameObject);</c>
    ///   2. 에디터 윈도우: Sindy/Tools/Field Peeker Window
    ///   3. 선택 오브젝트: Sindy/Tools/Print Field Names (Selected)
    /// </para>
    /// </summary>
    public static class FieldPeeker
    {
        // ── MenuItem 진입점 ───────────────────────────────────────────────────

        /// <summary>
        /// 현재 선택된 GameObject의 모든 컴포넌트 SerializedProperty 목록을 Console에 출력합니다.
        /// </summary>
        [MenuItem("Sindy/Tools/Print Field Names (Selected)")]
        public static void PrintSelectedComponents()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[FieldPeeker] 선택된 GameObject가 없습니다. Hierarchy에서 오브젝트를 선택하세요.");
                return;
            }

            foreach (var comp in go.GetComponents<Component>())
                PrintComponent(comp);
        }

        // ── 코드에서 직접 호출하는 API ────────────────────────────────────────

        /// <summary>
        /// T 컴포넌트의 SerializedProperty 목록을 Console에 출력합니다.
        /// </summary>
        public static void Print<T>(GameObject go) where T : Component
        {
            var comp = go.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogWarning($"[FieldPeeker] '{go.name}'에 {typeof(T).Name} 컴포넌트가 없습니다.");
                return;
            }
            PrintComponent(comp);
        }

        /// <summary>
        /// Component의 SerializedProperty 목록을 Console에 출력합니다.
        /// </summary>
        public static void Print(Component comp)
            => PrintComponent(comp);

        // ── 내부 구현 ─────────────────────────────────────────────────────────

        internal static List<(string path, string type)> GetFields(Component comp)
        {
            var fields  = new List<(string, string)>();
            var so      = new SerializedObject(comp);
            var iter    = so.GetIterator();

            if (iter.Next(true))
            {
                do
                {
                    fields.Add((iter.propertyPath, iter.propertyType.ToString()));
                }
                while (iter.Next(false));
            }

            return fields;
        }

        private static void PrintComponent(Component comp)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"── {comp.GetType().FullName} ──  (GameObject: '{comp.gameObject.name}')");
            sb.AppendLine("   SO* 경로                                         | 타입");
            sb.AppendLine("   ──────────────────────────────────────────────── | ──────────────");

            foreach (var (path, type) in GetFields(comp))
                sb.AppendLine($"   \"{path,-46}\" | {type}");

            Debug.Log(sb.ToString());
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    // EditorWindow: Sindy/Tools/Field Peeker Window
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// FieldPeeker의 GUI 버전.
    /// 컴포넌트를 드래그하거나 GameObject를 선택하면 SO* 경로 목록이 표시됩니다.
    /// 경로 옆 [복사] 버튼으로 클립보드에 복사할 수 있습니다.
    /// </summary>
    public class FieldPeekerWindow : EditorWindow
    {
        [MenuItem("Sindy/Tools/Field Peeker Window")]
        public static void Open()
            => GetWindow<FieldPeekerWindow>("Field Peeker");

        private Component _target;
        private Vector2 _scroll;
        private List<(string path, string type)> _fields = new();
        private string _filter = "";

        private void OnGUI()
        {
            // ── 헤더 ──────────────────────────────────────────────────────
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("SO* 경로 확인 도구", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "컴포넌트를 드래그하거나 선택하면 SerializedProperty 경로 목록이 표시됩니다.\n" +
                "[복사] 버튼으로 경로를 클립보드에 복사할 수 있습니다.",
                MessageType.Info);
            EditorGUILayout.Space(4);

            // ── 컴포넌트 선택 ─────────────────────────────────────────────
            var newTarget = (Component)EditorGUILayout.ObjectField(
                "컴포넌트", _target, typeof(Component), allowSceneObjects: true);

            if (newTarget != _target)
            {
                _target = newTarget;
                RefreshFields();
            }

            // ── 선택된 GO의 컴포넌트 자동 표시 버튼 ─────────────────────
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("선택된 GO의 컴포넌트 목록 가져오기", GUILayout.Height(22)))
                ShowComponentMenu();

            if (_target != null && GUILayout.Button("Console 출력", GUILayout.Width(90), GUILayout.Height(22)))
                FieldPeeker.Print(_target);
            EditorGUILayout.EndHorizontal();

            if (_target == null) return;

            EditorGUILayout.Space(6);

            // ── 필터 ──────────────────────────────────────────────────────
            _filter = EditorGUILayout.TextField("필터", _filter);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                $"{_target.GetType().Name}  —  SerializedProperty 목록 ({_fields.Count}개)",
                EditorStyles.boldLabel);

            // ── 필드 목록 ─────────────────────────────────────────────────
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            bool hasFilter  = !string.IsNullOrEmpty(_filter);
            string lower    = _filter.ToLowerInvariant();

            foreach (var (path, type) in _fields)
            {
                if (hasFilter && !path.ToLowerInvariant().Contains(lower)) continue;

                EditorGUILayout.BeginHorizontal();

                // [복사] 버튼
                if (GUILayout.Button("복사", GUILayout.Width(38), GUILayout.Height(16)))
                {
                    GUIUtility.systemCopyBuffer = path;
                    Debug.Log($"[FieldPeeker] 클립보드에 복사됨: \"{path}\"");
                }

                // 경로 (선택 가능한 라벨)
                EditorGUILayout.SelectableLabel(path, GUILayout.Height(16), GUILayout.ExpandWidth(true));

                // 타입 (고정 너비)
                EditorGUILayout.LabelField(type, EditorStyles.miniLabel, GUILayout.Width(100));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        private void RefreshFields()
        {
            _fields.Clear();
            if (_target == null) return;
            _fields = FieldPeeker.GetFields(_target);
        }

        private void ShowComponentMenu()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("[FieldPeeker] 선택된 GameObject가 없습니다.");
                return;
            }

            var comps = go.GetComponents<Component>();
            if (comps.Length == 0) return;

            var menu = new GenericMenu();
            foreach (var comp in comps)
            {
                var c = comp; // capture
                menu.AddItem(new GUIContent(comp.GetType().Name), false, () =>
                {
                    _target = c;
                    RefreshFields();
                    Repaint();
                });
            }
            menu.ShowAsContext();
        }
    }
}
#endif
