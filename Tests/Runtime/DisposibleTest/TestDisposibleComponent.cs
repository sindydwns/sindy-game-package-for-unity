using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.Test
{
    public class TestDisposibleComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestCaseDisposible());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
