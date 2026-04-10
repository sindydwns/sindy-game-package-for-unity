using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    public class TestInventoryComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestInventoryCrud());
            tests.Add(new TestInventoryEvent());
            tests.Add(new TestInventoryMoveTo());
            tests.Add(new TestInventorySetOps());
            tests.Add(new TestInventorySerialize());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
