using System.Collections.Generic;
using R3;
using Sindy.Reactive;
using Sindy.View;
using Sindy.View.Features;
using Sindy.View.Scroller;
using UnityEngine;

namespace Sindy.Samples.ComponentShowcase
{
    /// <summary>
    /// SindyComponent 쇼케이스 — 미니 상점 데모.
    ///
    /// 이 데모가 보여주는 것은 개별 Feature가 아니라 **사용 패턴**이다:
    ///
    /// 1. 프리팹 조합 (ComponentBlueprint) — 씬에는 ComponentManager 셸만 있고,
    ///    상점 화면은 카탈로그의 부품 프리팹(CaptionRow, InfoRow, QtyRow...)을
    ///    설계도로 조합해 Open() 시점에 만들어낸다.
    /// 2. 디자인/기능 분리 — 배치·간격·여백은 Blueprint 체인(BuildDetailPanel)에,
    ///    모델 상태·구독은 기능 코드(BuildModel)에. 두 관심사가 파일 안에서도 나뉜다.
    /// 3. 하이브리드 — 틀(헤더·스크롤러·로그 바)은 ShopFrame 프리팹에 두고
    ///    모델만 주입(title/gold/list/log), 가변 부품(상세 패널)은 코드로 조합.
    ///
    /// 원칙: Controller는 ViewModel(Feature)만 건드린다. Unity UI 직접 호출 없음.
    /// 모든 화면 갱신은 FeatureView의 Bind 구독이 처리한다.
    /// </summary>
    public class ShopDemoController : MonoBehaviour
    {
        [Header("스크롤러 설정")]
        [SerializeField] private SectionOption itemSectionOption;
        [SerializeField] private SectionOption bannerSectionOption;
        [Tooltip("일회성 셀 — 키 등록 없이 Section.ContentPrefab으로 직접 지정")]
        [SerializeField] private SindyComponent bannerPrefab;

        [Header("데모 데이터")]
        [SerializeField] private ShopItemData[] items;
        [Tooltip("아이템별 레벨 변형 수. 가상화(보이는 셀만 인스턴스화) 확인용으로 목록을 늘린다.")]
        [SerializeField] private int levelsPerItem = 6;
        [SerializeField] private long startGold = 12345;

        // ---- 모델 (Controller가 건드리는 유일한 대상) ----
        private ViewModel shop;
        private FormatNumberPropModel<long> gold;
        private FormatNumberPropModel<int> qty;
        private PropModel<string> itemName;
        private PropModel<string> itemDesc;
        private PropModel<Sprite> itemIcon;
        private PropModel<Sprite> itemFrame;
        private PropModel<string> buyLabel;
        private PropModel<bool> canBuy;
        private PropModel<string> logText;
        private ButtonFeature minusBtn;
        private ButtonFeature plusBtn;
        private ButtonFeature buyBtn;
        private ToggleFeature bgm;
        private ToggleFeature skipConfirm;

        private SindyComponent shopInstance;
        private ScreenFeature screen;
        private ShopItemData selected;
        private bool pendingConfirm;
        private readonly Queue<string> logLines = new();
        private const int MaxLogLines = 5;
        private const int MaxQty = 99;

        private void Start()
        {
            // 설계도 → Open(): 루트 프리팹 인스턴스화 + 부품 프리팹 조립 + 모델 바인딩
            shopInstance = BuildBlueprint().Open();

            // 화면 변형 전환 시 스크롤러 가상화 재계산.
            // Bind 이후에 구독해야 ResponsiveLayoutFeatureView의 레이아웃 적용(먼저 구독됨)이
            // 끝난 뒤 Reload가 실행된다 — 새 뷰포트 크기 기준으로 재계산됨.
            ((ViewComponent)shopInstance).TryGetView("list", out var scrollerHub);
            screen.Variant.Prop.Skip(1).Subscribe(v =>
            {
                Log($"화면 변형 전환: {v}");
                if (scrollerHub != null) scrollerHub.Reload();
            });

            if (items != null && items.Length > 0)
                Select(items[0]);
            Log("상점 데모 시작 — 셀을 눌러 아이템을 선택하세요");
        }

        private void OnDestroy()
        {
            // 순서 중요: 1) FeatureView 구독 해제 → 2) 모델 내부 구독 해제
            if (shopInstance != null) shopInstance.Bind(null);
            shop?.Dispose();
        }

        // ---------------- 설계도 (디자인 — 구조·배치·간격) ----------------

