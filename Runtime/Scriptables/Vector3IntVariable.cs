using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Vector3Int")]
    public class Vector3IntVariable : ScriptableObjectVariable<Vector3Int> { }
    [Serializable]
    public class Vector3IntReference : ScriptableObjectReference<Vector3Int, Vector3IntVariable>
    {
        public Vector3IntReference() : base() { }
        public Vector3IntReference(Vector3Int value) : base(value) { }
        public Vector3IntReference(Vector3IntVariable variable) : base(variable) { }
        public static implicit operator Vector3Int(Vector3IntReference reference) => reference.Value;
    }
}
