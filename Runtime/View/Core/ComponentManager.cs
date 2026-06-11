using UnityEngine;
using System.Collections.Generic;
using System;
using Sindy.Common;

namespace Sindy.View
{
    public class ComponentManager : SingletonBehaviour<ComponentManager>
    {
        [SerializeField] private List<RectTransform> parentRects;
        [SerializeField] private GameObjectCollection prefabs;

        private SindyComponent BuildComponent(ComponentPreset model)
        {
            var layer = parentRects[Mathf.Clamp(model.Layer, 0, parentRects.Count - 1)];
            return model.Build(layer);
        }

        /// <summary>프리셋을 빌드하고 생성된 인스턴스를 반환한다.</summary>
        public static SindyComponent Open(ComponentPreset preset)
        {
            return Instance.BuildComponent(preset);
        }

        /// <summary>등록된 프리팹을 빌드하고 생성된 인스턴스를 반환한다.</summary>
        public static SindyComponent Open(string panelName, IViewModel data = null, int layer = 0)
        {
            var prefab = Instance.prefabs.GetGameObject<SindyComponent>(panelName);
            if (prefab == null)
            {
                throw new Exception($"Component '{panelName}' not found in ComponentManager prefabs.");
            }
            var preset = new ComponentPreset(prefab, data, layer);
            return Instance.BuildComponent(preset);
        }

        public int GetComponentCount(int layer)
        {
            if (layer < 0 || layer >= parentRects.Count) return 0;
            return parentRects[layer].childCount;
        }

        public static T GetPrefab<T>(string name) where T : UnityEngine.Object => Instance.prefabs.GetGameObject<T>(name);
    }
}
