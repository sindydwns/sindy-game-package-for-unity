using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Vector2Int")]
    public class Vector2IntVariable : ScriptableObjectVariable<Vector2Int> { }
    [Serializable]
    public class Vector2IntReference : ScriptableObjectReference<Vector2Int, Vector2IntVariable>
    {
        public Vector2IntReference() : base() { }
        public Vector2IntReference(Vector2Int value) : base(value) { }
        public Vector2IntReference(Vector2IntVariable variable) : base(variable) { }
        public static implicit operator Vector2Int(Vector2IntReference reference) => reference.Value;
    }
}
