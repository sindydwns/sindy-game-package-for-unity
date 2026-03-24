using System;
using UnityEngine;

namespace Sindy.Scriptables
{
    [CreateAssetMenu(menuName = "Variables/Object")]
    public class ObjectVariable : ScriptableObjectVariable<object>
    {
        public T Cast<T>() where T : class
        {
            return Value as T;
        }
    }
    [Serializable]
    public class ObjectReference : ScriptableObjectReference<object, ObjectVariable>
    {
        public T Cast<T>() where T : class
        {
            return Variable.Cast<T>();
        }
        public ObjectReference() : base() { }
        public ObjectReference(object value) : base(value) { }
        public ObjectReference(ObjectVariable variable) : base(variable) { }
    }
}
