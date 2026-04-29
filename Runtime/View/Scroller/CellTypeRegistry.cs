using System;
using System.Collections.Generic;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// FR-CELL-01 ~ FR-CELL-05.
    /// VM 타입과 ViewComponent prefab 매핑을 보관한다.
    /// 전역 / 인스턴스 / 섹션의 세 스코프 중 인스턴스가 자기 자신을 들고,
    /// 전역은 정적 컨테이너, 섹션은 SectionOption.ContentPrefab 등으로 표현된다.
    /// </summary>
    public sealed class CellTypeRegistry
    {
        private static readonly Dictionary<Type, SindyComponent> globalMap = new();

        private readonly Dictionary<Type, SindyComponent> instanceMap = new();

        public static void RegisterGlobal(Type vmType, SindyComponent prefab)
        {
            if (vmType == null) throw new ArgumentNullException(nameof(vmType));
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            globalMap[vmType] = prefab;
        }

        public static bool TryGetGlobal(Type vmType, out SindyComponent prefab)
            => globalMap.TryGetValue(vmType, out prefab);

        public static void UnregisterGlobal(Type vmType)
            => globalMap.Remove(vmType);

        public static void ClearGlobal() => globalMap.Clear();

        public void Register(Type vmType, SindyComponent prefab)
        {
            if (vmType == null) throw new ArgumentNullException(nameof(vmType));
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            instanceMap[vmType] = prefab;
        }

        public bool TryGetInstance(Type vmType, out SindyComponent prefab)
            => instanceMap.TryGetValue(vmType, out prefab);

        /// <summary>
        /// FR-CELL-03. Prefab 해상도 우선순위:
        /// 1) 섹션에 명시된 prefab (sectionOverride)
        /// 2) 인스턴스에 등록된 prefab
        /// 3) 전역에 등록된 prefab
        /// FR-CELL-04. 어느 스코프에도 등록되지 않으면 예외.
        /// </summary>
        public SindyComponent Resolve(Type vmType, SindyComponent sectionOverride)
        {
            if (vmType == null) throw new ArgumentNullException(nameof(vmType));

            if (sectionOverride != null) return sectionOverride;
            if (instanceMap.TryGetValue(vmType, out var instancePrefab)) return instancePrefab;
            if (globalMap.TryGetValue(vmType, out var globalPrefab)) return globalPrefab;

            throw new InvalidOperationException(
                $"No prefab registered for VM type '{vmType.FullName}'. " +
                $"Register via Scroller.RegisterGlobalCellType<{vmType.Name}>() or scroller.RegisterCellType<{vmType.Name}>(), " +
                $"or set the prefab on the SectionOption.");
        }
    }
}
