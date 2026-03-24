using UnityEngine;
using System;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Float")]
    public class FloatVariable : ScriptableObjectVariable<float>
    {
        public bool Bool
        {
            get => Value > 0;
            set => Value = value ? 1 : 0;
        }
    }

    [Serializable]
    public class FloatReference : ScriptableObjectReference<float, FloatVariable>
    {
        public FloatReference() : base() { }
        public FloatReference(float value) : base(value) { }
        public FloatReference(FloatVariable variable) : base(variable) { }
        public static implicit operator float(FloatReference reference) => reference.Value;
    }
}