        /// <summary>
        /// 상점 화면 설계도. 틀(ShopFrame)에 상세 패널 Blueprint를 중첩 패치한다.
        /// 'detail' 키는 프레임에 이미 존재하므로 인스턴스화는 생략되고(하이브리드)
        /// DetailPanel의 루트 레이아웃과 모델만 주입된다.
        /// </summary>
        private ComponentBlueprint BuildBlueprint() => ComponentBlueprint
            .Create("ShopFrame")
            .WithModel(BuildModel)
            .Patch("detail", BuildDetailPanel()).WithModel(() => new ViewModel());

        /// <summary>
        /// 상세 패널 설계도 — 행 순서·간격·여백만 선언한다 (절대 좌표 없음).
        /// 각 행은 카탈로그의 부품 프리팹. 모델 팩토리는 BuildModel이 만든
        /// Feature/PropModel 필드를 ViewModel로 감싸기만 한다 (기능 코드는 아래쪽에).
        /// </summary>
        private ComponentBlueprint BuildDetailPanel() => ComponentBlueprint
            .Create("DetailPanel")
                .Layout(Direction.Vertical, spacing: 14)
                .Padding(top: 12, right: 32, bottom: 16, left: 32)
            .Patch("caption", "CaptionRow")
                .WithModel(() => Models.Label("상세 패널 — Controller는 PropModel 값만 변경, 화면 갱신은 FeatureView 구독이 처리"))
            .Patch("info", "InfoRow").WithModel(BuildInfoModel)
            .Patch("frameCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ImageFeature ×2 — 아이콘(일반) / 등급별 9-slice 프레임 교체"))
            .Patch("qty", "QtyRow").WithModel(BuildQtyModel)
            .Patch("buy", "BuyButton").WithModel(BuildBuyModel)
            .Patch("buyCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ButtonFeature 단순 클릭 + InteractableFeature — 골드 부족 시 비활성"))
            .Patch("bgm", "BgmRow").WithModel(BuildBgmModel)
            .Patch("skip", "SkipRow").WithModel(() => new ViewModel().With(skipConfirm));

        // ---------------- 모델 구성 (기능 — 상태·구독·로직) ----------------

        /// <summary>
        /// 루트 모델. Open()이 패치 모델보다 먼저 실행하므로, 여기서 만든
        /// Feature/PropModel 필드를 패치 팩토리들이 안전하게 참조할 수 있다.
        /// </summary>
        private ViewModel BuildModel()
        {
            var vm = new ViewModel();

            // 반응형 — 모델은 variant 키만 알고, 좌표는 ResponsiveLayoutFeatureView(뷰)에 있다.
            screen = new ScreenFeature();
            vm.With(screen);

            // 하이브리드 키 — 틀(ShopFrame)에 이미 있는 허브에 모델만 주입된다.
            gold = new FormatNumberPropModel<long>(startGold, v => $"{v:n0} G");
            vm["title"] = Models.Label("상점");
            vm["gold"] = Models.Label(gold);
            vm["list"] = BuildScroller();
            logText = new PropModel<string>("");
            vm["log"] = Models.Label(logText);

            // 상세 패널 상태 — 뷰는 전부 Blueprint가 조립한다 (BuildDetailPanel).
            itemFrame = new PropModel<Sprite>();
            itemIcon = new PropModel<Sprite>();
            itemName = new PropModel<string>();
            itemDesc = new PropModel<string>();
            qty = new FormatNumberPropModel<int>(1, v => $"× {v}");
            buyLabel = new PropModel<string>("구매");
            canBuy = new PropModel<bool>(false);
            minusBtn = new ButtonFeature(allowHold: true);
            plusBtn = new ButtonFeature(allowHold: true);
            buyBtn = new ButtonFeature();
            bgm = new ToggleFeature(true);
            skipConfirm = new ToggleFeature(false);

            // ---- 입력 구독 (구독 대상이 전부 shop 트리의 Subject/Prop이므로
            //      shop.Dispose()와 함께 정리된다) ----
            minusBtn.OnClick.Subscribe(_ => AddQty(-1));
            minusBtn.OnHold.Subscribe(_ => AddQty(-1));
            plusBtn.OnClick.Subscribe(_ => AddQty(+1));
            plusBtn.OnHold.Subscribe(_ => AddQty(+1));
            buyBtn.OnClick.Subscribe(_ => Buy());

            bgm.IsOn.Prop.Skip(1).Subscribe(on => Log($"BGM {(on ? "켜짐" : "꺼짐")}"));
            skipConfirm.IsOn.Prop.Skip(1).Subscribe(on =>
            {
                pendingConfirm = false;
                Log($"구매 확인 생략: {(on ? "ON — 즉시 구매" : "OFF — 두 번 눌러 구매")}");
            });

            gold.Source.Subscribe(_ => UpdateBuyState());

            shop = vm;
            return vm;
        }

        private ViewModel BuildInfoModel()
        {
            var vm = new ViewModel();
            vm["frame"] = new ViewModel().With(new ImageFeature(itemFrame));
            vm["icon"] = new ViewModel().With(new ImageFeature(itemIcon));
            vm["name"] = new ViewModel().With(new TextFeature(itemName));
            vm["desc"] = new ViewModel().With(new TextFeature(itemDesc));
            return vm;
        }

        private ViewModel BuildQtyModel()
        {
            var vm = new ViewModel();
            vm["minus"] = new ViewModel().With(minusBtn);
            vm["qty"] = new ViewModel().With(new TextFeature(qty));
            vm["plus"] = new ViewModel().With(plusBtn);
            return vm;
        }

        private ViewModel BuildBuyModel() => new ViewModel()
            .With(new TextFeature(buyLabel))
            .With(buyBtn)
            .With(new InteractableFeature(canBuy));

        private ViewModel BuildBgmModel()
        {
            var vm = new ViewModel();
            vm["switch"] = new ViewModel().With(bgm);
            return vm;
        }

        private ViewModel BuildScroller()
        {
            var listVm = new ViewModel();

            // 소모품 섹션 — 공용 셀은 키로 해상 (CellCatalog 에셋)
            var itemCells = new ObservableList<IViewModel>();
            foreach (var data in GenerateEntries())
            {
                var cell = ShopCells.Item(data, Select);
                cell.AddTo(listVm); // 셀 VM의 Dispose 책임을 모델 트리에 연결
                itemCells.Add(cell);
            }
            var itemSection = new Section(itemCells, itemSectionOption)
            {
                ContentKey = ShopCellKeys.Item,
                Header = ShopCells.Header("소모품"),
                HeaderKey = ShopCellKeys.Header,
            };

            // 이벤트 섹션 — 일회성 셀은 ContentPrefab 직접 지정 (키 등록 불필요)
            var banner = ShopCells.Banner("주말 한정 — 전 품목 골드 환급 이벤트!");
            banner.AddTo(listVm);
            var bannerSection = new Section(new ObservableList<IViewModel>(new IViewModel[] { banner }), bannerSectionOption)
            {
                ContentPrefab = bannerPrefab,
                Header = ShopCells.Header("이벤트"),
                HeaderKey = ShopCellKeys.Header,
            };

            return listVm.With(new ScrollerFeature(new[] { itemSection, bannerSection }));
        }

        /// <summary>직렬화된 원본 아이템을 레벨 변형으로 늘린다 — 가상화 확인용 데이터.</summary>
        private List<ShopItemData> GenerateEntries()
        {
            var list = new List<ShopItemData>();
            if (items == null) return list;
            foreach (var item in items)
            {
                for (var lv = 1; lv <= Mathf.Max(1, levelsPerItem); lv++)
                {
                    list.Add(new ShopItemData(
                        $"{item.name} Lv.{lv}", item.description, item.price * lv, item.icon, item.frame));
                }
            }
            return list;
        }

        // ---------------- 상호작용 (모델 값 변경까지만 책임) ----------------

        private void Select(ShopItemData data)
        {
            selected = data;
            pendingConfirm = false;
            itemFrame.Value = data.frame;
            itemIcon.Value = data.icon;
            itemName.Value = data.name;
            itemDesc.Value = data.description;
            qty.Source.Value = 1;
            UpdateBuyState();
            Log($"선택: {data.name}");
        }

        private void AddQty(int delta)
        {
            if (selected == null) return;
            pendingConfirm = false;
            qty.Source.Value = Mathf.Clamp(qty.Source.Value + delta, 1, MaxQty);
            UpdateBuyState();
        }

        private void Buy()
        {
            if (selected == null) return;
            var count = qty.Source.Value;
            var total = selected.price * count;

            if (gold.Source.Value < total)
            {
                Log($"골드 부족 — {total:n0}G 필요");
                return;
            }
            if (!skipConfirm.IsOn.Value && !pendingConfirm)
            {
                pendingConfirm = true;
                Log($"'{selected.name}' ×{count} = {total:n0}G — 한 번 더 누르면 구매");
                return;
            }

            pendingConfirm = false;
            gold.Source.Value -= total; // 표시 갱신은 FormatNumberPropModel이 자동 처리
            Log($"구매 완료: {selected.name} ×{count} (−{total:n0}G)");
        }

        private void UpdateBuyState()
        {
            if (selected == null)
            {
                buyLabel.Value = "구매";
                canBuy.Value = false;
                return;
            }
            var total = selected.price * qty.Source.Value;
            buyLabel.Value = $"구매 — {total:n0}G";
            canBuy.Value = gold.Source.Value >= total;
        }

        private void Log(string message)
        {
            logLines.Enqueue(message);
            while (logLines.Count > MaxLogLines) logLines.Dequeue();
            logText.Value = string.Join("\n", logLines);
        }
    }
}
