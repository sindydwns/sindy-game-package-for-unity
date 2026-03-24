using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Long")]
    public class LongVariable : ScriptableObjectVariable<long> { }
    [Serializable]
    public class LongReference : ScriptableObjectReference<long, LongVariable>
    {
        public LongReference() : base() { }
        public LongReference(long value) : base(value) { }
        public LongReference(LongVariable variable) : base(variable) { }
        public static implicit operator long(LongReference reference) => reference.Value;
    }
}
