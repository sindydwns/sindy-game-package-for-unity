using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// IconComponent — SpritePropModel.Sprite 변경 시 Image에 반영되는지 확인
    /// </summary>
    class TestIconComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private readonly Sprite testIcon;
        private SpritePropModel model;

        public TestIconComponentWork(SindyComponent component, Sprite testIcon)
        {
            this.component = component;
            this.testIcon = testIcon;
        }

        public override void Run()
        {
            model = new SpritePropModel();
            model.Sprite.Subscribe(v => Debug.Log($"[Icon] sprite = {(v != null ? v.name : "null")}")).AddTo(disposables);

            component.SetModel(model);

            if (testIcon != null)
            {
                model.Value = testIcon;
                Debug.Log("[Icon] Sprite testIcon applied.");
            }
            else
            {
                Debug.Log("[Icon] No test sprite assigned — skipping assignment.");
            }
        }

        protected override void Cleanup()
        {
            component?.SetModel(null);
            model?.Dispose();
        }
    }
}
