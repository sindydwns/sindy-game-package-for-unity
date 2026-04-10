using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    public class TestComponentBuilderComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestComponentBuilder());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
