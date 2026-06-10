using System;
using System.Collections.Generic;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// 셀 키(문자열) → prefab 매핑 보관소.
    /// VM 타입 키 대신 명시적 셀 키를 사용한다 — 셀 모델이 전부 "ViewModel + Feature"가 되면
    /// 타입 키가 붕괴하기 때문이다 (FEATURE_VIEW_SCENARIO.md Step 8).
    ///
    /// 해상 우선순위 (Resolve):
    ///   1) 인스턴스 등록 (scroller.RegisterCell)
    ///   2) CellCatalog 에셋 (ScrollerFeatureView Inspector)
    ///   3) 전역 등록 (RegisterGlobal)
    ///   4) 미등록 → throw (누락 키를 메시지에 명시)
    ///
    /// 키는 오타 방지를 위해 const 모음 클래스로 선언하는 것을 권장한다:
    /// <code>
    /// public static class CellKeys
    /// {
    ///     public const string Title = "title";
    ///     public const string Item  = "shop.item";
    /// }
    /// </code>
    /// </summary>
    public sealed class CellRegistry
    {
        private static readonly Dictionary<string, SindyComponent> globalMap = new();
        private readonly Dictionary<string, SindyComponent> instanceMap = new();

        public static void RegisterGlobal(string key, SindyComponent prefab)
        {
            ValidateArgs(key, prefab);
            globalMap[key] = prefab;
        }

        public static bool TryGetGlobal(string key, out SindyComponent prefab)
            => globalMap.TryGetValue(key, out prefab);

        public static void UnregisterGlobal(string key) => globalMap.Remove(key);

        public static void ClearGlobal() => globalMap.Clear();

        public void Register(string key, SindyComponent prefab)
        {
            ValidateArgs(key, prefab);
            instanceMap[key] = prefab;
        }

        public bool TryGetInstance(string key, out SindyComponent prefab)
            => instanceMap.TryGetValue(key, out prefab);

        public SindyComponent Resolve(string key, CellCatalog catalog)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (instanceMap.TryGetValue(key, out var instancePrefab)) return instancePrefab;
            if (catalog != null && catalog.TryGet(key, out var catalogPrefab)) return catalogPrefab;
            if (globalMap.TryGetValue(key, out var globalPrefab)) return globalPrefab;

            throw new InvalidOperationException(
                $"No prefab registered for cell key '{key}'. " +
                $"Register via scroller.RegisterCell(\"{key}\", prefab), a CellCatalog asset, " +
                $"or ScrollerFeatureView.RegisterGlobalCell(\"{key}\", prefab). " +
                $"Alternatively set the prefab directly on the Section.");
        }

        private static void ValidateArgs(string key, SindyComponent prefab)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
        }
    }
}
