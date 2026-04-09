using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// IconComponent — SpritePropModel.Sprite 변경 시 Image에 반영되는지 확인
    /// Sprite는 런타임 에셋 로드가 필요하므로 null → null 전환으로 구독 흐름만 검증
    /// 실제 Sprite 반영은 에디터에서 에셋을 Resources에 넣고 확인할 것
    /// </summary>
    class TestIconComponentWork : TestCase
    {
        private readonly SindyComponent component;
        private readonly Sprite testIcon;

        public TestIconComponentWork(SindyComponent component, Sprite testIcon)
        {
            this.component = component;
            this.testIcon = testIcon;
        }

        public override void Run()
        {
            var icon = new SpritePropModel();

            icon.Sprite
                .Subscribe(v => Debug.Log($"[Icon] sprite = {(v != null ? v.name : "null")}"))
                .AddTo(disposables);

            component.SetModel(icon);

            if (testIcon != null)
            {
                icon.Value = testIcon;
                Debug.Log("[Icon] Sprite testIcon and applied.");
            }
            else
            {
                Debug.Log("[Icon] No test sprite found at Resources/Icons/test_icon — skipping assignment.");
            }
        }
    }
}
