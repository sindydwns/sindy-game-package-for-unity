using System;
using R3;
using Sindy.Common;
using UnityEngine;

namespace Sindy.View
{
    /// <summary>화면 상태 스냅샷. 값 타입이므로 변화 비교가 정확하다.</summary>
    public readonly struct ScreenState : IEquatable<ScreenState>
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Rect SafeArea;

        public ScreenState(int width, int height, Rect safeArea)
        {
            Width = width;
            Height = height;
            SafeArea = safeArea;
        }

        public float Aspect => Height == 0 ? 0f : (float)Width / Height;
        public bool IsLandscape => Width >= Height;

        public bool Equals(ScreenState other) =>
            Width == other.Width && Height == other.Height && SafeArea == other.SafeArea;

        public override bool Equals(object obj) => obj is ScreenState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Width, Height, SafeArea);
        public override string ToString() => $"{Width}x{Height} (safe {SafeArea})";
    }

    /// <summary>
    /// 화면 상태 자가 갱신 모델. PropModel&lt;ScreenState&gt;를 상속하므로
    /// <see cref="Features.ScreenFeature"/> 생성자에 직접 주입할 수 있다.
    /// TimerModel과 같은 패턴 — EveryUpdate로 폴링하되 값이 바뀔 때만 방출한다.
    /// </summary>
    public class ScreenStateModel : PropModel<ScreenState>
    {
        public ScreenStateModel()
        {
            Prop.Value = Read();

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    var current = Read();
                    if (!current.Equals(Prop.Value))
                        Prop.Value = current;
                })
                .AddTo(disposables);
        }

        private static ScreenState Read() =>
            new(Screen.width, Screen.height, Screen.safeArea);
    }
}
