using System.Collections.Generic;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    public class TestRedDotComponent : MonoBehaviour
    {
        [SerializeField] private SindyComponent testComponent;

        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestRedDotDefaultWork());
            tests.Add(new TestRedDotComponentWork(testComponent));

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
