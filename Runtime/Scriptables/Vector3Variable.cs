using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Vector3")]
    public class Vector3Variable : ScriptableObjectVariable<Vector3> { }
    [Serializable]
    public class Vector3Reference : ScriptableObjectReference<Vector3, Vector3Variable>
    {
        public Vector3Reference() : base() { }
        public Vector3Reference(Vector3 value) : base(value) { }
        public Vector3Reference(Vector3Variable variable) : base(variable) { }
        public static implicit operator Vector3(Vector3Reference reference) => reference.Value;
    }
}
