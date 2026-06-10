#if UNITY_EDITOR
using System.Collections.Generic;
using Sindy.View;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.View
{
    /// <summary>
    /// SindyComponent(허브) Inspector.
    /// 부착된 FeatureView 목록을 표시하고, 플레이 중에는 바인딩된 모델의 Feature와의
    /// 매칭 상태(✓/✗)를 함께 보여준다.
    /// </summary>
    [CustomEditor(typeof(SindyComponent), editorForChildClasses: true)]
    public class SindyComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target is not SindyComponent component) return;

            var featureViews = component.GetComponents<IFeatureView>();
            if (featureViews.Length == 0) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("부착된 FeatureView", EditorStyles.boldLabel);

            var model = Application.isPlaying ? component.CurrentModel : null;
            var modelFeatureTypes = model != null
                ? new HashSet<System.Type>(model.GetFeatureTypes())
                : null;

            EditorGUI.indentLevel++;
            foreach (var view in featureViews)
            {
                if (modelFeatureTypes != null)
                {
                    var matched = modelFeatureTypes.Remove(view.FeatureType);
                    EditorGUILayout.LabelField($"{(matched ? "✓" : "✗")} {view.FeatureType.Name}");
                }
                else
                {
                    EditorGUILayout.LabelField("• " + view.FeatureType.Name);
                }
            }

            // 모델에는 있으나 매칭되는 FeatureView가 없는 Feature
            if (modelFeatureTypes != null)
            {
                foreach (var orphan in modelFeatureTypes)
                {
                    EditorGUILayout.LabelField($"✗ {orphan.Name} (View 없음)");
                }
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
