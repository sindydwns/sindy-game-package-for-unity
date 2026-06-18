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
    ///    모델 상태·구독은 기능 코드(ShopModel)에. 두 관심사가 파일 안에서도 나뉜다.
    /// 3. 하이브리드 — 틀(헤더·스크롤러·로그 바)은 ShopFrame 프리팹에 두고
    ///    모델만 주입(title/gold/list/log), 가변 부품(상세 패널)은 코드로 조합.
    /// 4. 생명주기 3패턴 — 하단 데모 버튼으로 직접 시연:
    ///    상점 교체 = 값 변경(재바인딩 없음) / 재시작 = 재-Open(파괴 후 재실행) /
    ///    모델 재주입 = BuildModelTree(구조·레이아웃 유지, 내용 통째 교체).
    ///
    /// 원칙: Controller는 생명주기·설계도(Blueprint)·뷰 연결만 담당하고,
    /// 모든 반응형 상태와 상점 로직은 <see cref="ShopModel"/> 한 곳에 모은다.
    /// Unity UI 직접 호출은 없으며, 화면 갱신은 FeatureView의 Bind 구독이 처리한다.
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

        // ---- Controller가 직접 다루는 상태: 생명주기·설계도·뷰 연결뿐 ----
        private ComponentBlueprint blueprint;
        private SindyComponent shopInstance;
        private SindyComponent scrollerHub;
        private ShopModel model; // 반응형 상태·상점 로직은 전부 이 안에 (BuildModel이 매 트리마다 새로 만든다)

        private void Start()
        {
            // 설계도는 한 번 만들고 재사용한다 — Open()/BuildModelTree() 모두 같은 설계도에서.
            blueprint = BuildBlueprint();
            OpenShop();
        }

        private void OnDestroy()
        {
            // 순서 중요: 1) FeatureView 구독 해제 → 2) 모델 내부 구독 해제
            if (shopInstance != null) shopInstance.Bind(null);
            model?.Dispose();
        }

        // ---------------- 생명주기 (Open / 재-Open / 재주입) ----------------

        /// <summary>설계도 실행 — 루트 프리팹 인스턴스화 + 부품 조립 + 모델 바인딩 (최초 1회).</summary>
        private void OpenShop()
        {
            BindInstance(blueprint.Open()); // BuildModel 실행 → model이 새 트리를 가리킨다
            model.Log("상점 데모 시작 — 셀을 눌러 아이템을 선택하세요");
        }

        /// <summary>새로 열린(또는 재-Open된) 인스턴스 공통 후처리 — 허브 확보·화면 구독·초기 선택.</summary>
        private void BindInstance(SindyComponent instance)
        {
            shopInstance = instance;
            ((ViewComponent)shopInstance).TryGetView("list", out scrollerHub);
            SubscribeScreen();
            model.SelectFirst();
        }

        /// <summary>
        /// 처방2 — 재-Open: 파괴 후 설계도 재실행. 구조·디자인·상태가 전부 새로 만들어진다.
        /// 파괴·이전 모델 정리는 ReopenNextFrame이 방출 스택 밖에서 처리한다(disposeOld 기본 true).
        /// </summary>
        private void Reopen() => blueprint.ReopenNextFrame(shopInstance, onOpened: instance =>
        {
            BindInstance(instance); // Open()이 BuildModel 재실행 → model이 새 트리를 가리킨다
            model.Log("처방2 — 재-Open: 파괴 후 설계도 재실행 (상태 초기화)");
        });

        /// <summary>
        /// 처방3 — 모델 재주입: BuildModelTree()가 설계도 모양(레이아웃 포함)의 새 모델 트리를
        /// 만들고, 기존 인스턴스에 Bind한다. 뷰는 그대로, 내용만 통째로 교체된다.
        /// Bind·이전 모델 정리는 RebindNextFrame이 방출 스택 밖에서 처리한다(disposeOld 기본 true).
        /// </summary>
        private void Reinject()
        {
            var fresh = blueprint.BuildModelTree(); // BuildModel 재실행 → model이 새 트리를 가리킨다
            shopInstance.RebindNextFrame(fresh, onRebound: () =>
            {
                SubscribeScreen(); // screen은 새 모델의 Feature이므로 Bind 이후 구독 재연결
                model.SelectFirst();
                model.Log("처방3 — BuildModelTree 재주입: 뷰·레이아웃 유지, 내용 통째 교체");
            });
        }

        /// <summary>처방1 — 값 변경: 재바인딩 없이 PropModel 값과 셀 목록만 바꿔 상점을 교체한다.</summary>
        private void SwapShop()
        {
            model.SwapVariant();
            scrollerHub?.Reload(); // 셀 수는 같지만 의도를 명확히 — 가상화 재계산
            model.Log($"처방1 — 값 변경만으로 '{model.Title.Value}' 전환 (재바인딩 없음)");
        }

        /// <summary>
        /// 화면 변형 전환 시 스크롤러 가상화 재계산.
        /// Bind 이후에 구독해야 ResponsiveLayoutFeatureView의 레이아웃 적용(먼저 구독됨)이
        /// 끝난 뒤 Reload가 실행된다 — 새 뷰포트 크기 기준으로 재계산됨.
        /// </summary>
        private void SubscribeScreen()
        {
            model.Screen.Variant.Prop.Skip(1).Subscribe(v =>
            {
                model.Log($"화면 변형 전환: {v}");
                if (scrollerHub != null) scrollerHub.Reload();
            });
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
        /// 각 행은 카탈로그의 부품 프리팹. 모델 팩토리는 ShopModel이 만든
        /// Feature/PropModel을 ViewModel로 감싸기만 한다 (기능 코드는 ShopModel에).
        /// 팩토리는 현재 model을 지연 참조하므로 재주입 시 새 트리를 가리킨다.
        /// </summary>
        private ComponentBlueprint BuildDetailPanel() => ComponentBlueprint
            .Create("DetailPanel")
                .Layout(Direction.Vertical, spacing: 14)
                .Padding(top: 12, right: 32, bottom: 16, left: 32)
            .Patch("caption", "CaptionRow")
                .WithModel(() => Models.Label("상세 패널 — Controller는 PropModel 값만 변경, 화면 갱신은 FeatureView 구독이 처리"))
            .Patch("info", "InfoRow").WithModel(() => model.BuildInfoModel())
            .Patch("frameCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ImageFeature ×2 — 아이콘(일반) / 등급별 9-slice 프레임 교체"))
            .Patch("qty", "QtyRow").WithModel(() => model.BuildQtyModel())
            .Patch("buy", "BuyButton").WithModel(() => model.BuildBuyModel())
            .Patch("buyCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ButtonFeature 단순 클릭 + InteractableFeature — 골드 부족 시 비활성"))
            .Patch("bgm", "BgmRow").WithModel(() => model.BuildBgmModel())
            .Patch("skip", "SkipRow").WithModel(() => new ViewModel().With(model.SkipConfirm))
            .Patch("demoCaption", "CaptionSmall")
                .WithModel(() => Models.Label("생명주기 3패턴 — 값 변경(상점 교체) · 재-Open(재시작) · BuildModelTree(모델 재주입)"))
            .Patch("demo", "PartContainer")
                .Layout(Direction.Horizontal, spacing: 8)
                .WithModel(() => new ViewModel())
            .Patch("demo.swap", "BuyButton").Size(width: 165, height: 56)
                .WithModel(() => DemoButton("상점 교체", SwapShop))
            .Patch("demo.reopen", "BuyButton").Size(width: 165, height: 56)
                .WithModel(() => DemoButton("재시작", Reopen))
            .Patch("demo.reinject", "BuyButton").Size(width: 165, height: 56)
                .WithModel(() => DemoButton("모델 재주입", Reinject));

        /// <summary>
        /// 루트 모델 팩토리. Open()/BuildModelTree()가 패치 모델보다 먼저 실행하므로,
        /// 여기서 새 ShopModel을 만들어 두면 패치 팩토리(() => model.BuildXxx())가 안전하게 참조한다.
        /// 주의: 팩토리가 단일 model 필드를 공유하므로 이 데모는 단일 인스턴스 전제다.
        /// </summary>
        private ViewModel BuildModel()
        {
            model = new ShopModel(items, levelsPerItem, startGold,
                itemSectionOption, bannerSectionOption, bannerPrefab);
            return model.Root;
        }

        /// <summary>생명주기 데모 버튼 — 클릭 구독은 버튼 모델에 묶여 모델과 함께 정리된다.</summary>
        private static ViewModel DemoButton(string label, System.Action onClick)
        {
            var vm = new ViewModel()
                .With(new TextFeature(label))
                .With(new ButtonFeature())
                .With(new InteractableFeature());
            vm.Feature<ButtonFeature>().OnClick.Subscribe(_ => onClick());
            return vm;
        }

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
        private sealed class ShopModel : System.IDisposable
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
            private readonly ButtonFeature minusBtn;
            private readonly ButtonFeature plusBtn;
            private readonly ButtonFeature buyBtn;
            private readonly ToggleFeature bgm;

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
                vm["title"] = Models.Label(Title);
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
                SkipConfirm = new ToggleFeature(false);

                // ---- 입력 구독 (구독 대상이 전부 Root 트리의 Subject/Prop이므로
                //      Dispose()와 함께 정리된다) ----
                minusBtn.OnClick.Subscribe(_ => AddQty(-1));
                minusBtn.OnHold.Subscribe(_ => AddQty(-1));
                plusBtn.OnClick.Subscribe(_ => AddQty(+1));
                plusBtn.OnHold.Subscribe(_ => AddQty(+1));
                buyBtn.OnClick.Subscribe(_ => Buy());

                bgm.IsOn.Prop.Skip(1).Subscribe(on => Log($"BGM {(on ? "켜짐" : "꺼짐")}"));
                SkipConfirm.IsOn.Prop.Skip(1).Subscribe(on =>
                {
                    pendingConfirm = false;
                    Log($"구매 확인 생략: {(on ? "ON — 즉시 구매" : "OFF — 두 번 눌러 구매")}");
                });

                gold.Source.Subscribe(_ => UpdateBuyState());

                Root = vm;
            }

            public void Dispose() => Root?.Dispose();

            // ---------------- 설계도가 호출하는 패치 모델 팩토리 ----------------

            public ViewModel BuildInfoModel()
            {
                var vm = new ViewModel();
                vm["frame"] = new ViewModel().With(new ImageFeature(itemFrame));
                vm["icon"] = new ViewModel().With(new ImageFeature(itemIcon));
                vm["name"] = new ViewModel().With(new TextFeature(itemName));
                vm["desc"] = new ViewModel().With(new TextFeature(itemDesc));
                return vm;
            }

            public ViewModel BuildQtyModel()
            {
                var vm = new ViewModel();
                vm["minus"] = new ViewModel().With(minusBtn);
                vm["qty"] = new ViewModel().With(new TextFeature(qty));
                vm["plus"] = new ViewModel().With(plusBtn);
                return vm;
            }

            public ViewModel BuildBuyModel() => new ViewModel()
                .With(new TextFeature(buyLabel))
                .With(buyBtn)
                .With(new InteractableFeature(canBuy));

            public ViewModel BuildBgmModel()
            {
                var vm = new ViewModel();
                vm["switch"] = new ViewModel().With(bgm);
                return vm;
            }

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
                        list.Add(new ShopItemData(name, item.description, price, item.icon, item.frame));
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
}
