using System;
using R3;
using Sindy.Common;

namespace Sindy.View.Features
{
    public class HoldFeature : ViewModelFeature
    {
        public Subject<int> OnHold { get; } = new();
        public PropModel<bool> AllowHold { get; }
        public PropModel<bool> KeepHold { get; }

        public HoldFeature(
            bool allowHold = true,
            Action onHold = null)
        {
            AllowHold = new PropModel<bool>(allowHold);
            KeepHold = new PropModel<bool>(false);

            if (onHold != null)
            {
                OnHold.Subscribe(_ => onHold.Invoke()).AddTo(disposables);
            }

            AllowHold.AddTo(this);
            KeepHold.AddTo(this);
        }

        public void Release() => KeepHold.Value = false;

        public override void Dispose()
        {
            base.Dispose();
            OnHold.Dispose();
        }
    }
}
