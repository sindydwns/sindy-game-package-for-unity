using System;

namespace Sindy.Easing
{
    /// <summary>
    /// Ease enum 값을 단일 함수 표현(Func&lt;float, float&gt;)으로 매핑한다.
    /// FR-EASE-03. 모든 함수는 t=0일 때 0, t=1일 때 1을 반환한다.
    /// </summary>
    public static class EaseFunctions
    {
        public static readonly Func<float, float> Linear = t => t;

        public static readonly Func<float, float> InQuad = t => t * t;
        public static readonly Func<float, float> OutQuad = t => 1f - (1f - t) * (1f - t);
        public static readonly Func<float, float> InOutQuad = t =>
            t < 0.5f ? 2f * t * t : 1f - 0.5f * (-2f * t + 2f) * (-2f * t + 2f);

        public static readonly Func<float, float> InCubic = t => t * t * t;
        public static readonly Func<float, float> OutCubic = t =>
        {
            var u = 1f - t;
            return 1f - u * u * u;
        };
        public static readonly Func<float, float> InOutCubic = t =>
        {
            if (t < 0.5f) return 4f * t * t * t;
            var u = -2f * t + 2f;
            return 1f - 0.5f * u * u * u;
        };

        public static Func<float, float> Get(Ease ease) => ease switch
        {
            Ease.Linear => Linear,
            Ease.InQuad => InQuad,
            Ease.OutQuad => OutQuad,
            Ease.InOutQuad => InOutQuad,
            Ease.InCubic => InCubic,
            Ease.OutCubic => OutCubic,
            Ease.InOutCubic => InOutCubic,
            _ => Linear,
        };

        public static float Evaluate(this Ease ease, float t) => Get(ease)(t);
    }
}
