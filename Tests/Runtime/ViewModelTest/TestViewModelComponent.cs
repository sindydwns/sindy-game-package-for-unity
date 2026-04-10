using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    public class TestViewModelComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestFormatNumberPropModel());
            tests.Add(new TestViewModelCore());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
