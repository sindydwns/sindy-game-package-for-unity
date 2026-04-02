using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Common
{
    public class GameObjectCollection : MonoBehaviour
    {
        [SerializeField] private List<GameObject> prefabs = new();

        public T GetPrefab<T>(string name) where T : Component
        {
            foreach (var prefab in prefabs)
            {
                if (prefab != null && prefab.name.Equals(name))
                {
                    return prefab.GetComponent<T>();
                }
            }
            return null;
        }
    }
}
