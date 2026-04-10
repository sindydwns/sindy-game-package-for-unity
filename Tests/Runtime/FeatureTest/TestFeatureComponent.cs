using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    public class TestFeatureComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestFeatureWith());
            tests.Add(new TestInteractableFeature());
            tests.Add(new TestHoldFeature());
            tests.Add(new TestSimpleFeatures());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
