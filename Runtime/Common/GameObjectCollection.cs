using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Common
{
    public class GameObjectCollection : MonoBehaviour, IGameObjectCollection
    {
        [SerializeField] private List<GameObject> prefabs = new();

        public T GetGameObject<T>(string name) where T : Object
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
