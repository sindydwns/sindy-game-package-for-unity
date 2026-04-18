// ────────────────────────────────────────────────────────────────────────────
// PackagePathHelper — UPM 설치 방식에 무관한 패키지 내부 경로 해결 유틸리티
//
// Embedded   : Assets/sindy-game-package-for-unity/
// Local ref  : Packages/com.sindy/
// Git URL    : Packages/com.sindy/  (Library/PackageCache 가상 경로)
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.EditorTools
{
    /// <summary>
    /// UPM 설치 방식(Embedded/Local/Git)에 무관하게 패키지 내부 경로를 해결합니다.
    /// </summary>
    public static class PackagePathHelper
    {
        // package.json 의 "name" 필드와 일치해야 합니다.
        private const string PackageName = "com.sindy";

        // Embedded 설치 시 Assets/ 하위 폴더명
        private const string EmbeddedFolderName = "sindy-game-package-for-unity";

        /// <summary>
        /// 패키지 내부 상대경로를 Unity 에디터에서 사용 가능한 절대 경로로 변환합니다.
        ///
        /// 예: "Tests/Runtime/Scenes/TestScene.unity"
        ///   → "Packages/com.sindy/Tests/Runtime/Scenes/TestScene.unity"  (UPM 설치 시)
        ///   → "Assets/sindy-game-package-for-unity/Tests/Runtime/Scenes/TestScene.unity"  (Embedded 시)
        /// </summary>
        public static string Resolve(string relativePath)
        {
            // 1. UPM(Packages/) 경로로 접근 가능한지 먼저 시도
            var packagePath = $"Packages/{PackageName}/{relativePath}";
            if (AssetDatabase.IsValidFolder(packagePath) ||
                AssetDatabase.LoadAssetAtPath<Object>(packagePath) != null)
                return packagePath;

            // 2. Assets/ 하위 Embedded 방식으로 폴백
            var embeddedRoot = FindEmbeddedRoot();
            if (embeddedRoot != null)
            {
                var candidate = $"{embeddedRoot}/{relativePath}";
                if (AssetDatabase.IsValidFolder(candidate) ||
                    AssetDatabase.LoadAssetAtPath<Object>(candidate) != null)
                    return candidate;
            }

            // 3. 경로가 존재하지 않더라도 UPM 기본 경로 반환 (호출부에서 존재 여부 판단)
            return packagePath;
        }

        /// <summary>
        /// 패키지 루트 경로를 반환합니다.
        ///   UPM: "Packages/com.sindy"
        ///   Embedded: "Assets/sindy-game-package-for-unity"
        /// </summary>
        public static string Root()
        {
            var packagePath = $"Packages/{PackageName}";
            if (AssetDatabase.IsValidFolder(packagePath))
                return packagePath;

            var embeddedRoot = FindEmbeddedRoot();
            if (embeddedRoot != null)
                return embeddedRoot;

            return packagePath;
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────────────

        private static string _cachedEmbeddedRoot;

        private static string FindEmbeddedRoot()
        {
            if (_cachedEmbeddedRoot != null)
                return _cachedEmbeddedRoot;

            // Assets/ 바로 아래의 폴더명으로 검색
            var guids = AssetDatabase.FindAssets(EmbeddedFolderName, new[] { "Assets" });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.IsValidFolder(path) && path.EndsWith(EmbeddedFolderName))
                {
                    _cachedEmbeddedRoot = path;
                    return path;
                }
            }

            // asmdef 경로로 재시도 (폴더명이 달라도 대응)
            var asmdefGuids = AssetDatabase.FindAssets("t:asmdef", new[] { "Assets" });
            foreach (var guid in asmdefGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains(EmbeddedFolderName)) continue;

                // "Assets/sindy-game-package-for-unity/..." → "Assets/sindy-game-package-for-unity"
                var idx = path.IndexOf(EmbeddedFolderName);
                if (idx < 0) continue;
                var root = path.Substring(0, idx + EmbeddedFolderName.Length);
                if (AssetDatabase.IsValidFolder(root))
                {
                    _cachedEmbeddedRoot = root;
                    return root;
                }
            }

            return null;
        }
    }
}
#endif
