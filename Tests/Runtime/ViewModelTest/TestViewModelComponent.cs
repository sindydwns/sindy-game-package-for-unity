using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.Test
{
    public class TestViewModelComponent : MonoBehaviour
    {
        private readonly List<TestCase> tests = new();

        void Start()
        {
            tests.Add(new TestFormatNumberPropModel());

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }
    }
}
