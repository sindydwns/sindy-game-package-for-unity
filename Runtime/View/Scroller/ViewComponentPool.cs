using System.Collections.Generic;
using UnityEngine;

namespace Sindy.View.Scroller
{
    /// <summary>
    /// FR-POOL-03. Prefab 단위로 분리되는 무제한 풀.
    /// 풀의 인스턴스는 Hierarchy 상에서 스크롤러의 하위에 위치하며 (FR-POOL-05),
    /// 스크롤러가 파괴될 때 자연스럽게 함께 파괴된다 (FR-POOL-06).
    /// </summary>
    internal sealed class ViewComponentPool
    {
        private readonly Dictionary<SindyComponent, Stack<SindyComponent>> pools = new();
        private readonly Transform poolRoot;
        private readonly Transform activeRoot;

        public ViewComponentPool(Transform poolRoot, Transform activeRoot)
        {
            this.poolRoot = poolRoot;
            this.activeRoot = activeRoot;
        }

        public SindyComponent Acquire(SindyComponent prefab)
        {
            if (prefab == null) return null;

            if (pools.TryGetValue(prefab, out var stack) && stack.Count > 0)
            {
                var reused = stack.Pop();
                reused.transform.SetParent(activeRoot, false);
                reused.gameObject.SetActive(true);
                return reused;
            }

            var inst = Object.Instantiate(prefab, activeRoot);
            inst.gameObject.SetActive(true);
            return inst;
        }

        public void Release(SindyComponent prefab, SindyComponent instance)
        {
            if (instance == null) return;

            instance.Bind(null);
            instance.gameObject.SetActive(false);
            instance.transform.SetParent(poolRoot, false);

            if (!pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<SindyComponent>();
                pools[prefab] = stack;
            }
            stack.Push(instance);
        }

        /// <summary>FR-POOL-04. 풀 사전 워밍.</summary>
        public void Prewarm(SindyComponent prefab, int count)
        {
            if (prefab == null || count <= 0) return;

            if (!pools.TryGetValue(prefab, out var stack))
            {
                stack = new Stack<SindyComponent>(count);
                pools[prefab] = stack;
            }

            for (var i = 0; i < count; i++)
            {
                var inst = Object.Instantiate(prefab, poolRoot);
                inst.gameObject.SetActive(false);
                stack.Push(inst);
            }
        }
    }
}
