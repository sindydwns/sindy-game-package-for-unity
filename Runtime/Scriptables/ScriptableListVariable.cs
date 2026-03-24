using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/ScriptableList")]
    public class ScriptableListVariable : ScriptableObjectVariable<List<ScriptableObject>>
    {
        public IEnumerable<T> Cast<T>() where T : ScriptableObject
        {
            return Value.Cast<T>();
        }
    }

    [Serializable]
    public class ScriptableListReference : ScriptableObjectReference<List<ScriptableObject>, ScriptableListVariable>
    {
        public IEnumerable<T> Cast<T>() where T : ScriptableObject
        {
            return Variable.Cast<T>();
        }
        public ScriptableListReference() : base() { }
        public ScriptableListReference(List<ScriptableObject> value) : base(value) { }
        public ScriptableListReference(ScriptableListVariable variable) : base(variable) { }
        public static implicit operator List<ScriptableObject>(ScriptableListReference reference) => reference.Value;
    }
}
