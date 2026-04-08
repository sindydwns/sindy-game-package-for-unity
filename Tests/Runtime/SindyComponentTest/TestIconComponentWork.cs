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

        public TestIconComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var icon = new SpritePropModel();

            icon.Sprite
                .Subscribe(v => Debug.Log($"[Icon] sprite = {(v != null ? v.name : "null")}"))
                .AddTo(disposables);

            component.SetModel(icon);

            // 에셋이 있는 경우: icon.Value = Resources.Load<Sprite>("Icons/item_sword");
            var loaded = Resources.Load<Sprite>("Icons/test_icon");
            if (loaded != null)
            {
                icon.Value = loaded;
                Debug.Log("[Icon] Sprite loaded and applied.");
            }
            else
            {
                Debug.Log("[Icon] No test sprite found at Resources/Icons/test_icon — skipping assignment.");
            }
        }
    }
}
