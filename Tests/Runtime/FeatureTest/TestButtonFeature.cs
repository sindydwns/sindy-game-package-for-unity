using System.Collections.Generic;
using Sindy.View.Features;
using R3;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// ButtonFeature — OnClick/OnHold 발행, AllowHold, KeepHold, Release, Dispose.
    /// 클릭/홀드는 같은 포인터 제스처 공간을 공유하므로 한 Feature의 내부 옵션이다.
    /// </summary>
    class TestButtonFeature : TestCase
    {
        public override void Run()
        {
            DefaultValues();
            AllowHoldConstructorArg();
            OnClickFires();
            OnHoldSubjectFires();
            AllowHoldToggle();
            KeepHoldAndRelease();
            DisposeDisposesSubjects();
        }

        // 기본 생성 시 AllowHold=false, KeepHold=false인지 확인
        private void DefaultValues()
        {
            var feature = new ButtonFeature();

            Assert.IsFalse(feature.AllowHold.Value);
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        // 생성 인자가 곧 "홀드 가능한가?"의 답이 되는지 확인
        private void AllowHoldConstructorArg()
        {
            var feature = new ButtonFeature(allowHold: true);
            Assert.IsTrue(feature.AllowHold.Value);
            feature.Dispose();
        }

        // OnClick 발행이 구독자에게 전달되는지 확인 (코드로 클릭 시뮬레이션)
        private void OnClickFires()
        {
            var feature = new ButtonFeature();
            int clicked = 0;
            feature.OnClick.Subscribe(_ => clicked++).AddTo(disposables);

            feature.OnClick.OnNext(Unit.Default);
            feature.OnClick.OnNext(Unit.Default);

            Assert.AreEqual(2, clicked);
            feature.Dispose();
        }

        // OnHold Subject를 구독하여 발행된 반복 횟수가 순서대로 수신되는지 확인
        private void OnHoldSubjectFires()
        {
            var feature = new ButtonFeature(allowHold: true);
            var received = new List<int>();
            feature.OnHold.Subscribe(v => received.Add(v)).AddTo(disposables);

            feature.OnHold.OnNext(1);
            feature.OnHold.OnNext(2);
            feature.OnHold.OnNext(3);

            Assert.AreEqual(3, received.Count);
            Assert.AreEqual(1, received[0]);
            Assert.AreEqual(3, received[2]);

            feature.Dispose();
        }

        // AllowHold 런타임 토글이 구독자에게 전달되는지 확인
        private void AllowHoldToggle()
        {
            var feature = new ButtonFeature(allowHold: true);
            bool? observed = null;
            feature.AllowHold.Subscribe(v => observed = v).AddTo(disposables);

            feature.AllowHold.Value = false;

            Assert.IsFalse(observed.Value);
            feature.Dispose();
        }

        // KeepHold 설정과 Release() 동작 확인
        private void KeepHoldAndRelease()
        {
            var feature = new ButtonFeature(allowHold: true);

            feature.KeepHold.Value = true;
            Assert.IsTrue(feature.KeepHold.Value);

            feature.Release();
            Assert.IsFalse(feature.KeepHold.Value);

            feature.Dispose();
        }

        // Dispose 후 Subject가 정리되는지 확인
        private void DisposeDisposesSubjects()
        {
            var feature = new ButtonFeature();
            feature.Dispose();

            bool threw = false;
            try
            {
                feature.OnClick.OnNext(Unit.Default);
            }
            catch (System.ObjectDisposedException)
            {
                threw = true;
            }
            Assert.IsTrue(threw, "Dispose 후 OnClick 발행은 ObjectDisposedException이어야 합니다.");
        }
    }
}
