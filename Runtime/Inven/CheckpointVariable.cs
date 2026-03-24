using System;
using Sindy.Scriptables;
using UnityEngine;

namespace Sindy.Inven
{
    [CreateAssetMenu(menuName = "Variables/Checkpoint")]
    public class CheckpointVariable : ScriptableObjectVariable<Checkpoint> { }

    [Serializable]
    public class CheckpointReference : ScriptableObjectReference<Checkpoint, CheckpointVariable>
    {
        public CheckpointReference() : base() { }
        public CheckpointReference(Checkpoint value) : base(value) { }
        public CheckpointReference(CheckpointVariable variable) : base(variable) { }
        public static implicit operator Checkpoint(CheckpointReference reference) => reference.Value;
    }
}
