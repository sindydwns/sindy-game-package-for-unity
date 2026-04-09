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

        private void BuildComponent(ComponentPreset model)
        {
            var layer = parentRects[Mathf.Clamp(model.Layer, 0, parentRects.Count - 1)];
            model.Build(layer);
        }

        public static void Open(ComponentPreset preset)
        {
            Instance.BuildComponent(preset);
        }

        public static void Open(string panelName, object data = null, int layer = 0)
        {
            var prefab = Instance.prefabs.GetGameObject<SindyComponent>(panelName);
            if (prefab == null)
            {
                throw new Exception($"Component '{panelName}' not found in ComponentManager prefabs.");
            }
            var preset = new ComponentPreset(prefab, data, layer);
            Instance.BuildComponent(preset);
        }

        public int GetComponentCount(int layer)
        {
            if (layer < 0 || layer >= parentRects.Count) return 0;
            return parentRects[layer].childCount;
        }

        public static T GetPrefab<T>(string name) where T : UnityEngine.Object => Instance.prefabs.GetGameObject<T>(name);
    }
}
