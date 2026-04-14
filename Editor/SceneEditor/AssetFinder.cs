#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sindy.Editor.SceneTools
{
    /// <summary>
    /// AssetDatabase에서 프리팹과 ScriptableObject 에셋을 탐색하는 유틸리티.
    /// 에디터 세션 내 결과를 캐싱하여 반복 탐색 비용을 줄입니다.
    /// <para>
    /// 프리팹을 새로 생성하거나 이동한 후 <see cref="ClearCache"/>를 호출하세요.
    /// </para>
    /// </summary>
    public static class AssetFinder
    {
        // 캐시: 키 → 오브젝트 목록
        private static readonly Dictionary<string, List<GameObject>>         _prefabCache = new();
        private static readonly Dictionary<string, List<ScriptableObject>>   _soCache     = new();

        /// <summary>에디터 세션 캐시를 전부 비웁니다.</summary>
        public static void ClearCache()
        {
            _prefabCache.Clear();
            _soCache.Clear();
        }

        // ════════════════════════════════════════════════════════════════════
        // 프리팹 탐색
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T 컴포넌트를 가진 프리팹 중 첫 번째 컴포넌트를 반환합니다.
        /// </summary>
        /// <typeparam name="T">탐색할 컴포넌트 타입</typeparam>
        /// <param name="inFolder">탐색 폴더 (null이면 Assets 전체)</param>
        public static T Prefab<T>(string inFolder = null) where T : Component
        {
            var prefabs = AllPrefabs<T>(inFolder);
            return prefabs.Count > 0 ? prefabs[0].GetComponentInChildren<T>(true) : null;
        }

        /// <summary>
        /// T 컴포넌트를 가진 프리팹 GameObject 전체를 반환합니다.
        /// </summary>
        public static List<GameObject> AllPrefabs<T>(string inFolder = null) where T : Component
        {
            string key = $"comp:{typeof(T).FullName}:{inFolder ?? "Assets"}";
            if (_prefabCache.TryGetValue(key, out var cached)) return cached;

            var results = SearchPrefabs(inFolder, go => go.GetComponentInChildren<T>(true) != null);
            _prefabCache[key] = results;
            return results;
        }

        /// <summary>
        /// 컴포넌트 전체 타입 이름(FullName)으로 프리팹을 탐색합니다.
        /// hint 문자열이 프리팹 이름에 포함된 것을 우선 반환합니다.
        /// <para>어셈블리 경계로 인해 제네릭을 쓸 수 없을 때 사용합니다.</para>
        /// </summary>
        /// <param name="componentTypeFullName">예: "Sindy.View.Components.LabelComponent"</param>
        /// <param name="hint">이름 우선순위 힌트 (예: "label"). null이면 무시.</param>
        /// <param name="inFolder">탐색 폴더 (null이면 Assets 전체)</param>
        public static Component Prefab(
            string componentTypeFullName,
            string hint = null,
            string inFolder = null)
        {
            string key = $"typename:{componentTypeFullName}:{hint}:{inFolder ?? "Assets"}";
            if (_prefabCache.TryGetValue(key, out var cached))
                return cached.Count > 0 ? GetComponentByTypeName(cached[0], componentTypeFullName) : null;

            var candidates = SearchPrefabs(inFolder, go =>
                go.GetComponentsInChildren<Component>(true)
                  .Any(c => c.GetType().FullName == componentTypeFullName));

            if (candidates.Count == 0)
            {
                _prefabCache[key] = new List<GameObject>();
                return null;
            }

            GameObject best;
            if (!string.IsNullOrEmpty(hint))
            {
                string lowerHint = hint.ToLowerInvariant();
                best = candidates.FirstOrDefault(p => p.name.ToLowerInvariant().Contains(lowerHint))
                       ?? candidates[0];
            }
            else
            {
                best = candidates[0];
            }

            _prefabCache[key] = new List<GameObject> { best };
            return GetComponentByTypeName(best, componentTypeFullName);
        }

        /// <summary>
        /// 이름에 patterns를 포함하는 프리팹 중 매칭 점수가 높은 것을 반환합니다.
        /// 대소문자를 구분하지 않습니다.
        /// </summary>
        public static GameObject PrefabByName(params string[] patterns)
            => PrefabByName(null, patterns);

        /// <summary>
        /// 지정 폴더 내에서 이름 패턴으로 프리팹을 탐색합니다.
        /// </summary>
        public static GameObject PrefabByName(string inFolder, params string[] patterns)
        {
            string key = $"name:{string.Join(",", patterns)}:{inFolder ?? "Assets"}";
            if (_prefabCache.TryGetValue(key, out var cached))
                return cached.Count > 0 ? cached[0] : null;

            string[] guids   = AssetDatabase.FindAssets("t:Prefab", new[] { inFolder ?? "Assets" });
            var scored = new List<(int score, GameObject prefab)>();

            foreach (string guid in guids)
            {
                string path   = AssetDatabase.GUIDToAssetPath(guid);
                var prefab    = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                string lower  = prefab.name.ToLowerInvariant();
                int score     = patterns.Count(p => lower.Contains(p.ToLowerInvariant()));
                if (score > 0) scored.Add((score, prefab));
            }

            var results = scored.OrderByDescending(x => x.score).Select(x => x.prefab).ToList();
            _prefabCache[key] = results;
            return results.Count > 0 ? results[0] : null;
        }

        // ════════════════════════════════════════════════════════════════════
        // ScriptableObject 에셋 탐색
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// T 타입의 ScriptableObject 에셋 중 첫 번째를 반환합니다.
        /// </summary>
        /// <typeparam name="T">탐색할 ScriptableObject 타입</typeparam>
        /// <param name="inFolder">탐색 폴더 (null이면 Assets 전체)</param>
        public static T Asset<T>(string inFolder = null) where T : ScriptableObject
        {
            var all = AllAssets<T>(inFolder);
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>
        /// T 타입의 ScriptableObject 에셋 전체를 반환합니다.
        /// </summary>
        public static List<T> AllAssets<T>(string inFolder = null) where T : ScriptableObject
        {
            string key = $"so:{typeof(T).FullName}:{inFolder ?? "Assets"}";
            if (_soCache.TryGetValue(key, out var cached))
                return cached.Cast<T>().ToList();

            string[] guids  = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { inFolder ?? "Assets" });
            var results     = new List<T>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset   = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) results.Add(asset);
            }

            // 타입 불일치 방지를 위해 ScriptableObject 캐시에 저장
            _soCache[key] = results.Cast<ScriptableObject>().ToList();
            return results;
        }

        // ════════════════════════════════════════════════════════════════════
        // 내부 헬퍼
        // ════════════════════════════════════════════════════════════════════

        private static List<GameObject> SearchPrefabs(
            string inFolder, System.Func<GameObject, bool> predicate)
        {
            string[] guids  = AssetDatabase.FindAssets("t:Prefab", new[] { inFolder ?? "Assets" });
            var results     = new List<GameObject>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab  = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && predicate(prefab))
                    results.Add(prefab);
            }
            return results;
        }

        private static Component GetComponentByTypeName(GameObject go, string typeFullName)
            => go.GetComponentsInChildren<Component>(true)
                 .FirstOrDefault(c => c.GetType().FullName == typeFullName);
    }
}
#endif
