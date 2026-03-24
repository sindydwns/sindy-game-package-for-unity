using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Bool")]
    public class BoolVariable : ScriptableObjectVariable<bool> { }
    [Serializable]
    public class BoolReference : ScriptableObjectReference<bool, BoolVariable>
    {
        public BoolReference() : base() { }
        public BoolReference(bool value) : base(value) { }
        public BoolReference(BoolVariable variable) : base(variable) { }
        public static implicit operator bool(BoolReference reference) => reference.Value;
    }
}
