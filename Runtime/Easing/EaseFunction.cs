using System;
using UnityEngine;

namespace Sindy.Easing
{
    /// <summary>
    /// FR-EASE-01. Ease enum, AnimationCurve, Func&lt;float, float&gt; 세 입력을
    /// 단일 함수 표현으로 정규화하는 값 타입.
    /// </summary>
    public readonly struct EaseFunction
    {
        private readonly Func<float, float> func;

        public EaseFunction(Func<float, float> func)
        {
            this.func = func;
        }

        public bool IsDefined => func != null;

        public float Evaluate(float t)
        {
            return func != null ? func(t) : t;
        }

        public static EaseFunction From(Ease ease) => new(EaseFunctions.Get(ease));
        public static EaseFunction From(AnimationCurve curve) => new(curve != null ? curve.Evaluate : null);
        public static EaseFunction From(Func<float, float> f) => new(f);

        public static implicit operator EaseFunction(Ease ease) => From(ease);
        public static implicit operator EaseFunction(AnimationCurve curve) => From(curve);
        public static implicit operator EaseFunction(Func<float, float> f) => From(f);
    }
}
