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
    /// 한 화면에서 시연하는 것:
    /// - 출력: TextFeature(타이틀·이름·로그), ImageFeature(아이콘·9-slice 프레임 교체),
    ///         FormatNumberPropModel(골드 자가 갱신 표시)
    /// - 입력: ButtonFeature 클릭(구매) / 홀드(수량 ±, allowHold 옵션),
    ///         ToggleFeature 스위치(BGM)·체크박스(구매 확인 생략) — 같은 Feature, 스킨만 다름
    /// - 스크롤러: ScrollerFeature 섹션 2개(소모품=셀 키 해상, 이벤트 배너=ContentPrefab 직접 지정)
    ///
    /// 원칙: Controller는 ViewModel(Feature)만 건드린다. Unity UI 직접 호출 없음.
    /// 모든 화면 갱신은 FeatureView의 Bind 구독이 처리한다.
    /// </summary>
    public class ShopDemoController : MonoBehaviour
    {
        [Header("뷰 (씬)")]
        [SerializeField] private ViewComponent shopView;

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
        private ToggleFeature bgm;
        private ToggleFeature skipConfirm;

        private ShopItemData selected;
        private bool pendingConfirm;
        private readonly Queue<string> logLines = new();
        private const int MaxLogLines = 5;
        private const int MaxQty = 99;

        private void Start()
        {
            shop = BuildModel();
            shopView.Bind(shop);

            if (items != null && items.Length > 0)
                Select(items[0]);
            Log("상점 데모 시작 — 셀을 눌러 아이템을 선택하세요");
        }

        private void OnDestroy()
        {
            // 순서 중요: 1) FeatureView 구독 해제 → 2) 모델 내부 구독 해제
            if (shopView != null) shopView.Bind(null);
            shop?.Dispose();
        }

        // ---------------- 모델 구성 ----------------

        private ViewModel BuildModel()
        {
            var vm = new ViewModel();

            // 헤더 — 자가 갱신 모델 주입 (gold.Source만 바꾸면 표시는 자동)
            gold = new FormatNumberPropModel<long>(startGold, v => $"{v:n0} G");
            vm["title"] = Models.Label("상점");
            vm["gold"] = Models.Label(gold);

            // 스크롤러 — 섹션 2개
            vm["list"] = BuildScroller();

            // 상세 패널 — 출력형 Feature는 전부 외부 모델 주입 생성자를 사용,
            // Controller는 PropModel 필드만 보유한다.
            itemFrame = new PropModel<Sprite>();
            itemIcon = new PropModel<Sprite>();
            itemName = new PropModel<string>();
            itemDesc = new PropModel<string>();
            qty = new FormatNumberPropModel<int>(1, v => $"× {v}");
            buyLabel = new PropModel<string>("구매");
            canBuy = new PropModel<bool>(false);
            logText = new PropModel<string>("");
            bgm = new ToggleFeature(true);
            skipConfirm = new ToggleFeature(false);

            vm["detail.frame"] = new ViewModel().With(new ImageFeature(itemFrame));
            vm["detail.icon"] = new ViewModel().With(new ImageFeature(itemIcon));
            vm["detail.name"] = new ViewModel().With(new TextFeature(itemName));
            vm["detail.desc"] = new ViewModel().With(new TextFeature(itemDesc));
            vm["detail.qty"] = new ViewModel().With(new TextFeature(qty));
            vm["detail.minus"] = Models.Button(allowHold: true);
            vm["detail.plus"] = Models.Button(allowHold: true);
            vm["detail.buy"] = new ViewModel()
                .With(new TextFeature(buyLabel))
                .With(new ButtonFeature())
                .With(new InteractableFeature(canBuy));
            vm["detail.bgm"] = new ViewModel().With(bgm);
            vm["detail.skip"] = new ViewModel().With(skipConfirm);

            // 로그 바
            vm["log"] = Models.Label(logText);

            // ---- 입력 구독 (구독 대상이 전부 shop 트리의 Subject/Prop이므로
            //      shop.Dispose()와 함께 정리된다) ----
            var minus = vm["detail.minus"].Feature<ButtonFeature>();
            var plus = vm["detail.plus"].Feature<ButtonFeature>();
            minus.OnClick.Subscribe(_ => AddQty(-1));
            minus.OnHold.Subscribe(_ => AddQty(-1));
            plus.OnClick.Subscribe(_ => AddQty(+1));
            plus.OnHold.Subscribe(_ => AddQty(+1));

            vm["detail.buy"].Feature<ButtonFeature>().OnClick.Subscribe(_ => Buy());

            bgm.IsOn.Prop.Skip(1).Subscribe(on => Log($"BGM {(on ? "켜짐" : "꺼짐")}"));
            skipConfirm.IsOn.Prop.Skip(1).Subscribe(on =>
            {
                pendingConfirm = false;
                Log($"구매 확인 생략: {(on ? "ON — 즉시 구매" : "OFF — 두 번 눌러 구매")}");
            });

            gold.Source.Subscribe(_ => UpdateBuyState());

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
