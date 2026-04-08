using R3;
using Sindy.View;
using Sindy.View.Model;
using UnityEngine;

namespace Sindy.Test
{
    /// <summary>
    /// PopupComponent — SetModel(vm) 으로 열기, SetModel(null) 으로 닫기 동작 확인
    /// Inspector에서 PopupComponent의 root를 연결해야 함
    /// views 리스트에 "title" 키로 LabelComponent를 등록해두면 내용도 확인 가능
    /// </summary>
    class TestPopupComponentWork : TestCase
    {
        private readonly SindyComponent component;

        public TestPopupComponentWork(SindyComponent component)
        {
            this.component = component;
        }

        public override void Run()
        {
            var model = new ViewModel();
            var title = new StringPropModel("팝업 타이틀");

            title.Text
                .Subscribe(v => Debug.Log($"[Popup] title = \"{v}\""))
                .AddTo(disposables);

            model["title"] = title;

            // 열기
            Debug.Log("[Popup] Opening...");
            component.SetModel(model);

            title.Value = "타이틀 변경됨";

            // 닫기 — root가 비활성화되어야 함
            Debug.Log("[Popup] Closing...");
            component.SetModel(null);

            // 다시 열기
            Debug.Log("[Popup] Reopening...");
            component.SetModel(model);
        }
    }
}
