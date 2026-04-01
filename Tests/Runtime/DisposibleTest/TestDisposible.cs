using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Sindy.Test
{
    public class TestDisposible : MonoBehaviour
    {
        private readonly List<Test> tests = new();

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

    abstract class Test : IDisposable
    {
        protected CompositeDisposable disposables = new();

        public void PrintValue<T>(T value)
        {
            Debug.Log($"Value: {value}");
        }

        public void PrintCompleteValue<T>(T value)
        {
            Debug.Log($"Complete: {value}");
        }

        public abstract void Run();

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
