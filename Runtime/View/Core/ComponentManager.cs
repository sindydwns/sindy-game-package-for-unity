using UnityEngine;
using System.Collections.Generic;
using System;
using Sindy.Common;

namespace Sindy.View
{
    /// <summary>
    /// 레이어별 부모 RectTransform와 프리팹 카탈로그를 보유한 싱글톤.
    /// 프리셋 또는 프리팹 이름으로 <see cref="SindyComponent"/>를 생성(Open)하는 진입점이다.
    /// </summary>
    public class ComponentManager : SingletonBehaviour<ComponentManager>
    {
        [SerializeField] private List<RectTransform> parentRects;
        [SerializeField] private GameObjectCollection prefabs;

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

        /// <summary>해당 레이어에 현재 열려 있는 컴포넌트 수. 범위 밖 레이어는 0.</summary>
        public int GetComponentCount(int layer)
        {
            if (layer < 0 || layer >= parentRects.Count) return 0;
            return parentRects[layer].childCount;
        }

        /// <summary>카탈로그에서 이름으로 프리팹을 조회한다.</summary>
        public static T GetPrefab<T>(string name) where T : UnityEngine.Object => Instance.prefabs.GetGameObject<T>(name);

        private SindyComponent BuildComponent(ComponentPreset model)
        {
            var layer = parentRects[Mathf.Clamp(model.Layer, 0, parentRects.Count - 1)];
            return model.Build(layer);
        }
    }
}
