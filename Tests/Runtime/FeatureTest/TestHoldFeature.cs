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

        private void DefaultValues()
        {
            var feature = new HoldFeature();

            Assert.IsTrue(feature.AllowHold.Value);
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        private void OnHoldCallbackFires()
        {
            int callCount = 0;
            var feature = new HoldFeature(onHold: () => callCount++);

            feature.OnHold.OnNext(0);
            feature.OnHold.OnNext(1);

            Assert.AreEqual(2, callCount);

            feature.Dispose();
        }

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

        private void AllowHoldToggle()
        {
            var feature = new HoldFeature(allowHold: false);

            Assert.IsFalse(feature.AllowHold.Value);

            feature.AllowHold.Value = true;
            Assert.IsTrue(feature.AllowHold.Value);

            feature.Dispose();
        }

        private void KeepHoldAndRelease()
        {
            var feature = new HoldFeature();

            feature.KeepHold.Value = true;
            Assert.IsTrue(feature.KeepHold.Value);

            feature.Release();
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        private void DisposeDisposesSubject()
        {
            var feature = new HoldFeature();

            feature.Dispose();

            Assert.IsTrue(feature.IsDisposed);
            Assert.IsTrue(feature.OnHold.IsDisposed);
        }
    }
}
