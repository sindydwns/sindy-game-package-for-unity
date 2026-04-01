using System;
using R3;
using Sindy.Reactive;
using Sindy.View.Model;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    class TestFormatNumberPropModel : TestCase
    {

        public override void Run()
        {
            Case1();
            Case2();
        }

        /// <summary>
        /// 단순 숫자 포맷팅 테스트
        /// </summary>
        private void Case1()
        {
            FormatNumberPropModel<int> intModel = new();
            FormatNumberPropModel<long> longModel = new();
            FormatNumberPropModel<float> floatModel = new();
            FormatNumberPropModel<double> doubleModel = new();

            intModel.Source.Value = 123;
            Assert.AreEqual("123", intModel.Text.Value);

            longModel.Source.Value = 123456789;
            Assert.AreEqual("123,456,789", longModel.Text.Value);

            floatModel.Source.Value = 123.456f;
            Assert.AreEqual("123.46", floatModel.Text.Value);

            doubleModel.Source.Value = 123.456789;
            Assert.AreEqual("123.46", doubleModel.Text.Value);
        }

        /// <summary>
        /// 소스가 끊기면 모델도 같이 끊어지는지 테스트
        /// </summary>
        private void Case2()
        {
            ReactiveProperty<int> intSource = new(123);
            ReactiveProperty<Func<int, string>> format = new(v => v.ToString());
            FormatNumberPropModel<int> intModel = new(intSource.ToObservableWrap(), format.ToObservableWrap());

            Assert.AreEqual("123", intModel.Text.Value);
            Assert.AreEqual(false, intModel.IsDisposed);
            intSource.Dispose();
            Assert.AreEqual(true, intModel.IsDisposed);
        }
    }
}
