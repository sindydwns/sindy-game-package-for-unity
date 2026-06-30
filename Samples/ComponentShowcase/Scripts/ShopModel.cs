using System;
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
    /// 상점 한 판의 반응형 상태 + 상점 로직을 한곳에 모은 모델.
    ///
    /// Controller가 흩뿌리던 ~20개 멤버변수를 여기로 옮겨, 상태를 만드는 곳과
    /// 변이하는 곳(Select/AddQty/Buy/UpdateBuyState)을 같은 클래스 안에 가둔다.
    /// Controller는 이 객체의 좁은 API(SelectFirst/SwapVariant/Log/Build*Model)만 호출하므로
    /// 어느 변수가 어디서 바뀌는지 추적할 필요가 사라진다.
    ///
    /// 생성자 한 번에 전체 모델 트리(Root)와 모든 입력 구독을 구성하고,
    /// 구독 대상은 전부 Root 트리의 Subject/Prop이라 Dispose() 한 번으로 정리된다.
    /// </summary>
    internal sealed class ShopModel : IDisposable
    {
        private const int MaxLogLines = 5;
        private const int MaxQty = 99;

        // ---- 설계도/스크롤러 구성에 필요한 외부 설정 (주입) ----
        private readonly ShopItemData[] items;
        private readonly int levelsPerItem;
        private readonly SectionOption itemSectionOption;
        private readonly SectionOption bannerSectionOption;
        private readonly SindyComponent bannerPrefab;

        // ---- Controller/설계도가 참조하는 공개 표면 ----
        public ViewModel Root { get; }
        public ScreenFeature Screen { get; }
        public PropModel<string> Title { get; }
        public ToggleFeature SkipConfirm { get; }
        public PropModel<string> TierText => tierText;
        public PropModel<Color> TierColor => tierColor;

        // 원자 부품 조립용 상태 노출 — Blueprint가 부품별로 모델을 구성하므로
        // 단일 프리팹 시절의 Build*Model 팩토리 대신 PropModel/Feature를 직접 참조한다.
        public PropModel<Sprite> ItemFrame => itemFrame;
        public PropModel<Sprite> ItemIcon => itemIcon;
        public PropModel<string> ItemName => itemName;
        public PropModel<string> ItemDesc => itemDesc;
        public PropModel<string> Qty => qty;
        public PropModel<string> BuyLabel => buyLabel;
        public PropModel<bool> CanBuy => canBuy;
        public ButtonFeature MinusBtn => minusBtn;
        public ButtonFeature PlusBtn => plusBtn;
        public ButtonFeature BuyBtn => buyBtn;

        // ---- 상세 패널 상태 (전부 ShopModel 내부에서만 변이) ----
        private readonly FormatNumberPropModel<long> gold;
        private readonly FormatNumberPropModel<int> qty;
        private readonly PropModel<string> itemName;
        private readonly PropModel<string> itemDesc;
        private readonly PropModel<Sprite> itemIcon;
        private readonly PropModel<Sprite> itemFrame;
        private readonly PropModel<string> buyLabel;
        private readonly PropModel<bool> canBuy;
        private readonly PropModel<string> logText;
        private readonly PropModel<string> tierText;
        private readonly PropModel<Color> tierColor;
        private readonly ButtonFeature minusBtn;
        private readonly ButtonFeature plusBtn;
        private readonly ButtonFeature buyBtn;

        // ---- 스크롤러 셀 ----
        private ViewModel listVm;
        private ObservableList<IViewModel> itemCells;
        private readonly List<ViewModel> liveCells = new();

        // ---- 상호작용 내부 상태 ----
        private ShopItemData selected;
        private bool pendingConfirm;
        private int variant; // 0=일반, 1=할인 — 처방1(값 변경) 시연용
        private readonly Queue<string> logLines = new();

        public ShopModel(ShopItemData[] items, int levelsPerItem, long startGold,
            SectionOption itemSectionOption, SectionOption bannerSectionOption, SindyComponent bannerPrefab)
        {
            this.items = items;
            this.levelsPerItem = levelsPerItem;
            this.itemSectionOption = itemSectionOption;
            this.bannerSectionOption = bannerSectionOption;
            this.bannerPrefab = bannerPrefab;

            var vm = new ViewModel();

            // 반응형 — 모델은 variant 키만 알고, 좌표는 ResponsiveLayoutFeatureView(뷰)에 있다.
            Screen = new ScreenFeature();
            vm.With(Screen);

            // 하이브리드 키 — 틀(ShopFrame)에 이미 있는 허브에 모델만 주입된다.
            Title = new PropModel<string>("상점");
            gold = new FormatNumberPropModel<long>(startGold, v => $"{v:n0} G");
            vm["title"] = Models.Empty().AddTextFeature(Title);
            vm["gold"] = Models.Empty().AddTextFeature(gold);
            vm["list"] = BuildScroller();
            logText = new PropModel<string>("");
            vm["log"] = Models.Empty().AddTextFeature(logText);

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
            SkipConfirm = new ToggleFeature(false);
            tierText = new PropModel<string>("");
            tierColor = new PropModel<Color>(Color.white);

            // ---- 입력 구독 (구독 대상이 전부 Root 트리의 Subject/Prop이므로
            //      Dispose()와 함께 정리된다) ----
            minusBtn.OnClick.Subscribe(_ => AddQty(-1));
            minusBtn.OnHold.Subscribe(_ => AddQty(-1));
            plusBtn.OnClick.Subscribe(_ => AddQty(+1));
            plusBtn.OnHold.Subscribe(_ => AddQty(+1));
            buyBtn.OnClick.Subscribe(_ => Buy());

            SkipConfirm.IsOn.Prop.Skip(1).Subscribe(on =>
            {
                pendingConfirm = false;
                Log($"구매 확인 생략: {(on ? "ON — 즉시 구매" : "OFF — 두 번 눌러 구매")}");
            });

            gold.Source.Subscribe(_ => UpdateBuyState());

            Root = vm;
        }

        public void Dispose() => Root?.Dispose();

        private ViewModel BuildScroller()
        {
            listVm = new ViewModel();
            itemCells = new ObservableList<IViewModel>();
            liveCells.Clear(); // 이전 모델의 셀은 이전 모델 Dispose가 정리한다
            PopulateCells();

            // 소모품 섹션 — 공용 셀은 키로 해상 (CellCatalog 에셋)
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

        /// <summary>셀 목록을 현재 상점 변형(일반/할인)에 맞춰 다시 채운다 — 처방1의 핵심.</summary>
        private void PopulateCells()
        {
            foreach (var cell in liveCells) cell.Dispose();
            liveCells.Clear();
            itemCells.Clear();

            foreach (var data in GenerateEntries())
            {
                var cell = ShopCells.Item(data, Select);
                cell.AddTo(listVm); // 셀 VM의 Dispose 책임을 모델 트리에 연결
                liveCells.Add(cell);
                itemCells.Add(cell);
            }
        }

        /// <summary>직렬화된 원본 아이템을 레벨 변형으로 늘린다 — 가상화 확인용 데이터. 할인 상점은 반값.</summary>
        private List<ShopItemData> GenerateEntries()
        {
            var list = new List<ShopItemData>();
            if (items == null) return list;
            var discount = variant == 1;
            foreach (var item in items)
            {
                for (var lv = 1; lv <= Mathf.Max(1, levelsPerItem); lv++)
                {
                    var price = item.price * lv;
                    if (discount) price /= 2;
                    var name = discount ? $"[할인] {item.name} Lv.{lv}" : $"{item.name} Lv.{lv}";
                    list.Add(new ShopItemData(name, item.description, price, item.icon, item.frame) { level = lv });
                }
            }
            return list;
        }

        // ---------------- Controller가 호출하는 좁은 API ----------------

        /// <summary>첫 아이템을 선택한다 (아이템이 있을 때만). Open/재주입 직후 초기 선택용.</summary>
        public void SelectFirst()
        {
            if (items != null && items.Length > 0) Select(items[0]);
        }

        /// <summary>처방1 — 일반/할인 변형 토글: 타이틀과 셀 목록만 바꾸고 선택을 초기화한다.</summary>
        public void SwapVariant()
        {
            variant = 1 - variant;
            Title.Value = variant == 0 ? "상점" : "할인 상점";
            PopulateCells();
            selected = null;
            UpdateBuyState();
        }

        public void Log(string message)
        {
            logLines.Enqueue(message);
            while (logLines.Count > MaxLogLines) logLines.Dequeue();
            logText.Value = string.Join("\n", logLines);
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
            var tier = ShopCells.Tier(data.level);
            tierText.Value = tier.text;
            tierColor.Value = tier.color;
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
            if (!SkipConfirm.IsOn.Value && !pendingConfirm)
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
    }
}
