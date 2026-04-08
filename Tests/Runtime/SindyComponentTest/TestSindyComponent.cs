using System.Collections.Generic;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    public class TestSindyComponent : MonoBehaviour
    {
        [SerializeField] private SindyComponent testComponent;

        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
