using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Components.Composite;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.Test
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ComponentBuilder 100 Use Cases — Blueprint 재사용 버전
    //
    // 이 파일은 ComponentBuilder + Blueprint의 실제 사용 예시입니다.
    // 반복되는 UI 구조를 Blueprint로 추출하여 재사용성을 극대화합니다.
    //
    // ── 발견된 필요 기능 요약 ────────────────────────────────────────
    //
    // [NEED] Scrollable         — Patch 대상에 ScrollRect를 자동 부여
    // [NEED] Grid(columns)      — GridLayoutGroup 지원
    // [NEED] Background(type)   — 팝업 뒤 딤/블러 배경 제어
    // [NEED] OnClose(Action)    — 팝업 닫힘 콜백
    // [NEED] Duration(sec)      — 일정 시간 후 자동 닫힘
    // [NEED] Stretch / Fill     — LayoutElement flexible 비율 지정
    // [NEED] Input(placeholder) — 텍스트 입력 필드
    // [NEED] Separator          — 구분선 요소 삽입
    // [NEED] Conditional(bool)  — 조건부 Patch (false면 스킵)
    // [NEED] Badge(path)        — RedDot 바인딩 단축
    // [NEED] Animate(type)      — 열기/닫기 애니메이션
    // [NEED] Draggable          — 드래그 이동
    // [NEED] FitContent         — ContentSizeFitter 자동 부여
    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static class ComponentBuilderUseCases
    {
        static readonly int Popup = 1;
        static readonly int Toast = 2;
        static readonly int HUD = 0;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 공통 Blueprint — 반복되는 UI 패턴을 한 번만 정의
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // 아이콘 + 라벨 가로 배치
        static readonly ComponentBlueprint IconLabel = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 8)
            .Align(TextAnchor.MiddleLeft)
            .Patch("icon", "icon").Size(width: 32, height: 32)
            .Patch("label", "label");

        // 아이콘 + 라벨 (큰 아이콘)
        static readonly ComponentBlueprint IconLabelLarge = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 12)
            .Align(TextAnchor.MiddleLeft)
            .Patch("icon", "icon").Size(width: 64, height: 64)
            .Patch("label", "label");

        // 제목 + 설명 세로 배치
        static readonly ComponentBlueprint TitleDesc = ComponentBlueprint.Create("container")
            .Layout(Direction.Vertical, spacing: 4)
            .Patch("title", "label")
            .Patch("desc", "label");

        // 제목 + 게이지 바 세로 배치
        static readonly ComponentBlueprint LabelGauge = ComponentBlueprint.Create("container")
            .Layout(Direction.Vertical, spacing: 4)
            .Patch("label", "label")
            .Patch("gauge", "gauge");

        // 확인 + 취소 버튼 가로 배치
        static readonly ComponentBlueprint ButtonPair = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 12)
            .Align(TextAnchor.MiddleCenter)
            .Patch("confirm", "button")
            .Patch("cancel", "button");

        // 스탯 한 줄 (라벨: 값)
        static readonly ComponentBlueprint StatRow = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 8)
            .Patch("name", "label")
            .Patch("value", "label");

        // 스탯 비교 한 줄 (라벨: 이전 → 이후)
        static readonly ComponentBlueprint StatCompareRow = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 8)
            .Patch("name", "label")
            .Patch("old", "label")
            .Patch("arrow", "label").WithModel(() => new LabelModel("→"))
            .Patch("new", "label");

        // 아이콘 + 제목/설명 (아이템 상세 헤더)
        static readonly ComponentBlueprint ItemHeader = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 12)
            .Align(TextAnchor.MiddleLeft)
            .Patch("icon", "icon").Size(width: 96, height: 96)
            .Patch("info", TitleDesc);

        // 재료 슬롯 가로 목록
        static readonly ComponentBlueprint MaterialRow = ComponentBlueprint.Create("container")
            .Layout(Direction.Horizontal, spacing: 8)
            .Align(TextAnchor.MiddleCenter)
            .Patch("materials", "list");

        // 탭 + 리스트 (필터링 목록)
        static readonly ComponentBlueprint FilteredList = ComponentBlueprint.Create("container")
            .Layout(Direction.Vertical, spacing: 8)
            .Patch("tabs", "tab")
            .Patch("list", "list");
            // [NEED] .Scrollable() on list

        // 팝업 기본 구조 (제목 + 본문 + 확인 버튼)
        static readonly ComponentBlueprint SimplePopup = ComponentBlueprint.Create("popup")
            .WithModel(() => new PopupModel())
            .Layout(Direction.Vertical, spacing: 12)
            .Padding(16)
            .Patch("title", "label")
            .Patch("body", "label")
            .Patch("confirm", "button").WithModel(() => new ButtonModel());

        // 팝업 기본 구조 (제목 + 본문 + 확인/취소)
        static readonly ComponentBlueprint ConfirmPopup = ComponentBlueprint.Create("popup")
            .WithModel(() => new PopupModel())
            .Layout(Direction.Vertical, spacing: 12)
            .Padding(16)
            .Patch("title", "label")
            .Patch("body", "label")
            .Patch("buttons", ButtonPair);

        // 토스트 메시지
        static readonly ComponentBlueprint ToastMsg = ComponentBlueprint.Create("toast")
            .WithModel(() => new PopupModel())
            .Patch("message", "label");
            // [NEED] .Duration(2f)


        // ── 1~10: 알림/확인 ──────────────────────────────────────────

        // 1. 단순 공지 팝업
        static void SimpleNotice()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("공지사항"))
                .Patch("body", "label").WithModel(() => new LabelModel("서버 점검이 예정되어 있습니다."))
                .Open(Popup);
        }

        // 2. 확인/취소 선택 팝업
        static void ConfirmCancel()
        {
            var confirm = new ButtonModel();
            var cancel = new ButtonModel();
            confirm.Subscribe(_ => Debug.Log("구매"));
            cancel.Subscribe(_ => Debug.Log("취소"));

            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("아이템 구매"))
                .Patch("body", "label").WithModel(() => new LabelModel("정말 구매하시겠습니까?"))
                .Patch("buttons.confirm", "button").WithModel(() => confirm)
                .Patch("buttons.cancel", "button").WithModel(() => cancel)
                .Open(Popup);
        }

        // 3. 삭제 확인 팝업 (위험 동작)
        static void DeleteConfirm()
        {
            var confirm = new ButtonModel();
            confirm.Subscribe(_ => Debug.Log("삭제 실행"));

            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("캐릭터 삭제"))
                .Patch("body", "label").WithModel(() => new LabelModel("이 작업은 되돌릴 수 없습니다."))
                .Patch("buttons.confirm", "button").WithModel(() => confirm)
                .Open(Popup);
        }

        // 4. 자동 닫힘 토스트
        static void AutoCloseToast()
        {
            ComponentBlueprint.Create(ToastMsg)
                .Patch("message", "label").WithModel(() => new LabelModel("저장되었습니다."))
                // [NEED] .Duration(2f)
                .Open(Toast);
        }

        // 5. 네트워크 오류 재시도 팝업
        static void NetworkRetry()
        {
            var retry = new ButtonModel();
            retry.Subscribe(_ => Debug.Log("재시도"));

            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("연결 실패"))
                .Patch("body", "label").WithModel(() => new LabelModel("네트워크 연결을 확인해주세요."))
                .Patch("confirm", "button").WithModel(() => retry)
                .Open(Popup);
        }

        // 6. 서버 점검 안내 (타이머)
        static void MaintenanceTimer()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("서버 점검 중"))
                .Patch("body", "label").WithModel(() => new TimerModel(3600f, @"hh\:mm\:ss"))
                .Open(Popup);
        }

        // 7. 앱 업데이트 강제 안내
        static void ForceUpdate()
        {
            var go = new ButtonModel();
            go.Subscribe(_ => Application.OpenURL("market://details?id=com.example"));

            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("업데이트 필요"))
                .Patch("body", "label").WithModel(() => new LabelModel("최신 버전으로 업데이트 해주세요."))
                .Patch("confirm", "button").WithModel(() => go)
                .Open(Popup);
        }

        // 8. 이용약관 동의 팝업
        // [NEED] Scrollable
        static void TermsOfService()
        {
            var agree = new ToggleModel(false);
            var confirm = new ButtonModel().With(new InteractableFeature(false));
            agree.Subscribe(v => confirm.Feature<InteractableFeature>().Interactable.Value = v);

            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("이용약관"))
                .Patch("body", "label").WithModel(() => new LabelModel("약관 내용..."))
                    // [NEED] .Scrollable()
                .Patch("agree", "toggle").WithModel(() => agree)
                .Patch("confirm", "button").WithModel(() => confirm)
                .Open(Popup);
        }

        // 9. 보상 획득 연출 팝업
        static void RewardPopup()
        {
            var rewards = new ListModel();

            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("보상 획득!"))
                .Patch("body", "list").WithModel(() => rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Open(Popup);
        }

        // 10. 레벨업 축하 팝업 (스탯 비교)
        static void LevelUp(int oldLv, int newLv, int oldAtk, int newAtk)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(20)
                .Patch("title", "label").WithModel(() => new LabelModel($"Lv.{newLv} 달성!"))
                .Patch("level", StatCompareRow)
                    .WithModel(() => new ViewModel())
                .Patch("level.name", "label").WithModel(() => new LabelModel("레벨"))
                .Patch("level.old", "label").WithModel(() => new LabelModel($"{oldLv}"))
                .Patch("level.new", "label").WithModel(() => new LabelModel($"{newLv}"))
                .Patch("atk", StatCompareRow)
                    .WithModel(() => new ViewModel())
                .Patch("atk.name", "label").WithModel(() => new LabelModel("공격력"))
                .Patch("atk.old", "label").WithModel(() => new LabelModel($"{oldAtk}"))
                .Patch("atk.new", "label").WithModel(() => new LabelModel($"{newAtk}"))
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }


        // ── 11~20: 상점 ─────────────────────────────────────────────

        // 11. 카테고리 탭 상점 메인
        static void ShopMain()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("content", FilteredList).WithModel(() => new ViewModel())
                .Patch("content.tabs", "tab").WithModel(() => new TabModel(0))
                .Patch("content.list", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 12. 상품 상세 팝업 (아이콘+이름+설명+가격)
        static void ShopItemDetail(Sprite icon, string name, string desc, int price)
        {
            var buy = new ButtonModel();
            buy.Subscribe(_ => Debug.Log($"구매: {name}"));

            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("header", ItemHeader).WithModel(() => new ViewModel())
                .Patch("header.icon", "icon").WithModel(() => new IconModel(icon))
                .Patch("header.info.title", "label").WithModel(() => new LabelModel(name))
                .Patch("header.info.desc", "label").WithModel(() => new LabelModel(desc))
                .Patch("price", "label").WithModel(() => new LabelModel($"{price:n0} 골드"))
                .Patch("buy", "button").WithModel(() => buy)
                .Open(Popup);
        }

        // 13. 수량 선택 구매 팝업
        // [NEED] Slider / 수량 조절 컴포넌트
        static void QuantityPurchase(string itemName, int unitPrice)
        {
            var qty = new PropModel<int>(1);
            var total = new FormatNumberPropModel<int>(unitPrice, v => $"총액: {v:n0}");
            qty.Subscribe(q => total.Source.Value = q * unitPrice);

            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{itemName} 구매"))
                .Patch("body", "label").WithModel(() => total)
                .Open(Popup);
        }

        // 14. 패키지 번들 상품
        static void BundlePackage()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("스타터 패키지"))
                .Patch("body", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 15. 기간 한정 세일 배너
        static void TimeLimitedSale()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("50% 할인!"))
                .Patch("body", "label").WithModel(() => new TimerModel(86400f, @"dd\:hh\:mm\:ss"))
                .Open(Popup);
        }

        // 16. 구매 완료 영수증
        static void PurchaseReceipt(string itemName, int amount, int totalCost)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("구매 완료"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{itemName} x{amount}\n사용: {totalCost:n0} 골드"))
                .Open(Popup);
        }

        // 17. 무료 일일 상품 수령
        static void DailyFreeItem(Sprite icon)
        {
            var claim = new ButtonModel();
            claim.Subscribe(_ => Debug.Log("수령 완료"));

            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("오늘의 무료 선물"))
                .Patch("icon", "icon").WithModel(() => new IconModel(icon))
                    .Size(width: 96, height: 96)
                .Patch("claim", "button").WithModel(() => claim)
                .Open(Popup);
        }

        // 18. VIP 전용 상점
        static void VipShop(int vipLevel)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("header", "label").WithModel(() => new LabelModel($"VIP {vipLevel} 전용 상점"))
                .Patch("items", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 19. 실제 결제 확인
        static void RealMoneyPurchase(string productName, string price)
        {
            var confirm = new ButtonModel();
            confirm.Subscribe(_ => Debug.Log("결제 진행"));

            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{productName} 구매"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{price}이(가) 결제됩니다."))
                .Patch("buttons.confirm", "button").WithModel(() => confirm)
                .Open(Popup);
        }

        // 20. 구매 이력 목록
        // [NEED] Scrollable
        static void PurchaseHistory()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("구매 이력"))
                .Patch("list", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }


        // ── 21~30: 인벤토리/장비 ────────────────────────────────────

        // 21. 인벤토리 그리드
        // [NEED] Grid(columns), Scrollable
        static void InventoryGrid()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("content", FilteredList).WithModel(() => new ViewModel())
                .Patch("content.tabs", "tab").WithModel(() => new TabModel(0))
                .Patch("content.list", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(4).Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 22. 아이템 상세 팝업
        static void ItemDetail(Sprite icon, string name, string desc)
        {
            var use = new ButtonModel();
            use.Subscribe(_ => Debug.Log($"사용: {name}"));

            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("header", ItemHeader).WithModel(() => new ViewModel())
                .Patch("header.icon", "icon").WithModel(() => new IconModel(icon))
                .Patch("header.info.title", "label").WithModel(() => new LabelModel(name))
                .Patch("header.info.desc", "label").WithModel(() => new LabelModel(desc))
                .Patch("use", "button").WithModel(() => use)
                .Open(Popup);
        }

        // 23. 장비 비교 팝업 (현재 vs 신규) — StatCompareRow 재사용
        static void EquipCompare(string curName, int curAtk, string newName, int newAtk)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("장비 비교"))
                .Patch("name", StatCompareRow).WithModel(() => new ViewModel())
                .Patch("name.name", "label").WithModel(() => new LabelModel("이름"))
                .Patch("name.old", "label").WithModel(() => new LabelModel(curName))
                .Patch("name.new", "label").WithModel(() => new LabelModel(newName))
                .Patch("atk", StatCompareRow).WithModel(() => new ViewModel())
                .Patch("atk.name", "label").WithModel(() => new LabelModel("ATK"))
                .Patch("atk.old", "label").WithModel(() => new LabelModel($"{curAtk}"))
                .Patch("atk.new", "label").WithModel(() => new LabelModel($"{newAtk}"))
                .Patch("equip", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 24. 아이템 판매 수량 선택
        static void SellQuantity(string itemName)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{itemName} 판매"))
                .Patch("body", "label").WithModel(() => new LabelModel("수량을 선택하세요."))
                // [NEED] SliderModel
                .Open(Popup);
        }

        // 25. 일괄 판매 선택 화면
        // [NEED] Grid, Scrollable
        static void BulkSellSelect()
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("판매할 아이템 선택"))
                .Patch("body", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(4).Scrollable()
                .Open(Popup);
        }

        // 26. 장비 강화 화면
        static void EnhanceEquipment(string equipName, float successRate)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel($"{equipName} 강화"))
                .Patch("materials", MaterialRow).WithModel(() => new ViewModel())
                .Patch("materials.materials", "list").WithModel(() => new ListModel())
                .Patch("rate", "label").WithModel(() => new LabelModel($"성공 확률: {successRate:P0}"))
                .Patch("enhance", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 27. 강화 결과 팝업
        // [NEED] Animate(type)
        static void EnhanceResult(bool success, string equipName)
        {
            ComponentBlueprint.Create(SimplePopup)
                // [NEED] .Animate(success ? AnimType.Celebrate : AnimType.Shake)
                .Patch("title", "label").WithModel(() => new LabelModel(success ? "강화 성공!" : "강화 실패"))
                .Patch("body", "label").WithModel(() => new LabelModel(equipName))
                .Open(Popup);
        }

        // 28. 룬 소켓 편집
        static void RuneSocket()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("룬 소켓"))
                .Patch("slots", MaterialRow).WithModel(() => new ViewModel())
                .Patch("slots.materials", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 29. 세트 효과 미리보기
        static void SetEffectPreview(string setName, string bonus2, string bonus4)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{setName} 세트"))
                .Patch("body", "label").WithModel(() => new LabelModel($"2세트: {bonus2}\n4세트: {bonus4}"))
                .Open(Popup);
        }

        // 30. 아이템 잠금/즐겨찾기 토글 확인
        static void LockToggleConfirm(string itemName, bool willLock)
        {
            var msg = willLock ? $"{itemName}을(를) 잠금하시겠습니까?" : $"{itemName}의 잠금을 해제하시겠습니까?";
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("아이템 잠금"))
                .Patch("body", "label").WithModel(() => new LabelModel(msg))
                .Open(Popup);
        }


        // ── 31~40: 캐릭터/영웅 ──────────────────────────────────────

        // 31. 캐릭터 프로필 팝업 — LabelGauge 재사용
        static void CharacterProfile(Sprite portrait, string name, float hpRatio, float mpRatio)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("portrait", "icon").WithModel(() => new IconModel(portrait))
                    .Size(width: 200, height: 200)
                .Patch("name", "label").WithModel(() => new LabelModel(name))
                .Patch("hp", LabelGauge).WithModel(() => new ViewModel())
                .Patch("hp.label", "label").WithModel(() => new LabelModel("HP"))
                .Patch("hp.gauge", "gauge").WithModel(() => new GaugeModel(hpRatio))
                .Patch("mp", LabelGauge).WithModel(() => new ViewModel())
                .Patch("mp.label", "label").WithModel(() => new LabelModel("MP"))
                .Patch("mp.gauge", "gauge").WithModel(() => new GaugeModel(mpRatio))
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 32. 캐릭터 목록 (필터 + 그리드)
        // [NEED] Grid, Scrollable, Input
        static void CharacterList()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("content", FilteredList).WithModel(() => new ViewModel())
                .Patch("content.tabs", "tab").WithModel(() => new TabModel(0))
                .Patch("content.list", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(4).Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 33. 스킬 상세 팝업 — IconLabel 재사용
        static void SkillDetail(Sprite skillIcon, string skillName, int level, string desc)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("header", IconLabelLarge).WithModel(() => new ViewModel())
                .Patch("header.icon", "icon").WithModel(() => new IconModel(skillIcon))
                .Patch("header.label", "label").WithModel(() => new LabelModel($"{skillName} Lv.{level}"))
                .Patch("desc", "label").WithModel(() => new LabelModel(desc))
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 34. 캐릭터 승급 화면
        static void PromoteCharacter(string name, int curStar, int nextStar)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel($"{name} 승급"))
                .Patch("stars", StatCompareRow).WithModel(() => new ViewModel())
                .Patch("stars.name", "label").WithModel(() => new LabelModel("등급"))
                .Patch("stars.old", "label").WithModel(() => new LabelModel(new string('★', curStar)))
                .Patch("stars.new", "label").WithModel(() => new LabelModel(new string('★', nextStar)))
                .Patch("materials", MaterialRow).WithModel(() => new ViewModel())
                .Patch("materials.materials", "list").WithModel(() => new ListModel())
                .Patch("promote", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 35. 호감도/친밀도 화면 — LabelGauge 재사용
        static void Affinity(string charName, float progress)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("affinity", LabelGauge).WithModel(() => new ViewModel())
                .Patch("affinity.label", "label").WithModel(() => new LabelModel(charName))
                .Patch("affinity.gauge", "gauge").WithModel(() => new GaugeModel(progress))
                .Patch("gift", "button").WithModel(() => new ButtonModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 36. 캐릭터 스킨 선택
        // [NEED] Scrollable (가로)
        static void SkinSelect()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("스킨 선택"))
                .Patch("skins", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 12)
                    // [NEED] .Scrollable()
                .Patch("equip", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 37. 스킬 레벨업 확인
        static void SkillLevelUp(string skillName, int curLv, int costGold)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{skillName} 레벨업"))
                .Patch("body", "label").WithModel(() => new LabelModel($"Lv.{curLv} → Lv.{curLv + 1}\n비용: {costGold:n0} 골드"))
                .Open(Popup);
        }

        // 38. 파티 편성 화면
        // [NEED] Draggable
        static void PartyFormation()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("파티 편성"))
                .Patch("slots", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 16)
                    .Align(TextAnchor.MiddleCenter)
                    // [NEED] .Draggable()
                .Patch("reserve", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(5).Scrollable()
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 39. 각성/돌파 확인
        static void AwakeningConfirm(string charName, int stage)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"{charName} 각성 {stage}단계"))
                .Patch("body", "label").WithModel(() => new LabelModel("각성 재료가 소모됩니다."))
                .Open(Popup);
        }

        // 40. 장비 자동 장착 결과
        static void AutoEquipResult()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("자동 장착 완료"))
                .Patch("body", "label").WithModel(() => new LabelModel("최적의 장비가 장착되었습니다."))
                .Open(Popup);
        }


        // ── 41~50: 소셜/채팅/우편 ───────────────────────────────────

        // 41. 친구 목록
        // [NEED] Scrollable
        static void FriendList()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("친구 목록"))
                .Patch("friends", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 42. 친구 요청 팝업
        static void FriendRequest(string playerName)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("친구 요청"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{playerName}님의 친구 요청을 수락하시겠습니까?"))
                .Open(Popup);
        }

        // 43. 타 플레이어 프로필
        static void PlayerProfile(Sprite avatar, string name, int level, int power)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("header", IconLabelLarge).WithModel(() => new ViewModel())
                .Patch("header.icon", "icon").WithModel(() => new IconModel(avatar))
                .Patch("header.label", "label").WithModel(() => new LabelModel($"{name} (Lv.{level})"))
                .Patch("power", StatRow).WithModel(() => new ViewModel())
                .Patch("power.name", "label").WithModel(() => new LabelModel("전투력"))
                .Patch("power.value", "label").WithModel(() => new LabelModel($"{power:n0}"))
                .Patch("buttons", ButtonPair).WithModel(() => new ViewModel())
                .Open(Popup);
        }

        // 44. 우편함 목록
        // [NEED] Scrollable, Badge
        static void MailBox()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("우편함"))
                    // [NEED] .Badge("mail")
                .Patch("mails", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("claim_all", "button").WithModel(() => new ButtonModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 45. 우편 상세 (첨부 아이템)
        static void MailDetail(string title, string body, Sprite attachIcon)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel(title))
                .Patch("body", "label").WithModel(() => new LabelModel(body))
                .Patch("attach", IconLabel).WithModel(() => new ViewModel())
                .Patch("attach.icon", "icon").WithModel(() => new IconModel(attachIcon))
                .Patch("attach.label", "label").WithModel(() => new LabelModel("첨부 아이템"))
                .Patch("claim", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 46. 길드 정보 팝업
        static void GuildInfo(string guildName, int memberCount, int maxMembers)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("name", "label").WithModel(() => new LabelModel(guildName))
                .Patch("members", StatRow).WithModel(() => new ViewModel())
                .Patch("members.name", "label").WithModel(() => new LabelModel("인원"))
                .Patch("members.value", "label").WithModel(() => new LabelModel($"{memberCount}/{maxMembers}"))
                .Patch("buttons", ButtonPair).WithModel(() => new ViewModel())
                .Open(Popup);
        }

        // 47. 길드 가입 신청 확인
        static void GuildJoinConfirm(string guildName)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("길드 가입"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{guildName}에 가입 신청하시겠습니까?"))
                .Open(Popup);
        }

        // 48. 채팅 이모티콘 선택
        // [NEED] Grid
        static void EmoticonPicker()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("emoticons", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(6)
                .Open(Popup);
        }

        // 49. 신고/차단 확인
        static void ReportConfirm(string playerName)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("신고"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{playerName}님을 신고하시겠습니까?"))
                .Open(Popup);
        }

        // 50. 선물 보내기 팝업
        static void SendGift(string targetName)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("title", "label").WithModel(() => new LabelModel($"{targetName}에게 선물"))
                .Patch("items", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("send", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }


        // ── 51~60: 퀘스트/미션/도전 ────────────────────────────────

        // 51. 퀘스트 목록
        // [NEED] Scrollable
        static void QuestList()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("content", FilteredList).WithModel(() => new ViewModel())
                .Patch("content.tabs", "tab").WithModel(() => new TabModel(0))
                .Patch("content.list", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 52. 퀘스트 상세 (목표 + 보상)
        static void QuestDetail(string title, string objective, string reward)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel(title))
                .Patch("objective", IconLabel).WithModel(() => new ViewModel())
                .Patch("objective.label", "label").WithModel(() => new LabelModel(objective))
                .Patch("reward", IconLabel).WithModel(() => new ViewModel())
                .Patch("reward.label", "label").WithModel(() => new LabelModel(reward))
                .Patch("accept", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 53. 일일 미션 진행도
        static void DailyMissions(int completed, int total)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("header", LabelGauge).WithModel(() => new ViewModel())
                .Patch("header.label", "label").WithModel(() => new LabelModel($"일일 미션 ({completed}/{total})"))
                .Patch("header.gauge", "gauge").WithModel(() => new GaugeModel((float)completed / total))
                .Patch("missions", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 54. 업적 달성 토스트
        static void AchievementToast(string achievementName)
        {
            ComponentBlueprint.Create(ToastMsg)
                .Patch("message", "label").WithModel(() => new LabelModel($"업적 달성: {achievementName}"))
                .Open(Toast);
        }

        // 55. 미션 보상 일괄 수령 확인
        static void ClaimAllMissions(int count)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("보상 수령"))
                .Patch("body", "label").WithModel(() => new LabelModel($"완료된 {count}개 미션 보상을 모두 수령하시겠습니까?"))
                .Open(Popup);
        }

        // 56. 주간 도전 진행도
        static void WeeklyChallenge(float progress)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("header", LabelGauge).WithModel(() => new ViewModel())
                .Patch("header.label", "label").WithModel(() => new LabelModel("주간 도전"))
                .Patch("header.gauge", "gauge").WithModel(() => new GaugeModel(progress))
                .Patch("tasks", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 57. 시즌 패스 보상 트랙
        static void SeasonPassTrack(int currentTier)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel($"시즌 패스 (Tier {currentTier})"))
                .Patch("rewards", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 4)
                    // [NEED] .Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 58. 퀘스트 포기 확인
        static void AbandonQuest(string questName)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("퀘스트 포기"))
                .Patch("body", "label").WithModel(() => new LabelModel($"'{questName}'을(를) 포기하시겠습니까?"))
                .Open(Popup);
        }

        // 59. 이벤트 퀘스트 기간 안내
        static void EventQuestInfo(string eventName, float remainSec)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel(eventName))
                .Patch("body", "label").WithModel(() => new TimerModel(remainSec, @"dd\일\ hh\시\간"))
                .Open(Popup);
        }

        // 60. 도전 모드 입장 확인
        static void ChallengeModeEntry(string modeName, int staminaCost)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel(modeName))
                .Patch("body", "label").WithModel(() => new LabelModel($"체력 {staminaCost}을 소모합니다."))
                .Open(Popup);
        }


        // ── 61~70: 설정/시스템 ──────────────────────────────────────

        // 61. 설정 메인 — IconLabel 재사용
        static void SettingsMain()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 8).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("설정"))
                .Patch("sound", IconLabel).WithModel(() => new ViewModel())
                .Patch("sound.label", "label").WithModel(() => new LabelModel("사운드"))
                .Patch("music", IconLabel).WithModel(() => new ViewModel())
                .Patch("music.label", "label").WithModel(() => new LabelModel("음악"))
                .Patch("vibration", IconLabel).WithModel(() => new ViewModel())
                .Patch("vibration.label", "label").WithModel(() => new LabelModel("진동"))
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 62. 언어 선택
        static void LanguageSelect()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("언어 선택"))
                .Patch("languages", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 63. 계정 연동 화면
        static void AccountLink()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("계정 연동"))
                .Patch("google", "button").WithModel(() => new ButtonModel())
                .Patch("apple", "button").WithModel(() => new ButtonModel())
                .Patch("facebook", "button").WithModel(() => new ButtonModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 64. 로그아웃 확인
        static void LogoutConfirm()
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("로그아웃"))
                .Patch("body", "label").WithModel(() => new LabelModel("정말 로그아웃 하시겠습니까?"))
                .Open(Popup);
        }

        // 65. 계정 삭제 확인 (이중 확인)
        static void DeleteAccount()
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("계정 삭제"))
                .Patch("body", "label").WithModel(() => new LabelModel("모든 데이터가 영구 삭제됩니다.\n이 작업은 되돌릴 수 없습니다."))
                .Open(Popup);
        }

        // 66. 데이터 다운로드 진행
        static void DownloadProgress(float progress)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("title", "label").WithModel(() => new LabelModel("데이터 다운로드"))
                .Patch("progress", LabelGauge).WithModel(() => new ViewModel())
                .Patch("progress.label", "label").WithModel(() => new FormatNumberPropModel<float>(progress, v => $"{v:P0}"))
                .Patch("progress.gauge", "gauge").WithModel(() => new GaugeModel(progress))
                .Open(Popup);
        }

        // 67. 푸시 알림 설정
        static void PushNotificationSettings()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 8).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("알림 설정"))
                .Patch("event", "toggle").WithModel(() => new ToggleModel(true))
                .Patch("stamina", "toggle").WithModel(() => new ToggleModel(true))
                .Patch("friend", "toggle").WithModel(() => new ToggleModel(false))
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 68. 그래픽 품질 설정
        static void GraphicsSettings()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10).Padding(16)
                .Patch("title", "label").WithModel(() => new LabelModel("그래픽 설정"))
                .Patch("quality", "tab").WithModel(() => new TabModel(1))
                .Patch("fps", "tab").WithModel(() => new TabModel(0))
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 69. 고객센터/FAQ
        // [NEED] Scrollable
        static void CustomerService()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("고객센터"))
                .Patch("faq", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("contact", "button").WithModel(() => new ButtonModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 70. 쿠폰 입력
        // [NEED] Input
        static void CouponInput()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("쿠폰 입력"))
                .Patch("body", "label").WithModel(() => new LabelModel("쿠폰 코드를 입력해주세요."))
                // [NEED] .Patch("input", "input").WithModel(() => new InputModel("코드 입력..."))
                .Open(Popup);
        }


        // ── 71~80: 전투/콘텐츠 ──────────────────────────────────────

        // 71. 전투 결과 — 승리
        static void BattleVictory(int expGain, int goldGain)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12).Padding(20)
                // [NEED] .Animate(AnimType.Celebrate)
                .Patch("title", "label").WithModel(() => new LabelModel("승리!"))
                .Patch("exp", StatRow).WithModel(() => new ViewModel())
                .Patch("exp.name", "label").WithModel(() => new LabelModel("경험치"))
                .Patch("exp.value", "label").WithModel(() => new LabelModel($"+{expGain:n0}"))
                .Patch("gold", StatRow).WithModel(() => new ViewModel())
                .Patch("gold.name", "label").WithModel(() => new LabelModel("골드"))
                .Patch("gold.value", "label").WithModel(() => new LabelModel($"+{goldGain:n0}"))
                .Patch("rewards", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 72. 전투 결과 — 패배
        static void BattleDefeat()
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("패배..."))
                .Patch("body", "label").WithModel(() => new LabelModel("다시 도전하시겠습니까?"))
                .Open(Popup);
        }

        // 73. 스테이지 선택 화면
        static void StageSelect(int chapter)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel($"챕터 {chapter}"))
                .Patch("stages", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(3)
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 74. 보스 출현 알림
        static void BossAlert(string bossName)
        {
            ComponentBlueprint.Create(ToastMsg)
                .Patch("message", "label").WithModel(() => new LabelModel($"보스 출현: {bossName}!"))
                .Open(Toast);
        }

        // 75. 체력 부족 충전 팝업
        static void StaminaRefill(int current, int max, int cost)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("체력 부족"))
                .Patch("body", "label").WithModel(() => new LabelModel($"현재 체력: {current}/{max}\n{cost} 다이아로 회복하시겠습니까?"))
                .Open(Popup);
        }

        // 76. PvP 매칭 대기
        static void PvpMatchmaking()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Align(TextAnchor.MiddleCenter)
                .Patch("title", "label").WithModel(() => new LabelModel("상대 검색 중..."))
                .Patch("timer", "label").WithModel(() => new TimerModel(0f))
                .Patch("cancel", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 77. PvP 시즌 순위
        // [NEED] Scrollable
        static void PvpRanking()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("PvP 랭킹"))
                .Patch("ranking", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 78. 던전 입장 확인
        static void DungeonEntry(string dungeonName, int recommendPower)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel(dungeonName))
                .Patch("body", "label").WithModel(() => new LabelModel($"추천 전투력: {recommendPower:n0}"))
                .Open(Popup);
        }

        // 79. 소탕 (스윕) 결과
        static void SweepResult(int count)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel($"소탕 완료 ({count}회)"))
                .Patch("body", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                .Open(Popup);
        }

        // 80. 부활/계속 확인 (다이아 소모)
        static void ReviveConfirm(int cost)
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("부활"))
                .Patch("body", "label").WithModel(() => new LabelModel($"다이아 {cost}개로 부활하시겠습니까?"))
                .Open(Popup);
        }


        // ── 81~90: 가챠/뽑기/이벤트 ────────────────────────────────

        // 81. 뽑기 메인 화면
        static void GachaMain()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 16).Padding(16)
                .Patch("banner", "icon").WithModel(() => new IconModel())
                    .Size(width: 600, height: 300)
                .Patch("buttons", "container")
                    .Layout(Direction.Horizontal, spacing: 12)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("buttons.single", "button").WithModel(() => new ButtonModel())
                .Patch("buttons.ten", "button").WithModel(() => new ButtonModel())
                .Patch("info", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 82. 뽑기 결과 연출
        // [NEED] Animate
        static void GachaResult()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                // [NEED] .Animate(AnimType.GachaReveal)
                .Patch("items", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 83. 뽑기 확률표
        // [NEED] Scrollable
        static void GachaProbability()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("확률 정보"))
                .Patch("table", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 84. 출석 체크 캘린더
        // [NEED] Grid
        static void AttendanceCalendar(int day)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel($"출석 {day}일차"))
                .Patch("calendar", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(7)
                .Patch("claim", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 85. 룰렛/행운의 바퀴
        // [NEED] Animate
        static void LuckyWheel()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 16)
                .Align(TextAnchor.MiddleCenter)
                .Patch("wheel", "icon").WithModel(() => new IconModel())
                    .Size(width: 400, height: 400)
                    // [NEED] .Animate(AnimType.Spin)
                .Patch("spin", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 86. 이벤트 배너 팝업
        static void EventBanner(Sprite bannerImage)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("banner", "icon").WithModel(() => new IconModel(bannerImage))
                    .Size(width: 600, height: 400)
                .Patch("go", "button").WithModel(() => new ButtonModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 87. 초보자 7일 이벤트
        static void NewbieEvent(int day)
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("header", LabelGauge).WithModel(() => new ViewModel())
                .Patch("header.label", "label").WithModel(() => new LabelModel($"신규 유저 이벤트 ({day}/7)"))
                .Patch("header.gauge", "gauge").WithModel(() => new GaugeModel(day / 7f))
                .Patch("rewards", "list").WithModel(() => new ListModel())
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 88. 교환소 (토큰으로 아이템 교환)
        static void TokenExchange()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("content", FilteredList).WithModel(() => new ViewModel())
                .Patch("content.tabs", "tab").WithModel(() => new TabModel(0))
                .Patch("content.list", "list").WithModel(() => new ListModel())
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 89. 한정 뽑기 보장 카운터
        static void PityCounter(int current, int max)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("보장 카운터"))
                .Patch("body", "label").WithModel(() => new LabelModel($"{current}/{max}회 — {max - current}회 후 확정 획득"))
                .Open(Popup);
        }

        // 90. 뽑기 토큰 부족 충전 유도
        static void InsufficientGems(int have, int need)
        {
            var goShop = new ButtonModel();
            goShop.Subscribe(_ => Debug.Log("상점 이동"));

            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("다이아 부족"))
                .Patch("body", "label").WithModel(() => new LabelModel($"보유: {have:n0} / 필요: {need:n0}\n상점으로 이동하시겠습니까?"))
                .Patch("buttons.confirm", "button").WithModel(() => goShop)
                .Open(Popup);
        }


        // ── 91~100: HUD/유틸 ────────────────────────────────────────

        // 91. HUD 상단 바 (골드 + 다이아 + 체력)
        static void HudTopBar(int gold, int gem, int stamina, int maxStamina)
        {
            ComponentBlueprint.Create("hud_top").WithModel(() => new ViewModel())
                .Layout(Direction.Horizontal, spacing: 16)
                .Patch("gold", IconLabel).WithModel(() => new ViewModel())
                .Patch("gold.label", "label").WithModel(() => new LabelModel($"{gold:n0}"))
                .Patch("gem", IconLabel).WithModel(() => new ViewModel())
                .Patch("gem.label", "label").WithModel(() => new LabelModel($"{gem:n0}"))
                .Patch("stamina", IconLabel).WithModel(() => new ViewModel())
                .Patch("stamina.label", "label").WithModel(() => new LabelModel($"{stamina}/{maxStamina}"))
                .Open(HUD);
        }

        // 92. HUD 미니맵 위 퀘스트 추적기
        static void QuestTracker(string questName, string progress)
        {
            ComponentBlueprint.Create("hud_tracker").WithModel(() => new ViewModel())
                .Layout(Direction.Vertical, spacing: 4)
                .Patch("quest", TitleDesc).WithModel(() => new ViewModel())
                .Patch("quest.title", "label").WithModel(() => new LabelModel(questName))
                .Patch("quest.desc", "label").WithModel(() => new LabelModel(progress))
                .Open(HUD);
        }

        // 93. 전투 중 HP/MP 바 (HUD)
        static void BattleHud(float hp, float mp)
        {
            ComponentBlueprint.Create("battle_hud").WithModel(() => new ViewModel())
                .Layout(Direction.Vertical, spacing: 4)
                .Patch("hp", LabelGauge).WithModel(() => new ViewModel())
                .Patch("hp.label", "label").WithModel(() => new LabelModel("HP"))
                .Patch("hp.gauge", "gauge").WithModel(() => new GaugeModel(hp))
                .Patch("mp", LabelGauge).WithModel(() => new ViewModel())
                .Patch("mp.label", "label").WithModel(() => new LabelModel("MP"))
                .Patch("mp.gauge", "gauge").WithModel(() => new GaugeModel(mp))
                .Open(HUD);
        }

        // 94. 데미지 표시 토스트
        static void DamageToast(int damage, bool isCritical)
        {
            var text = isCritical ? $"CRITICAL! -{damage:n0}" : $"-{damage:n0}";
            ComponentBlueprint.Create(ToastMsg)
                .Patch("message", "label").WithModel(() => new LabelModel(text))
                // [NEED] .Duration(1f)
                .Open(Toast);
        }

        // 95. 아이템 획득 토스트
        static void ItemAcquiredToast(string itemName, int qty)
        {
            ComponentBlueprint.Create(ToastMsg)
                .Patch("message", "label").WithModel(() => new LabelModel($"{itemName} x{qty} 획득!"))
                .Open(Toast);
        }

        // 96. 로딩 화면 (프로그레스)
        static void LoadingScreen(string tip)
        {
            ComponentBlueprint.Create("loading").WithModel(() => new ViewModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Align(TextAnchor.LowerCenter)
                .Patch("progress", LabelGauge).WithModel(() => new ViewModel())
                .Patch("progress.label", "label").WithModel(() => new LabelModel("로딩 중..."))
                .Patch("progress.gauge", "gauge").WithModel(() => new GaugeModel(0f))
                .Patch("tip", "label").WithModel(() => new LabelModel(tip))
                .Open(Popup);
        }

        // 97. 튜토리얼 안내 말풍선
        static void TutorialBubble(string message)
        {
            ComponentBlueprint.Create(SimplePopup)
                .Patch("title", "label").WithModel(() => new LabelModel("튜토리얼"))
                .Patch("body", "label").WithModel(() => new LabelModel(message))
                .Patch("confirm", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }

        // 98. 닉네임 변경
        // [NEED] Input
        static void ChangeNickname()
        {
            ComponentBlueprint.Create(ConfirmPopup)
                .Patch("title", "label").WithModel(() => new LabelModel("닉네임 변경"))
                .Patch("body", "label").WithModel(() => new LabelModel("새 닉네임을 입력해주세요."))
                // [NEED] .Patch("input", "input").WithModel(...)
                .Open(Popup);
        }

        // 99. 서버 선택 화면
        // [NEED] Scrollable
        static void ServerSelect()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("서버 선택"))
                .Patch("servers", "list").WithModel(() => new ListModel())
                    // [NEED] .Scrollable()
                .Open(Popup);
        }

        // 100. 디버그/치트 메뉴 (개발용)
        // [NEED] Grid, Scrollable
        static void DebugMenu()
        {
            ComponentBlueprint.Create("popup").WithModel(() => new PopupModel())
                .Patch("title", "label").WithModel(() => new LabelModel("[DEV] 디버그 메뉴"))
                .Patch("commands", "list").WithModel(() => new ListModel())
                    // [NEED] .Grid(2).Scrollable()
                .Patch("close", "button").WithModel(() => new ButtonModel())
                .Open(Popup);
        }
    }
}
