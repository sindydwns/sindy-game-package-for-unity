using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Int")]
    public class IntVariable : ScriptableObjectVariable<int> { }
    [Serializable]
    public class IntReference : ScriptableObjectReference<int, IntVariable>
    {
        public IntReference() : base() { }
        public IntReference(int value) : base(value) { }
        public IntReference(IntVariable variable) : base(variable) { }
        public static implicit operator int(IntReference reference) => reference.Value;
    }
}
