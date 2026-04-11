using System.Collections.Generic;
using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Components.Composite;
using UnityEngine;

namespace Sindy.Test
{
    public class TestViewComponent : MonoBehaviour
    {
        [SerializeField] private ViewComponent viewComponent;

        private readonly List<TestCase> tests = new();
        private ViewModel viewModel;

        void Start()
        {
            Example1_SimpleNotice();
            Example2_ConfirmWithCallbacks();
            Example3_ToastWithTimer();
            Example4_CharacterProfile();
            Example5_ShopWithTabs();
            Example6_CharacterInventoryWithFilter();

            viewComponent.SetModel(viewModel);
            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
            viewModel?.Dispose();
        }

        // ── 예시 1 ────────────────────────────────────────────────────────────
        // 가장 단순한 형태.
        // 루트 팝업에 모델을 지정하고, 내부 레이블 두 개를 패치한다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example1_SimpleNotice()
        {
            ComponentBlueprint
                .Create("notice_popup").WithModel(() => new PopupModel())
                .Patch("header.title", "label").WithModel(() => new LabelModel("공지"))
                .Patch("body.message", "label").WithModel(() => new LabelModel("서버 점검이 예정되어 있습니다."))
                .Patch("footer.confirm", "button").WithModel(() => new ButtonModel())
                .Open(UILayer.Popup);
        }

        // ── 예시 2 ────────────────────────────────────────────────────────────
        // 확인 / 취소 콜백이 있는 Notice.
        // NoticeModel이 title·content·confirm·cancel을 이미 정의하므로
        // 패치 없이 루트 모델만 넘기면 된다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example2_ConfirmWithCallbacks()
        {
            var model = new NoticeModel("아이템 구매", "정말 구매하시겠습니까?", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("구매 확인"));
            model.Cancel.Subscribe(_ => Debug.Log("구매 취소"));

            ComponentBlueprint
                .Create("notice_popup").WithModel(() => model)
                .Open(UILayer.Popup);
        }

        // ── 예시 3 ────────────────────────────────────────────────────────────
        // 일정 시간 후 자동으로 사라지는 Toast 메시지.
        // 팩토리 람다로 모델을 늦게 생성하고, 타이머 종료 이벤트를 구독한다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example3_ToastWithTimer()
        {
            ComponentBlueprint
                .Create("toast_popup").WithModel(() =>
                {
                    var model = new ToastModel("저장되었습니다.", duration: 2f);
                    model.Timer.IsFinished
                        .Where(finished => finished)
                        .Subscribe(_ => Debug.Log("토스트 종료"));
                    return model;
                })
                .Open(UILayer.Toast);
        }

        // ── 예시 4 ────────────────────────────────────────────────────────────
        // 캐릭터 프로필 팝업.
        // 도메인 데이터를 모델로 변환하는 과정은 Builder 밖에서 처리한다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example4_CharacterProfile()
        {
            var data = new CharacterData
            {
                Name = "아르카나",
                Level = 45,
                Hp = 780,
                MaxHp = 1000,
                Mp = 320,
                MaxMp = 500,
                Attack = 240,
                Defense = 180,
            };

            var model = new CharacterProfileModel(data);
            model.Close.Subscribe(_ => Debug.Log("프로필 닫기"));

            ComponentBlueprint
                .Create("character_profile_popup").WithModel(() => model)
                .Open(UILayer.Popup);
        }

        // ── 예시 5 ────────────────────────────────────────────────────────────
        // 카테고리 탭이 있는 상점.
        // 탭 변경 시 아이템 목록을 갱신하는 로직을 모델 안에 둔다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example5_ShopWithTabs()
        {
            ComponentBlueprint
                .Create("shop_popup").WithModel(() =>
                {
                    var model = new ShopModel();

                    model.Category.Subscribe(index =>
                    {
                        var items = LoadShopItems((ShopCategory)index);
                        model.Items.SetItems(items);
                    });

                    model.Close.Subscribe(_ => Debug.Log("상점 닫기"));
                    return model;
                })
                .Open(UILayer.Popup);
        }

        // ── 예시 6 ────────────────────────────────────────────────────────────
        // 클래스 + 등급 + 검색어 세 가지 필터가 조합된 캐릭터 인벤토리.
        // 필터 중 하나라도 바뀌면 CombineLatest가 즉시 목록을 갱신한다.
        // 검색 바는 기본 팝업에 없으므로 Patch로 외부 프리팹을 주입한다.
        // ─────────────────────────────────────────────────────────────────────
        private void Example6_CharacterInventoryWithFilter()
        {
            ComponentBlueprint
                .Create("character_inventory_popup").WithModel(() =>
                {
                    var model = new CharacterInventoryModel();
                    var filter = model.Filter;

                    Observable.CombineLatest(
                        filter.ClassFilter.Obs,
                        filter.GradeFilter.Obs,
                        filter.SearchText.Obs,
                        (cls, grade, search) => (cls, grade, search)
                    )
                    .Subscribe(f =>
                    {
                        var filtered = FilterCharacters(
                            (CharacterClass)f.cls,
                            (ItemGrade)f.grade,
                            f.search
                        );
                        model.List.SetItems(filtered);
                    });

                    model.Selected.Subscribe(slot =>
                        Debug.Log($"캐릭터 선택: {slot?.Name.Value}")
                    );

                    model.Close.Subscribe(_ => Debug.Log("인벤토리 닫기"));
                    return model;
                })
                .Patch("filter.search_bar", "search_input").WithModel(() => new LabelModel("캐릭터 이름 검색"))
                .Open(UILayer.Popup);
        }

        // ── 가상 데이터 헬퍼 ──────────────────────────────────────────────────

#pragma warning disable IDE0060
        private IReadOnlyList<ShopItemModel> LoadShopItems(ShopCategory category)
            => new List<ShopItemModel>();

        private IReadOnlyList<CharacterSlotModel> FilterCharacters(
            CharacterClass cls, ItemGrade grade, string search)
            => new List<CharacterSlotModel>();
#pragma warning restore IDE0060
    }
}
