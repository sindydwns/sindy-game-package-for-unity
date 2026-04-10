using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Components.Composite;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.Test
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // ComponentBuilder 100 Use Cases
    //
    // 이 파일은 ComponentBuilder의 실제 사용 예시 100가지를 모은 레퍼런스입니다.
    // 각 메서드는 독립적인 UI 구성 예시이며, 컴파일 대상이 아닌 설계 참고용입니다.
    //
    // ── 발견된 필요 기능 요약 ────────────────────────────────────────
    //
    // [NEED] Scrollable       — Patch 대상에 ScrollRect를 자동 부여
    // [NEED] Grid(columns)    — GridLayoutGroup 지원 (Layout의 확장)
    // [NEED] Background(type) — 팝업 뒤 딤/블러 배경 제어
    // [NEED] OnClose(Action)  — 팝업 닫힘 콜백을 Builder 체인에서 등록
    // [NEED] Duration(sec)    — 일정 시간 후 자동 닫힘
    // [NEED] Stretch / Fill   — LayoutElement flexible 비율 지정
    // [NEED] Input(placeholder) — 텍스트 입력 필드 Patch
    // [NEED] Separator        — 구분선 요소 삽입
    // [NEED] Conditional(bool) — 조건부 Patch (false면 스킵)
    // [NEED] Badge(path)      — RedDot 바인딩 단축 메서드
    // [NEED] Animate(type)    — 열기/닫기 애니메이션 지정
    // [NEED] Draggable        — 드래그 이동 가능하게 설정
    // [NEED] FitContent       — ContentSizeFitter 자동 부여
    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static class ComponentBuilderUseCases
    {
        static readonly int Popup = 1;
        static readonly int Toast = 2;
        static readonly int HUD = 0;

        // ── 1~10: 알림/확인 ──────────────────────────────────────────

        // 1. 단순 공지 팝업
        static void SimpleNotice()
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("공지사항"))
                .Patch("body", "label").WithModel(new LabelModel("서버 점검이 예정되어 있습니다."))
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 2. 확인/취소 선택 팝업
        static void ConfirmCancel()
        {
            var model = new NoticeModel("아이템 구매", "정말 구매하시겠습니까?", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("구매"));
            model.Cancel.Subscribe(_ => Debug.Log("취소"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 3. 삭제 확인 팝업 (빨간 강조 버튼)
        // [NEED] Color 또는 Style variant를 Patch에 지정하는 방법
        static void DeleteConfirm()
        {
            var model = new NoticeModel("캐릭터 삭제", "이 작업은 되돌릴 수 없습니다.", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("삭제 실행"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                // .PatchStyle("confirm", "button_danger") // [NEED] 프리팹 variant 지정
                .OnLayer(Popup).Open();
        }

        // 4. 자동 닫힘 토스트
        // [NEED] Duration(seconds) — 일정 시간 후 자동 닫힘
        static void AutoCloseToast()
        {
            ComponentBuilder.Build("toast").WithModel(new PopupModel())
                .Patch("message", "label").WithModel(new LabelModel("저장되었습니다."))
                // .Duration(2f)  // [NEED]
                .OnLayer(Toast).Open();
        }

        // 5. 네트워크 오류 재시도 팝업
        // [NEED] OnClose(Action) — 닫힘 시 콜백
        static void NetworkRetry()
        {
            var confirm = new ButtonModel();
            confirm.Subscribe(_ => Debug.Log("재시도"));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("연결 실패"))
                .Patch("body", "label").WithModel(new LabelModel("네트워크 연결을 확인해주세요."))
                .Patch("confirm", "button").WithModel(confirm)
                // .OnClose(() => Debug.Log("닫힘"))  // [NEED]
                .OnLayer(Popup).Open();
        }

        // 6. 서버 점검 안내 (남은 시간 타이머)
        static void MaintenanceTimer()
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("서버 점검 중"))
                .Patch("timer", "label").WithModel(new TimerModel(3600f, @"hh\:mm\:ss"))
                .Patch("body", "label").WithModel(new LabelModel("잠시 후 다시 접속해주세요."))
                .OnLayer(Popup).Open();
        }

        // 7. 앱 업데이트 강제 안내
        static void ForceUpdate()
        {
            var goStore = new ButtonModel();
            goStore.Subscribe(_ => Application.OpenURL("market://details?id=com.example.game"));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("업데이트 필요"))
                .Patch("body", "label").WithModel(new LabelModel("최신 버전으로 업데이트 해주세요."))
                .Patch("confirm", "button").WithModel(goStore)
                .OnLayer(Popup).Open();
        }

        // 8. 이용약관 동의 팝업
        // [NEED] Scrollable — 긴 텍스트 스크롤
        static void TermsOfService()
        {
            var agree = new ToggleModel(false);
            var confirm = new ButtonModel().With(new InteractableFeature(false));
            agree.Subscribe(v => confirm.Feature<InteractableFeature>().Interactable.Value = v);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("이용약관"))
                .Patch("body", "label").WithModel(new LabelModel("약관 내용이 여기에 들어갑니다..."))
                    // .Scrollable()  // [NEED] 스크롤 영역 자동 생성
                .Patch("agree", "toggle").WithModel(agree)
                .Patch("confirm", "button").WithModel(confirm)
                .OnLayer(Popup).Open();
        }

        // 9. 보상 획득 연출 팝업 (아이콘 + 수량 리스트)
        static void RewardPopup()
        {
            var rewards = new ListModel();
            // rewards.SetItems(...) — 보상 아이템 리스트

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("보상 획득!"))
                .Patch("items", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 10. 레벨업 축하 팝업 (이전/이후 스탯 비교)
        static void LevelUp(int oldLv, int newLv, int oldAtk, int newAtk)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Padding(20)
                .Patch("title", "label").WithModel(new LabelModel($"Lv.{newLv} 달성!"))
                .Patch("stats", "container")
                    .Layout(Direction.Vertical, spacing: 4)
                .Patch("stats.level", "label").WithModel(new LabelModel($"레벨: {oldLv} -> {newLv}"))
                .Patch("stats.atk", "label").WithModel(new LabelModel($"공격력: {oldAtk} -> {newAtk}"))
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 11~20: 상점 ─────────────────────────────────────────────

        // 11. 카테고리 탭이 있는 상점 메인
        static void ShopMain()
        {
            var tab = new TabModel(0);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("tabs", "tab").WithModel(tab)
                .Patch("items", "list").WithModel(new ListModel())
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 12. 상품 상세 팝업
        static void ShopItemDetail(Sprite icon, string name, string desc, int price)
        {
            var buy = new ButtonModel();
            buy.Subscribe(_ => Debug.Log($"구매: {name}"));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Padding(16)
                .Patch("icon", "icon").WithModel(new IconModel(icon))
                    .Size(width: 128, height: 128)
                    .Margin(bottom: 8)
                .Patch("name", "label").WithModel(new LabelModel(name))
                .Patch("desc", "label").WithModel(new LabelModel(desc))
                .Patch("price", "label").WithModel(new LabelModel($"{price:n0} 골드"))
                .Patch("buy", "button").WithModel(buy)
                .OnLayer(Popup).Open();
        }

        // 13. 수량 선택 구매 팝업
        // [NEED] Slider 또는 수량 조절 컴포넌트 지원
        static void QuantityPurchase(string itemName, int unitPrice)
        {
            var qty = new PropModel<int>(1);
            var total = new FormatNumberPropModel<int>(unitPrice, v => $"총액: {v:n0}");
            qty.Subscribe(q => total.Source.Value = q * unitPrice);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel($"{itemName} 구매"))
                // .Patch("qty", "slider").WithModel(qty)  // [NEED] SliderModel
                .Patch("total", "label").WithModel(total)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 14. 패키지 번들 상품
        static void BundlePackage()
        {
            var items = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("title", "label").WithModel(new LabelModel("스타터 패키지"))
                .Patch("items", "list").WithModel(items)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("price", "label").WithModel(new LabelModel("$4.99"))
                .Patch("buy", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 15. 기간 한정 세일 배너
        static void TimeLimitedSale()
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("50% 할인!"))
                .Patch("timer", "label").WithModel(new TimerModel(86400f, @"dd\:hh\:mm\:ss"))
                .Patch("go", "button").WithModel(new ButtonModel())
                // .Duration(0)  // 타이머 종료 시 자동 닫힘과 별개 — 배너는 수동 닫힘
                .OnLayer(Popup).Open();
        }

        // 16. 구매 완료 영수증 팝업
        static void PurchaseReceipt(string itemName, int amount, int totalCost)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16)
                .Patch("title", "label").WithModel(new LabelModel("구매 완료"))
                .Patch("item", "label").WithModel(new LabelModel($"{itemName} x{amount}"))
                .Patch("cost", "label").WithModel(new LabelModel($"사용: {totalCost:n0} 골드"))
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 17. 무료 일일 상품 수령
        static void DailyFreeItem(Sprite icon)
        {
            var claim = new ButtonModel();
            claim.Subscribe(_ => Debug.Log("수령 완료"));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("오늘의 무료 선물"))
                .Patch("icon", "icon").WithModel(new IconModel(icon))
                    .Size(width: 96, height: 96)
                .Patch("claim", "button").WithModel(claim)
                .OnLayer(Popup).Open();
        }

        // 18. VIP 전용 상점 탭
        static void VipShop(int vipLevel)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("header", "label").WithModel(new LabelModel($"VIP {vipLevel} 전용 상점"))
                .Patch("items", "list").WithModel(new ListModel())
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 19. 실제 결제 확인 팝업
        static void RealMoneyPurchase(string productName, string price)
        {
            var model = new NoticeModel($"{productName} 구매", $"{price}이(가) 결제됩니다.\n미성년자는 법정대리인 동의가 필요합니다.", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("결제 진행"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 20. 구매 이력 목록 팝업
        // [NEED] Scrollable
        static void PurchaseHistory()
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("구매 이력"))
                .Patch("list", "list").WithModel(new ListModel())
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 21~30: 인벤토리/장비 ────────────────────────────────────

        // 21. 인벤토리 그리드 (필터 탭 + 정렬)
        // [NEED] Grid(columns) — GridLayoutGroup
        static void InventoryGrid()
        {
            var filter = new TabModel(0);
            var items = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("filter", "tab").WithModel(filter)
                .Patch("items", "list").WithModel(items)
                    // .Grid(4)  // [NEED] 4열 그리드
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 22. 아이템 상세 팝업
        static void ItemDetail(Sprite icon, string name, string desc)
        {
            var use = new ButtonModel();
            use.Subscribe(_ => Debug.Log($"사용: {name}"));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Padding(16)
                .Patch("icon", "icon").WithModel(new IconModel(icon))
                    .Size(width: 96, height: 96)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("name", "label").WithModel(new LabelModel(name))
                .Patch("desc", "label").WithModel(new LabelModel(desc))
                .Patch("use", "button").WithModel(use)
                .OnLayer(Popup).Open();
        }

        // 23. 장비 비교 팝업 (현재 vs 신규)
        static void EquipCompare(string curName, int curAtk, string newName, int newAtk)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("장비 비교"))
                .Patch("compare", "container")
                    .Layout(Direction.Horizontal, spacing: 20)
                    .Align(TextAnchor.UpperCenter)
                .Patch("compare.current", "container")
                    .Layout(Direction.Vertical, spacing: 4)
                .Patch("compare.current.name", "label").WithModel(new LabelModel(curName))
                .Patch("compare.current.atk", "label").WithModel(new LabelModel($"ATK {curAtk}"))
                .Patch("compare.new", "container")
                    .Layout(Direction.Vertical, spacing: 4)
                .Patch("compare.new.name", "label").WithModel(new LabelModel(newName))
                .Patch("compare.new.atk", "label").WithModel(new LabelModel($"ATK {newAtk}"))
                .Patch("equip", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 24. 아이템 분해/판매 수량 선택
        static void SellQuantity(string itemName, int maxQty)
        {
            var qty = new PropModel<int>(1);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel($"{itemName} 판매"))
                // .Patch("qty", "slider").WithModel(qty)  // [NEED] SliderModel
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .Patch("cancel", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 25. 일괄 판매 선택 화면
        // [NEED] Grid(columns), Scrollable
        static void BulkSellSelect()
        {
            var items = new ListModel(); // 체크박스 포함 아이템 슬롯

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("판매할 아이템 선택"))
                .Patch("items", "list").WithModel(items)
                    // .Grid(4)       // [NEED]
                    // .Scrollable()  // [NEED]
                .Patch("sell_all", "button").WithModel(new ButtonModel())
                .Patch("cancel", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 26. 장비 강화 화면 (재료 슬롯 + 성공 확률)
        static void EnhanceEquipment(string equipName, float successRate)
        {
            var materials = new ListModel();
            var enhance = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("title", "label").WithModel(new LabelModel($"{equipName} 강화"))
                .Patch("materials", "list").WithModel(materials)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("rate", "label").WithModel(new LabelModel($"성공 확률: {successRate:P0}"))
                .Patch("enhance", "button").WithModel(enhance)
                .OnLayer(Popup).Open();
        }

        // 27. 강화 결과 팝업
        // [NEED] Animate(type) — 성공/실패 연출
        static void EnhanceResult(bool success, string equipName)
        {
            var msg = success ? $"{equipName} 강화 성공!" : "강화 실패...";

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(success ? AnimType.Celebrate : AnimType.Shake)  // [NEED]
                .Patch("result", "label").WithModel(new LabelModel(msg))
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 28. 장비 각인/룬 소켓 편집
        static void RuneSocket()
        {
            var slots = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("룬 소켓"))
                .Patch("slots", "list").WithModel(slots)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 29. 세트 효과 미리보기
        static void SetEffectPreview(string setName, string bonus2, string bonus4)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16)
                .Patch("title", "label").WithModel(new LabelModel($"{setName} 세트"))
                .Patch("bonus2", "label").WithModel(new LabelModel($"2세트: {bonus2}"))
                .Patch("bonus4", "label").WithModel(new LabelModel($"4세트: {bonus4}"))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 30. 잠금/즐겨찾기 토글 확인
        static void LockToggleConfirm(string itemName, bool willLock)
        {
            var msg = willLock ? $"{itemName}을(를) 잠금하시겠습니까?" : $"{itemName}의 잠금을 해제하시겠습니까?";
            var model = new NoticeModel("아이템 잠금", msg, hasCancel: true);

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // ── 31~40: 캐릭터/영웅 ──────────────────────────────────────

        // 31. 캐릭터 프로필 팝업
        static void CharacterProfile(Sprite portrait, string name, float hpRatio, float mpRatio)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Padding(16)
                .Patch("portrait", "icon").WithModel(new IconModel(portrait))
                    .Size(width: 200, height: 200)
                .Patch("name", "label").WithModel(new LabelModel(name))
                .Patch("bars", "container")
                    .Layout(Direction.Vertical, spacing: 6)
                .Patch("bars.hp", "gauge").WithModel(new GaugeModel(hpRatio))
                .Patch("bars.mp", "gauge").WithModel(new GaugeModel(mpRatio))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 32. 캐릭터 목록 (등급/클래스 필터 + 검색)
        // [NEED] Input(placeholder) — 텍스트 입력 필드
        // [NEED] Grid(columns)
        static void CharacterList()
        {
            var classFilter = new TabModel(0);
            var gradeFilter = new TabModel(0);
            var characters = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("filters", "container")
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("filters.class", "tab").WithModel(classFilter)
                .Patch("filters.grade", "tab").WithModel(gradeFilter)
                // .Patch("search", "input").WithModel(new InputModel("이름 검색..."))  // [NEED]
                .Patch("list", "list").WithModel(characters)
                    // .Grid(4)  // [NEED]
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 33. 캐릭터 스킬 상세 팝업
        static void SkillDetail(Sprite skillIcon, string skillName, int level, string desc)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("header", "container")
                    .Layout(Direction.Horizontal, spacing: 12)
                    .Align(TextAnchor.MiddleLeft)
                .Patch("header.icon", "icon").WithModel(new IconModel(skillIcon))
                    .Size(width: 64, height: 64)
                .Patch("header.info", "container")
                    .Layout(Direction.Vertical, spacing: 2)
                .Patch("header.info.name", "label").WithModel(new LabelModel(skillName))
                .Patch("header.info.level", "label").WithModel(new LabelModel($"Lv.{level}"))
                .Patch("desc", "label").WithModel(new LabelModel(desc))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 34. 캐릭터 승급 화면 (재료 + 전후 비교)
        static void PromoteCharacter(string name, int curStar, int nextStar)
        {
            var materials = new ListModel();
            var promote = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("title", "label").WithModel(new LabelModel($"{name} 승급"))
                .Patch("stars", "label").WithModel(new LabelModel($"{'★'.ToString().PadRight(curStar, '★')} -> {'★'.ToString().PadRight(nextStar, '★')}"))
                .Patch("materials", "list").WithModel(materials)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("promote", "button").WithModel(promote)
                .OnLayer(Popup).Open();
        }

        // 35. 캐릭터 호감도/친밀도 화면
        static void Affinity(string charName, float progress)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("name", "label").WithModel(new LabelModel(charName))
                .Patch("progress", "gauge").WithModel(new GaugeModel(progress))
                .Patch("gift", "button").WithModel(new ButtonModel())
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 36. 캐릭터 스킨 선택 팝업
        // [NEED] Scrollable (가로 스크롤)
        static void SkinSelect()
        {
            var skins = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("스킨 선택"))
                .Patch("skins", "list").WithModel(skins)
                    .Layout(Direction.Horizontal, spacing: 12)
                    // .Scrollable()  // [NEED] 가로 스크롤
                .Patch("equip", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 37. 파티 편성 화면 (슬롯 배치)
        // [NEED] Draggable — 드래그로 슬롯 교체
        static void PartyFormation()
        {
            var slots = new ListModel();
            var reserve = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("파티 편성"))
                .Patch("party", "list").WithModel(slots)
                    .Layout(Direction.Horizontal, spacing: 8)
                    // .Draggable()  // [NEED]
                .Patch("reserve", "list").WithModel(reserve)
                    // .Grid(4)       // [NEED]
                    // .Scrollable()  // [NEED]
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 38. 캐릭터 잠금 해제 연출
        // [NEED] Animate(type)
        static void CharacterUnlock(Sprite portrait, string name)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(AnimType.Reveal)  // [NEED]
                .Patch("portrait", "icon").WithModel(new IconModel(portrait))
                    .Size(width: 256, height: 256)
                .Patch("name", "label").WithModel(new LabelModel(name))
                    .Margin(top: 12)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 39. 능력치 포인트 분배
        static void StatAllocation(int remainPoints)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("title", "label").WithModel(new LabelModel($"남은 포인트: {remainPoints}"))
                .Patch("stats", "container")
                    .Layout(Direction.Vertical, spacing: 6)
                // 각 스탯 행: [이름] [값] [+] [-]
                .Patch("stats.str", "container").Layout(Direction.Horizontal, spacing: 8)
                .Patch("stats.dex", "container").Layout(Direction.Horizontal, spacing: 8)
                .Patch("stats.int", "container").Layout(Direction.Horizontal, spacing: 8)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 40. 캐릭터 도감 상세
        // [NEED] Conditional(bool) — 미보유 시 일부 Patch 숨김
        static void CollectionDetail(Sprite icon, string name, bool owned)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("icon", "icon").WithModel(new IconModel(icon))
                    .Size(width: 128, height: 128)
                .Patch("name", "label").WithModel(new LabelModel(owned ? name : "???"))
                // .Conditional(owned)
                //     .Patch("stats", "container")  // [NEED] 미보유면 스킵
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 41~45: 가챠/소환 ────────────────────────────────────────

        // 41. 소환 메인 화면
        // [NEED] Badge(redDotPath) — 무료 뽑기 알림
        static void GachaMain()
        {
            var pull1 = new ButtonModel();
            var pull10 = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("banner", "icon").WithModel(new IconModel())
                    // .Stretch()  // [NEED] 가로 꽉 참
                .Patch("pity", "label").WithModel(new LabelModel("보장 카운터: 73/90"))
                .Patch("buttons", "container")
                    .Layout(Direction.Horizontal, spacing: 12)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("buttons.pull1", "button").WithModel(pull1)
                    // .Badge("gacha.free")  // [NEED]
                .Patch("buttons.pull10", "button").WithModel(pull10)
                .OnLayer(Popup).Open();
        }

        // 42. 소환 결과 목록
        static void GachaResult()
        {
            var results = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(AnimType.GachaReveal)  // [NEED]
                .Patch("results", "list").WithModel(results)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 43. 소환 확률표
        // [NEED] Scrollable, Separator
        static void GachaRates()
        {
            var rates = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("확률 정보"))
                .Patch("rates", "list").WithModel(rates)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 44. 픽업 배너 상세
        static void PickupBannerDetail(Sprite bannerImg, string charName)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("banner", "icon").WithModel(new IconModel(bannerImg))
                    // .Stretch()  // [NEED]
                .Patch("name", "label").WithModel(new LabelModel($"픽업: {charName}"))
                .Patch("timer", "label").WithModel(new TimerModel(259200f, @"dd\일\ hh\시\간"))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 45. 천장 시스템 진행도
        static void PityProgress(int current, int max)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("보장 시스템"))
                .Patch("progress", "gauge").WithModel(new GaugeModel((float)current / max))
                .Patch("count", "label").WithModel(new LabelModel($"{current} / {max}"))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 46~55: 퀘스트/미션 ──────────────────────────────────────

        // 46. 메인 퀘스트 추적 HUD 패널
        // [NEED] FitContent — 내용에 맞게 크기 자동 조절
        static void QuestTracker(string questName, string objective)
        {
            ComponentBuilder.Build("hud_panel").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 4)
                .Padding(8)
                // .FitContent()  // [NEED]
                .Patch("quest", "label").WithModel(new LabelModel(questName))
                .Patch("obj", "label").WithModel(new LabelModel(objective))
                .OnLayer(HUD).Open();
        }

        // 47. 퀘스트 목록 팝업 (메인/서브/일일 탭)
        static void QuestList()
        {
            var tab = new TabModel(0);
            var quests = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("tabs", "tab").WithModel(tab)
                .Patch("list", "list").WithModel(quests)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 48. 퀘스트 상세 팝업
        static void QuestDetail(string title, string desc)
        {
            var rewards = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("title", "label").WithModel(new LabelModel(title))
                .Patch("desc", "label").WithModel(new LabelModel(desc))
                // .Patch("separator", "separator")  // [NEED] Separator
                .Patch("reward_title", "label").WithModel(new LabelModel("보상"))
                .Patch("rewards", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("go", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 49. 퀘스트 완료 보상 수령
        static void QuestComplete(string title)
        {
            var rewards = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(AnimType.Celebrate)  // [NEED]
                .Patch("title", "label").WithModel(new LabelModel($"{title} 완료!"))
                .Patch("rewards", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("claim", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 50. 일일/주간 미션 체크리스트
        static void DailyMissions()
        {
            var missions = new ListModel();
            var progress = new GaugeModel(0.6f);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("title", "label").WithModel(new LabelModel("일일 미션"))
                .Patch("progress", "gauge").WithModel(progress)
                .Patch("missions", "list").WithModel(missions)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 51. 업적 목록 (카테고리 탭 + 진행도 바)
        static void Achievements()
        {
            var tab = new TabModel(0);
            var list = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("tabs", "tab").WithModel(tab)
                .Patch("list", "list").WithModel(list)
                    .Layout(Direction.Vertical, spacing: 6)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 52. 업적 달성 알림 토스트
        // [NEED] Duration(seconds)
        static void AchievementToast(string title)
        {
            ComponentBuilder.Build("toast").WithModel(new PopupModel())
                .Layout(Direction.Horizontal, spacing: 8)
                .Align(TextAnchor.MiddleLeft)
                .Padding(12)
                .Patch("icon", "icon").WithModel(new IconModel())
                    .Size(width: 32, height: 32)
                .Patch("text", "label").WithModel(new LabelModel($"업적 달성: {title}"))
                // .Duration(3f)  // [NEED]
                .OnLayer(Toast).Open();
        }

        // 53. 시즌 패스 진행도 화면
        static void SeasonPass(int currentTier, int maxTier)
        {
            var tiers = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("title", "label").WithModel(new LabelModel($"시즌 패스 ({currentTier}/{maxTier})"))
                .Patch("progress", "gauge").WithModel(new GaugeModel((float)currentTier / maxTier))
                .Patch("tiers", "list").WithModel(tiers)
                    .Layout(Direction.Horizontal, spacing: 4)
                    // .Scrollable()  // [NEED] 가로 스크롤
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 54. 이벤트 퀘스트 전용 탭
        static void EventQuests()
        {
            var quests = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("banner", "icon").WithModel(new IconModel())
                    // .Stretch()  // [NEED]
                .Patch("timer", "label").WithModel(new TimerModel(604800f, @"dd\일\ hh\:mm"))
                .Patch("quests", "list").WithModel(quests)
                    .Layout(Direction.Vertical, spacing: 6)
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 55. 튜토리얼 퀘스트 가이드 말풍선
        // [NEED] FitContent, Animate
        static void TutorialBubble(string text)
        {
            ComponentBuilder.Build("bubble").WithModel(new PopupModel())
                .Padding(12)
                // .FitContent()  // [NEED]
                // .Animate(AnimType.Bounce)  // [NEED]
                .Patch("text", "label").WithModel(new LabelModel(text))
                .OnLayer(HUD).Open();
        }

        // ── 56~65: 소셜 ─────────────────────────────────────────────

        // 56. 친구 목록
        static void FriendList()
        {
            var friends = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("친구 목록"))
                .Patch("list", "list").WithModel(friends)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 57. 친구 요청 수신 팝업
        static void FriendRequest(string fromName)
        {
            var model = new NoticeModel($"친구 요청", $"{fromName}님이 친구 요청을 보냈습니다.", hasCancel: true);

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 58. 타인 프로필 카드
        static void PlayerProfile(Sprite avatar, string name, int level)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Padding(16)
                .Align(TextAnchor.MiddleCenter)
                .Patch("avatar", "icon").WithModel(new IconModel(avatar))
                    .Size(width: 80, height: 80)
                .Patch("name", "label").WithModel(new LabelModel(name))
                .Patch("level", "label").WithModel(new LabelModel($"Lv.{level}"))
                .Patch("actions", "container")
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("actions.add", "button").WithModel(new ButtonModel())
                .Patch("actions.block", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 59. 채팅 입력창 + 메시지 리스트
        // [NEED] Input(placeholder), Scrollable, Stretch
        static void ChatWindow()
        {
            var messages = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 4)
                .Patch("messages", "list").WithModel(messages)
                    .Layout(Direction.Vertical, spacing: 2)
                    // .Scrollable()  // [NEED]
                    // .Stretch()     // [NEED] 남은 공간 채우기
                .Patch("input_area", "container")
                    .Layout(Direction.Horizontal, spacing: 4)
                // .Patch("input_area.field", "input").WithModel(new InputModel("메시지 입력..."))  // [NEED]
                .Patch("input_area.send", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 60. 채팅 채널 선택 드롭다운
        // [NEED] Dropdown 또는 SelectModel
        static void ChannelSelect()
        {
            var channels = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .FitContent()  // [NEED]
                .Patch("channels", "list").WithModel(channels)
                    .Layout(Direction.Vertical, spacing: 2)
                .OnLayer(Popup).Open();
        }

        // 61. 길드 가입 신청
        static void GuildApply(string guildName)
        {
            var model = new NoticeModel("길드 가입", $"{guildName}에 가입 신청하시겠습니까?", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("가입 신청"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 62. 길드 정보 화면
        static void GuildInfo(string guildName, int level, int memberCount, int maxMembers)
        {
            var members = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("name", "label").WithModel(new LabelModel($"{guildName} (Lv.{level})"))
                .Patch("count", "label").WithModel(new LabelModel($"멤버: {memberCount}/{maxMembers}"))
                .Patch("members", "list").WithModel(members)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 63. 길드 공지사항 편집
        // [NEED] Input(placeholder) — 멀티라인 텍스트 입력
        static void GuildNoticeEdit()
        {
            var save = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("공지사항 수정"))
                // .Patch("content", "input_multiline").WithModel(new InputModel("공지 내용..."))  // [NEED]
                .Patch("save", "button").WithModel(save)
                .Patch("cancel", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 64. 선물 보내기 대상 선택
        static void SendGiftTarget()
        {
            var friends = new ListModel();
            var send = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("선물 보내기"))
                .Patch("friends", "list").WithModel(friends)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("send", "button").WithModel(send)
                .OnLayer(Popup).Open();
        }

        // 65. 차단 목록 관리
        static void BlockList()
        {
            var blocked = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("차단 목록"))
                .Patch("list", "list").WithModel(blocked)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 66~75: 전투/콘텐츠 ──────────────────────────────────────

        // 66. 스테이지 선택 화면
        // [NEED] Grid(columns)
        static void StageSelect()
        {
            var tabs = new TabModel(0); // 난이도
            var stages = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("difficulty", "tab").WithModel(tabs)
                .Patch("stages", "list").WithModel(stages)
                    // .Grid(3)       // [NEED]
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 67. 전투 시작 전 파티 확인
        static void BattleReady(int staminaCost, int currentStamina)
        {
            var party = new ListModel();
            var start = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("party", "list").WithModel(party)
                    .Layout(Direction.Horizontal, spacing: 8)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("stamina", "label").WithModel(new LabelModel($"스태미나: {staminaCost} (보유: {currentStamina})"))
                .Patch("start", "button").WithModel(start)
                .OnLayer(Popup).Open();
        }

        // 68. 전투 결과 요약
        static void BattleResult(int stars, int exp, int gold)
        {
            var rewards = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Padding(16)
                // .Animate(AnimType.SlideUp)  // [NEED]
                .Patch("stars", "label").WithModel(new LabelModel($"{'★'.ToString().PadRight(stars, '★')}"))
                    .Align(TextAnchor.MiddleCenter)
                .Patch("stats", "container")
                    .Layout(Direction.Horizontal, spacing: 16)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("stats.exp", "label").WithModel(new LabelModel($"EXP +{exp}"))
                .Patch("stats.gold", "label").WithModel(new LabelModel($"골드 +{gold:n0}"))
                .Patch("rewards", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 69. 전투 패배 팝업
        static void BattleDefeat()
        {
            var retry = new ButtonModel();
            var exit = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("전투 패배"))
                .Patch("buttons", "container")
                    .Layout(Direction.Horizontal, spacing: 12)
                    .Align(TextAnchor.MiddleCenter)
                .Patch("buttons.retry", "button").WithModel(retry)
                .Patch("buttons.exit", "button").WithModel(exit)
                .OnLayer(Popup).Open();
        }

        // 70. 보스 정보 미리보기
        static void BossPreview(Sprite bossIcon, string name, string weakness)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Align(TextAnchor.MiddleCenter)
                .Patch("icon", "icon").WithModel(new IconModel(bossIcon))
                    .Size(width: 160, height: 160)
                .Patch("name", "label").WithModel(new LabelModel(name))
                .Patch("weakness", "label").WithModel(new LabelModel($"약점: {weakness}"))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 71. 자동 전투 설정
        static void AutoBattleSetting()
        {
            var repeat = new PropModel<int>(10);
            var toggle = new ToggleModel(true);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("title", "label").WithModel(new LabelModel("자동 전투 설정"))
                // .Patch("repeat", "slider").WithModel(repeat)  // [NEED] SliderModel
                .Patch("stop_on_full", "toggle").WithModel(toggle)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 72. PvP 매칭 대기 화면
        // [NEED] Animate (로딩 스피너)
        static void PvpMatchmaking()
        {
            var cancel = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 16)
                .Align(TextAnchor.MiddleCenter)
                .Patch("text", "label").WithModel(new LabelModel("상대를 찾는 중..."))
                // .Patch("spinner", "spinner")  // [NEED] 로딩 스피너 컴포넌트
                .Patch("timer", "label").WithModel(new TimerModel(0f)) // 경과 시간
                .Patch("cancel", "button").WithModel(cancel)
                .OnLayer(Popup).Open();
        }

        // 73. PvP 시즌 랭킹 보상 안내
        static void PvpSeasonReward()
        {
            var tiers = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("title", "label").WithModel(new LabelModel("시즌 보상 안내"))
                .Patch("tiers", "list").WithModel(tiers)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 74. 레이드 로비
        static void RaidLobby(string bossName)
        {
            var participants = new ListModel();
            var start = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("boss", "label").WithModel(new LabelModel($"레이드: {bossName}"))
                .Patch("members", "list").WithModel(participants)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("start", "button").WithModel(start)
                .OnLayer(Popup).Open();
        }

        // 75. 소탕권 사용 확인
        static void SweepConfirm(int ticketCount, int stageStars)
        {
            var model = new NoticeModel("소탕", $"소탕권 {ticketCount}장을 사용하시겠습니까?\n(★{stageStars} 이상 클리어 필요)", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("소탕 실행"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // ── 76~79: 랭킹/리더보드 ────────────────────────────────────

        // 76. 전체 랭킹 리스트
        // [NEED] Scrollable, Separator (내 순위 구분)
        static void Leaderboard()
        {
            var ranks = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("랭킹"))
                .Patch("my_rank", "label").WithModel(new LabelModel("내 순위: 1,234위"))
                    .Margin(bottom: 8)
                // .Patch("divider", "separator")  // [NEED]
                .Patch("list", "list").WithModel(ranks)
                    .Layout(Direction.Vertical, spacing: 2)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 77. 주간 랭킹 보상 안내
        static void WeeklyRankReward(string tier, string rewardDesc)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("title", "label").WithModel(new LabelModel("주간 보상"))
                .Patch("tier", "label").WithModel(new LabelModel($"현재 등급: {tier}"))
                .Patch("reward", "label").WithModel(new LabelModel(rewardDesc))
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 78. 길드 랭킹 탭
        static void GuildRanking()
        {
            var ranks = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("길드 랭킹"))
                .Patch("list", "list").WithModel(ranks)
                    .Layout(Direction.Vertical, spacing: 2)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 79. 랭킹 시즌 종료 결산
        static void SeasonEnd(int finalRank, string tier)
        {
            var rewards = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(AnimType.Celebrate)  // [NEED]
                .Layout(Direction.Vertical, spacing: 12)
                .Align(TextAnchor.MiddleCenter)
                .Patch("title", "label").WithModel(new LabelModel("시즌 종료"))
                .Patch("rank", "label").WithModel(new LabelModel($"최종 순위: {finalRank}위 ({tier})"))
                .Patch("rewards", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 80~84: 우편/보상 ────────────────────────────────────────

        // 80. 우편함 목록
        // [NEED] Badge(path) — 안읽은 메일 표시
        static void Mailbox()
        {
            var mails = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("우편함"))
                    // .Badge("mail.unread")  // [NEED]
                .Patch("list", "list").WithModel(mails)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("claim_all", "button").WithModel(new ButtonModel())
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 81. 우편 상세
        static void MailDetail(string sender, string body)
        {
            var rewards = new ListModel();
            var claim = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("sender", "label").WithModel(new LabelModel($"보낸이: {sender}"))
                .Patch("body", "label").WithModel(new LabelModel(body))
                .Patch("rewards", "list").WithModel(rewards)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("claim", "button").WithModel(claim)
                .OnLayer(Popup).Open();
        }

        // 82. 전체 수령 확인
        static void ClaimAllConfirm(int mailCount)
        {
            var model = new NoticeModel("전체 수령", $"{mailCount}개의 우편을 모두 수령하시겠습니까?", hasCancel: true);

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 83. 출석 체크 캘린더
        // [NEED] Grid(7) — 7열 캘린더
        static void AttendanceCalendar(int currentDay)
        {
            var days = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel($"출석 체크 ({currentDay}일차)"))
                .Patch("calendar", "list").WithModel(days)
                    // .Grid(7)  // [NEED] 7열 캘린더
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 84. 보상 코드 입력
        // [NEED] Input(placeholder)
        static void RedeemCode()
        {
            var redeem = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("코드 입력"))
                // .Patch("code", "input").WithModel(new InputModel("코드를 입력하세요"))  // [NEED]
                .Patch("redeem", "button").WithModel(redeem)
                .OnLayer(Popup).Open();
        }

        // ── 85~92: 설정/시스템 ──────────────────────────────────────

        // 85. 설정 화면 (사운드/그래픽/알림 토글)
        static void Settings()
        {
            var bgm = new ToggleModel(true);
            var sfx = new ToggleModel(true);
            var push = new ToggleModel(true);

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(16)
                .Patch("title", "label").WithModel(new LabelModel("설정"))
                .Patch("bgm", "toggle").WithModel(bgm)
                .Patch("sfx", "toggle").WithModel(sfx)
                .Patch("push", "toggle").WithModel(push)
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 86. 계정 연동 팝업
        static void AccountLink()
        {
            var google = new ButtonModel();
            var apple = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 12)
                .Patch("title", "label").WithModel(new LabelModel("계정 연동"))
                .Patch("google", "button").WithModel(google)
                .Patch("apple", "button").WithModel(apple)
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 87. 언어 선택 팝업
        static void LanguageSelect()
        {
            var languages = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("언어 선택"))
                .Patch("list", "list").WithModel(languages)
                    .Layout(Direction.Vertical, spacing: 4)
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 88. 고객센터 문의 작성 폼
        // [NEED] Input(placeholder) — 멀티라인
        static void CustomerSupport()
        {
            var submit = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("title", "label").WithModel(new LabelModel("문의하기"))
                // .Patch("subject", "input").WithModel(new InputModel("제목"))        // [NEED]
                // .Patch("body", "input_multiline").WithModel(new InputModel("내용"))  // [NEED]
                .Patch("submit", "button").WithModel(submit)
                .OnLayer(Popup).Open();
        }

        // 89. 로그아웃 확인
        static void LogoutConfirm()
        {
            var model = new NoticeModel("로그아웃", "정말 로그아웃 하시겠습니까?", hasCancel: true);

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 90. 계정 삭제 경고 팝업 (이중 확인)
        // [NEED] Input(placeholder) — "삭제" 입력 확인
        static void DeleteAccount()
        {
            var model = new NoticeModel("계정 삭제", "이 작업은 되돌릴 수 없습니다.\n계속하려면 '삭제'를 입력하세요.", hasCancel: true);
            // [NEED] 텍스트 입력 일치 시에만 확인 버튼 활성화

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 91. 푸시 알림 권한 요청
        static void PushPermission()
        {
            var model = new NoticeModel("알림 허용", "보상 알림 및 이벤트 소식을 받으시겠습니까?", hasCancel: true);
            model.Confirm.Subscribe(_ => Debug.Log("푸시 허용"));

            ComponentBuilder.Build("notice_popup").WithModel(model)
                .OnLayer(Popup).Open();
        }

        // 92. 데이터 다운로드 진행 바
        // [NEED] Background(type) — 배경 터치 차단
        static void DownloadProgress()
        {
            var progress = new GaugeModel(0f);
            var label = new LabelModel("다운로드 중... 0%");

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Align(TextAnchor.MiddleCenter)
                // .Background(BackgroundType.BlockAll)  // [NEED] 터치 완전 차단
                .Patch("label", "label").WithModel(label)
                .Patch("progress", "gauge").WithModel(progress)
                .OnLayer(Popup).Open();
        }

        // ── 93~95: 제작/생활 ────────────────────────────────────────

        // 93. 제작 레시피 목록
        static void CraftingRecipes()
        {
            var tab = new TabModel(0);
            var recipes = new ListModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("tabs", "tab").WithModel(tab)
                .Patch("list", "list").WithModel(recipes)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("close", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 94. 제작 상세 화면
        static void CraftDetail(string itemName, int craftTime)
        {
            var materials = new ListModel();
            var craft = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 10)
                .Patch("title", "label").WithModel(new LabelModel($"{itemName} 제작"))
                .Patch("materials", "list").WithModel(materials)
                    .Layout(Direction.Horizontal, spacing: 8)
                .Patch("time", "label").WithModel(new LabelModel($"제작 시간: {craftTime}초"))
                .Patch("craft", "button").WithModel(craft)
                .OnLayer(Popup).Open();
        }

        // 95. 제작 완료 결과
        static void CraftComplete(Sprite icon, string itemName)
        {
            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                // .Animate(AnimType.Celebrate)  // [NEED]
                .Patch("icon", "icon").WithModel(new IconModel(icon))
                    .Size(width: 96, height: 96)
                .Patch("name", "label").WithModel(new LabelModel($"{itemName} 제작 완료!"))
                .Patch("confirm", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // ── 96~97: 맵/탐험 ──────────────────────────────────────────

        // 96. 월드맵 지역 선택 팝업
        // [NEED] Conditional(bool) — 잠금 시 자물쇠 표시
        static void WorldMapRegion(string regionName, bool unlocked, int recommendLv)
        {
            var enter = new ButtonModel().With(new InteractableFeature(unlocked));

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Patch("name", "label").WithModel(new LabelModel(regionName))
                .Patch("level", "label").WithModel(new LabelModel($"권장 Lv.{recommendLv}"))
                // .Conditional(!unlocked)
                //     .Patch("lock_icon", "icon")  // [NEED]
                .Patch("enter", "button").WithModel(enter)
                .OnLayer(Popup).Open();
        }

        // 97. NPC 대화 말풍선 (선택지 버튼)
        // [NEED] FitContent
        static void NpcDialogue(string npcName, string text, string[] choices)
        {
            var builder = ComponentBuilder.Build("dialogue").WithModel(new PopupModel())
                // .FitContent()  // [NEED]
                .Layout(Direction.Vertical, spacing: 8)
                .Padding(12)
                .Patch("name", "label").WithModel(new LabelModel(npcName))
                .Patch("text", "label").WithModel(new LabelModel(text));

            for (int i = 0; i < choices.Length; i++)
            {
                var btn = new ButtonModel();
                var idx = i;
                btn.Subscribe(_ => Debug.Log($"선택: {idx}"));
                builder.Patch($"choices.c{i}", "button").WithModel(btn);
            }

            builder.OnLayer(HUD).Open();
        }

        // ── 98~100: 기타 ────────────────────────────────────────────

        // 98. 로딩 화면 (진행 바 + 팁)
        // [NEED] Background(type), Stretch
        static void LoadingScreen(string tip)
        {
            var progress = new GaugeModel(0f);

            ComponentBuilder.Build("fullscreen").WithModel(new PopupModel())
                .Layout(Direction.Vertical, spacing: 8)
                .Align(TextAnchor.LowerCenter)
                .Padding(bottom: 40)
                // .Background(BackgroundType.Opaque)  // [NEED] 전체 화면 불투명
                .Patch("tip", "label").WithModel(new LabelModel(tip))
                .Patch("progress", "gauge").WithModel(progress)
                .OnLayer(Popup).Open();
        }

        // 99. 닉네임 변경 입력 팝업
        // [NEED] Input(placeholder)
        static void NicknameChange()
        {
            var confirm = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("닉네임 변경"))
                // .Patch("name", "input").WithModel(new InputModel("새 닉네임"))  // [NEED]
                .Patch("confirm", "button").WithModel(confirm)
                .Patch("cancel", "button").WithModel(new ButtonModel())
                .OnLayer(Popup).Open();
        }

        // 100. 최초 서버 선택 팝업
        // [NEED] Scrollable
        static void ServerSelect()
        {
            var servers = new ListModel();
            var confirm = new ButtonModel();

            ComponentBuilder.Build("popup").WithModel(new PopupModel())
                .Patch("title", "label").WithModel(new LabelModel("서버 선택"))
                .Patch("recommended", "label").WithModel(new LabelModel("추천: Asia-1"))
                    .Margin(bottom: 8)
                .Patch("servers", "list").WithModel(servers)
                    .Layout(Direction.Vertical, spacing: 4)
                    // .Scrollable()  // [NEED]
                .Patch("confirm", "button").WithModel(confirm)
                .OnLayer(Popup).Open();
        }
    }
}
