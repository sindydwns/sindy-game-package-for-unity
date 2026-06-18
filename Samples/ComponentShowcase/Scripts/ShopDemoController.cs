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
    ///    모델 상태·구독은 기능 코드(ShopModel)에. 두 관심사가 파일로도 나뉜다.
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

        /// <summary>
        /// 현재 세대의 모델 — 반응형 상태·상점 로직은 전부 이 안에 있다.
        /// Open()/BuildModelTree()가 매번 <see cref="BuildModel"/>로 새 인스턴스를 만들어 이 필드를 교체하고,
        /// 설계도의 패치 팩토리(() => currentModel.BuildXxx())는 항상 이 최신 세대를 지연 참조한다.
        /// 주의: 단일 필드를 공유하므로 이 데모는 단일 인스턴스 전제다.
        /// </summary>
        private ShopModel currentModel;

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
            currentModel?.Dispose();
        }

        // ==================== 생명주기 — 최초 진입 ====================

        /// <summary>설계도 실행 — 루트 프리팹 인스턴스화 + 부품 조립 + 모델 바인딩 (최초 1회).</summary>
        private void OpenShop()
        {
            BindInstance(blueprint.Open());
            currentModel.Log("상점 데모 시작 — 셀을 눌러 아이템을 선택하세요");
        }

        /// <summary>새로 열린(또는 재-Open된) 인스턴스 공통 후처리 — 허브 확보·화면 구독·초기 선택.</summary>
        private void BindInstance(SindyComponent instance)
        {
            shopInstance = instance;
            ((ViewComponent)shopInstance).TryGetView("list", out scrollerHub);
            SubscribeScreen();
            currentModel.SelectFirst();
        }

        // ==================== 생명주기 — 처방1: 값 변경 ====================

        /// <summary>처방1 — 값 변경: 재바인딩 없이 PropModel 값과 셀 목록만 바꿔 상점을 교체한다.</summary>
        private void SwapShop()
        {
            currentModel.SwapVariant();
            scrollerHub?.Reload(); // 셀 수는 같지만 의도를 명확히 — 가상화 재계산
            currentModel.Log($"처방1 — 값 변경만으로 '{currentModel.Title.Value}' 전환 (재바인딩 없음)");
        }

        // ==================== 생명주기 — 처방2: 재시작(재-Open) ====================

        /// <summary>
        /// 처방2 — 재-Open: 파괴 후 설계도 재실행. 구조·디자인·상태가 전부 새로 만들어진다.
        /// 파괴·이전 모델 정리는 ReopenNextFrame이 방출 스택 밖에서 처리한다(disposeOld 기본 true).
        /// </summary>
        private void Reopen() => blueprint.ReopenNextFrame(shopInstance, onOpened: instance =>
        {
            BindInstance(instance);
            currentModel.Log("처방2 — 재-Open: 파괴 후 설계도 재실행 (상태 초기화)");
        });

        // ==================== 생명주기 — 처방3: 모델 재주입 ====================

        /// <summary>
        /// 처방3 — 모델 재주입: BuildModelTree()가 설계도 모양(레이아웃 포함)의 새 모델 트리를
        /// 만들고, 기존 인스턴스에 Bind한다. 뷰는 그대로, 내용만 통째로 교체된다.
        /// Bind·이전 모델 정리는 RebindNextFrame이 방출 스택 밖에서 처리한다(disposeOld 기본 true).
        /// </summary>
        private void Reinject()
        {
            var fresh = blueprint.BuildModelTree();
            shopInstance.RebindNextFrame(fresh, onRebound: () =>
            {
                SubscribeScreen(); // screen은 새 모델의 Feature이므로 Bind 이후 구독 재연결
                currentModel.SelectFirst();
                currentModel.Log("처방3 — BuildModelTree 재주입: 뷰·레이아웃 유지, 내용 통째 교체");
            });
        }

        // ==================== 공통 — 화면 변형 구독 ====================

        /// <summary>
        /// 화면 변형 전환 시 스크롤러 가상화 재계산.
        /// Bind 이후에 구독해야 ResponsiveLayoutFeatureView의 레이아웃 적용(먼저 구독됨)이
        /// 끝난 뒤 Reload가 실행된다 — 새 뷰포트 크기 기준으로 재계산됨.
        /// </summary>
        private void SubscribeScreen()
        {
            currentModel.Screen.Variant.Prop.Skip(1).Subscribe(v =>
            {
                currentModel.Log($"화면 변형 전환: {v}");
                if (scrollerHub != null) scrollerHub.Reload();
            });
        }

        // ==================== 설계도 (디자인 — 구조·배치·간격) ====================

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
        /// 팩토리는 currentModel을 지연 참조하므로 재주입 시 새 트리를 가리킨다.
        /// </summary>
        private ComponentBlueprint BuildDetailPanel() => ComponentBlueprint
            .Create("DetailPanel")
                .Layout(Direction.Vertical, spacing: 14)
                .Padding(top: 12, right: 32, bottom: 16, left: 32)

            // 정보 — 선택한 아이템 표시
            .Patch("caption", "CaptionRow")
                .WithModel(() => Models.Label("상세 패널 — Controller는 PropModel 값만 변경, 화면 갱신은 FeatureView 구독이 처리"))
            .Patch("info", "InfoRow").WithModel(() => currentModel.BuildInfoModel())
            .Patch("frameCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ImageFeature ×2 — 아이콘(일반) / 등급별 9-slice 프레임 교체"))

            // 수량·구매
            .Patch("qty", "QtyRow").WithModel(() => currentModel.BuildQtyModel())
            .Patch("buy", "BuyButton").WithModel(() => currentModel.BuildBuyModel())
            .Patch("buyCaption", "CaptionSmall")
                .WithModel(() => Models.Label("ButtonFeature 단순 클릭 + InteractableFeature — 골드 부족 시 비활성"))

            // 옵션 — BGM 토글 / 구매 확인 생략
            .Patch("bgm", "BgmRow").WithModel(() => currentModel.BuildBgmModel())
            .Patch("skip", "SkipRow").WithModel(() => new ViewModel().With(currentModel.SkipConfirm))

            // 데모 버튼 — 생명주기 3패턴 직접 시연
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
        /// 여기서 새 ShopModel을 만들어 두면 패치 팩토리(() => currentModel.BuildXxx())가 안전하게 참조한다.
        /// </summary>
        private ViewModel BuildModel()
        {
            currentModel = new ShopModel(items, levelsPerItem, startGold,
                itemSectionOption, bannerSectionOption, bannerPrefab);
            return currentModel.Root;
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
    }
}
