using System.Collections.Generic;
using UnityEngine;

namespace Sindy.Test
{
    public class TestReactiveCollectionComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestReactiveList());
            tests.Add(new TestReactiveSet());
            tests.Add(new TestReactiveDictionary());
            tests.Add(new TestReactiveListCondition());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
