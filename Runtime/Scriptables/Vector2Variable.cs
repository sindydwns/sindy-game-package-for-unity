using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Vector2")]
    public class Vector2Variable : ScriptableObjectVariable<Vector2> { }
    [Serializable]
    public class Vector2Reference : ScriptableObjectReference<Vector2, Vector2Variable>
    {
        public Vector2Reference() : base() { }
        public Vector2Reference(Vector2 value) : base(value) { }
        public Vector2Reference(Vector2Variable variable) : base(variable) { }
        public static implicit operator Vector2(Vector2Reference reference) => reference.Value;
    }
}
