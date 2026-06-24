#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sindy.View;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.View
{
    /// <summary>
    /// SindyComponent(허브) Inspector.
    ///
    /// 모델 ↔ 뷰의 키 매칭 상태를 두 개의 표로 보여주는 디버깅 패널을 그린다.
    /// - Feature 매칭: FeatureView ↔ ModelFeature (Type 키)
    /// - 자식 매칭: views 리스트의 문자열 키 ↔ 모델 자식
    ///
    /// 런타임에는 매칭 미스매치를 조용히 통과시키므로(설계 의도), 진단은 이 표로 한다.
    /// 플레이 중에는 바인딩된 실제 모델과 대조해 매칭(✓)/미스매치(⚠)를 표시하고,
    /// 에디트 모드에서는 대조 상대가 없으므로 뷰 측 정적 목록(•)만 표시한다.
    /// </summary>
    [CustomEditor(typeof(SindyComponent), editorForChildClasses: true)]
    public class SindyComponentEditor : UnityEditor.Editor
    {
        private static readonly Color WarnColor = new(1f, 0.6f, 0.1f);
        private static GUIStyle warnStyle;

        private static GUIStyle WarnStyle => warnStyle ??=
            new GUIStyle(EditorStyles.label) { normal = { textColor = WarnColor } };

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not SindyComponent component) return;

            var featureViews = component.GetComponents<IFeatureView>();
            var views = component.ViewsForEditor;

            bool hasFeatureViews = featureViews.Length > 0;
            bool hasViews = views != null && views.Count > 0;
            if (!hasFeatureViews && !hasViews) return;

            var model = Application.isPlaying ? component.CurrentModel : null;

            EditorGUILayout.Space();
            if (model == null)
            {
                EditorGUILayout.HelpBox(
                    "플레이 중에 바인딩된 모델과의 키 매칭 상태(✓/⚠)가 표시됩니다.",
                    MessageType.None);
            }

            if (hasFeatureViews) DrawFeatureAxis(featureViews, model);
            if (hasViews) DrawStructureAxis(views, model);
        }

        /// <summary>Feature 매칭: FeatureView ↔ ModelFeature 매칭 표.</summary>
        private static void DrawFeatureAxis(IFeatureView[] featureViews, IViewModel model)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Feature 매칭 (Feature ↔ View)", EditorStyles.boldLabel);
            DrawHeader("", "FeatureView", "ModelFeature");

            var modelFeatures = model != null ? new HashSet<Type>(model.GetFeatureTypes()) : null;

            foreach (var view in featureViews)
            {
                var viewName = view.GetType().Name;
                var featureName = view.FeatureType.Name;

                if (modelFeatures == null)
                {
                    DrawRow("•", true, viewName, featureName);
                }
                else
                {
                    bool matched = modelFeatures.Remove(view.FeatureType);
                    DrawRow(matched ? "✓" : "⚠", matched,
                        viewName, matched ? featureName : "(모델에 없음)");
                }
            }

            // 모델에는 있으나 매칭되는 FeatureView가 없는 Feature
            if (modelFeatures != null)
            {
                foreach (var orphan in modelFeatures)
                    DrawRow("⚠", false, "(View 없음)", orphan.Name);
            }
        }

        /// <summary>자식 매칭: views 리스트 문자열 키 ↔ 모델 자식 매칭 표.</summary>
        private static void DrawStructureAxis(IReadOnlyList<SindyComponent.ViewBehaviour> views, IViewModel model)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자식(Child) 매칭 (Key ↔ Child Hub)", EditorStyles.boldLabel);
            DrawHeader("", "키", "연결 허브", "모델 자식");

            // GetChildNames()는 최상위 레벨 키만 반환한다. 점 표기 하위 키 비교는 최상위 기준.
            var modelChildren = model != null ? new HashSet<string>(model.GetChildNames()) : null;

            foreach (var vb in views)
            {
                var key = string.IsNullOrEmpty(vb.name) ? "(키 없음)" : vb.name;
                var hub = vb.component != null ? vb.component.name : "(허브 없음)";

                if (modelChildren == null)
                {
                    DrawRow("•", true, key, hub, "");
                }
                else
                {
                    bool matched = !string.IsNullOrEmpty(vb.name) && model[vb.name] != null;
                    if (!string.IsNullOrEmpty(vb.name))
                        modelChildren.Remove(vb.name.Split('.')[0]);
                    DrawRow(matched ? "✓" : "⚠", matched,
                        key, hub, matched ? "있음" : "(모델에 없음)");
                }
            }

            // 모델에는 있으나 키로 등록된 뷰가 없는 자식
            if (modelChildren != null)
            {
                foreach (var orphan in modelChildren)
                    DrawRow("⚠", false, orphan, "(뷰 없음)", "있음");
            }
        }

        private static void DrawHeader(params string[] cols)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(cols[0], EditorStyles.miniBoldLabel, GUILayout.Width(22));
            for (int i = 1; i < cols.Length; i++)
                EditorGUILayout.LabelField(cols[i], EditorStyles.miniBoldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawRow(string status, bool ok, params string[] cols)
        {
            var style = ok ? EditorStyles.label : WarnStyle;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(status, style, GUILayout.Width(22));
            foreach (var c in cols)
                EditorGUILayout.LabelField(c, style);
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
