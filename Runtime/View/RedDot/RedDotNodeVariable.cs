using System;
using Sindy.Scriptables;
using UnityEngine;

namespace Sindy.RedDot
{
    [CreateAssetMenu(menuName = "Variables/RedDotNode")]
    public class RedDotNodeVariable : ScriptableObjectVariable<RedDotNode> { }
    [Serializable]
    public class RedDotNodeReference : ScriptableObjectReference<RedDotNode, RedDotNodeVariable>
    {
        public RedDotNodeReference() : base() { }
        public RedDotNodeReference(RedDotNode value) : base(value) { }
        public RedDotNodeReference(RedDotNodeVariable variable) : base(variable) { }
        public static implicit operator RedDotNode(RedDotNodeReference reference) => reference.Value;
    }
}
