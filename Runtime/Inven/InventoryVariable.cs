using System;
using UnityEngine;
using Sindy.Scriptables;

namespace Sindy.Inven
{
    [CreateAssetMenu(menuName = "Variables/Inventory")]
    public class InventoryVariable : ScriptableObjectVariable<Inventory> { }

    [Serializable]
    public class InventoryReference : ScriptableObjectReference<Inventory, InventoryVariable>
    {
        public InventoryReference() : base() { }
        public InventoryReference(Inventory value) : base(value) { }
        public InventoryReference(InventoryVariable variable) : base(variable) { }
        public static implicit operator Inventory(InventoryReference reference) => reference.Value;
    }
}
