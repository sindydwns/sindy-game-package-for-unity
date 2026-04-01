using System;
using R3;
using UnityEngine;

namespace Sindy.Test
{
    abstract class TestCase : IDisposable
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
