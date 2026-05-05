#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Sindy.View;
using UnityEditor;

namespace Sindy.Editor.View
{
    [CustomEditor(typeof(SindyComponent), editorForChildClasses: true)]
    public class SindyComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (target == null) return;

            var attrs = target.GetType().GetCustomAttributes(typeof(SupportedFeatureAttribute), inherit: true);
            if (attrs == null || attrs.Length == 0) return;

            var seen = new HashSet<Type>();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("지원 Feature", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            foreach (var raw in attrs)
            {
                if (raw is not SupportedFeatureAttribute attr) continue;
                if (attr.FeatureType == null) continue;
                if (!seen.Add(attr.FeatureType)) continue;
                EditorGUILayout.LabelField("• " + attr.FeatureType.Name);
            }
            EditorGUI.indentLevel--;
        }
    }
}
#endif
