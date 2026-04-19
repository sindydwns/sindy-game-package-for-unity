#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    public static class SindyPreferences
    {
        private const string PortPrefKey = "Sindy.EditorTools.HttpPort";
        private const int DefaultPort = 6060;

        [SettingsProvider]
        public static SettingsProvider CreateProvider() => new SettingsProvider("Preferences/Sindy", SettingsScope.User)
        {
            label = "Sindy",
            guiHandler = _ =>
            {
                EditorGUILayout.LabelField("EditorTools HTTP 서버", EditorStyles.boldLabel);
                EditorGUILayout.Space(4);

                int current = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
                int newPort = EditorGUILayout.IntField("HTTP 포트", current);
                if (newPort != current)
                {
                    if (newPort < 1024 || newPort > 65535)
                        EditorGUILayout.HelpBox("포트는 1024–65535 범위여야 합니다.", MessageType.Warning);
                    else
                    {
                        EditorPrefs.SetInt(PortPrefKey, newPort);
                        EditorGUILayout.HelpBox("변경사항은 Unity를 재시작하거나 스크립트 재컴파일 후 적용됩니다.", MessageType.Info);
                    }
                }

                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("현재 서버 주소", $"http://localhost:{current}/execute");

                if (GUILayout.Button("기본값으로 초기화"))
                    EditorPrefs.SetInt(PortPrefKey, DefaultPort);
            }
        };
    }
}
#endif
