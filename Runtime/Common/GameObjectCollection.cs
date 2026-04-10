using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Common
{
    public class GameObjectCollection : MonoBehaviour, IGameObjectCollection
    {
        [SerializeField] private List<GameObject> prefabs = new();
        private Dictionary<string, GameObject> _cache;

        private Dictionary<string, GameObject> Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = new Dictionary<string, GameObject>(prefabs.Count);
                    foreach (var prefab in prefabs)
                    {
                        if (prefab != null)
                            _cache[prefab.name] = prefab;
                    }
                }
                return _cache;
            }
        }

        public T GetGameObject<T>(string name) where T : Object
        {
            if (Cache.TryGetValue(name, out var prefab))
                return prefab.GetComponent<T>();
            return null;
        }
    }
}
