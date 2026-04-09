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

        /// <summary>
        /// Dispose 전에 호출됩니다. 컴포넌트 모델 해제 등 명시적 정리를 여기서 수행하세요.
        /// </summary>
        protected virtual void Cleanup() { }

        public void Dispose()
        {
            Cleanup();
            disposables.Dispose();
        }
    }
}
