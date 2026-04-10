using System.Collections.Generic;
using Sindy.View.Features;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// HoldFeature — OnHold, AllowHold, KeepHold, Release, 콜백, Dispose
    /// </summary>
    class TestHoldFeature : TestCase
    {
        public override void Run()
        {
            DefaultValues();
            OnHoldCallbackFires();
            OnHoldSubjectFires();
            AllowHoldToggle();
            KeepHoldAndRelease();
            DisposeDisposesSubject();
        }

        // 기본 생성 시 AllowHold=true, KeepHold=false인지 확인
        private void DefaultValues()
        {
            var feature = new HoldFeature();

            Assert.IsTrue(feature.AllowHold.Value);
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        // OnHold 이벤트 발생 시 생성자에서 등록한 콜백이 호출되는지 확인
        private void OnHoldCallbackFires()
        {
            int callCount = 0;
            var feature = new HoldFeature(onHold: () => callCount++);

            feature.OnHold.OnNext(0);
            feature.OnHold.OnNext(1);

            Assert.AreEqual(2, callCount);

            feature.Dispose();
        }

        // OnHold Subject를 구독하여 발행된 값들이 순서대로 수신되는지 확인
        private void OnHoldSubjectFires()
        {
            var feature = new HoldFeature();
            var received = new List<int>();
            feature.OnHold.Subscribe(v => received.Add(v)).AddTo(disposables);

            feature.OnHold.OnNext(0);
            feature.OnHold.OnNext(1);
            feature.OnHold.OnNext(2);

            Assert.AreEqual(3, received.Count);
            Assert.AreEqual(0, received[0]);
            Assert.AreEqual(1, received[1]);
            Assert.AreEqual(2, received[2]);

            feature.Dispose();
        }

        // AllowHold 값을 false로 시작하여 true로 토글할 수 있는지 확인
        private void AllowHoldToggle()
        {
            var feature = new HoldFeature(allowHold: false);

            Assert.IsFalse(feature.AllowHold.Value);

            feature.AllowHold.Value = true;
            Assert.IsTrue(feature.AllowHold.Value);

            feature.Dispose();
        }

        // KeepHold를 true로 설정한 뒤 Release() 호출 시 false로 복귀하는지 확인
        private void KeepHoldAndRelease()
        {
            var feature = new HoldFeature();

            feature.KeepHold.Value = true;
            Assert.IsTrue(feature.KeepHold.Value);

            feature.Release();
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        // Dispose 시 Feature와 내부 OnHold Subject 모두 Dispose되는지 확인
        private void DisposeDisposesSubject()
        {
            var feature = new HoldFeature();

            feature.Dispose();

            Assert.IsTrue(feature.IsDisposed);
            Assert.IsTrue(feature.OnHold.IsDisposed);
        }
    }
}
