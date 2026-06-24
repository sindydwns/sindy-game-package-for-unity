using R3;
using Sindy.Reactive;
using Sindy.View;
using Sindy.View.Features;
using Sindy.View.Parts;
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
            shopInstance.TryGetView("list", out scrollerHub);
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
        /// 기본 부품 키트(원자 부품 + SindyKit 합성 Blueprint)만으로 조합한다.
        /// 모델 팩토리는 ShopModel의 상태(PropModel/Feature)를 ViewModel로 감싸기만 한다.
        /// 팩토리는 currentModel을 지연 참조하므로 재주입 시 새 트리를 가리킨다.
        ///
        /// 복합 행(InfoRow/QtyRow)은 전용 프리팹 대신 원자 부품을 경로로 중첩 조합하고,
        /// 버튼·토글 행은 SindyKit.ButtonLabel/ToggleRow로 치환했다.
        /// </summary>
        private ComponentBlueprint BuildDetailPanel() => ComponentBlueprint
            .Create("DetailPanel")
                .Layout(Direction.Vertical, spacing: 14)
                .Padding(top: 12, right: 32, bottom: 16, left: 32)


            // InfoRow → 컨테이너 + 프레임(아이콘 중첩) + 이름 + 설명 (ImageFeature ×2)
            .Patch("info", PartKeys.Container).Layout(Direction.Vertical, spacing: 6).Align(TextAnchor.UpperCenter).Flexible(1)
                .WithModel(() => new ViewModel())
            .Patch("info.frame", PartKeys.Icon).Size(120, 120)
                .WithModel(() => new ViewModel().With(new ImageFeature(currentModel.ItemFrame)))
            .Patch("info.frame.icon", PartKeys.Icon).Size(96, 96)
                .WithModel(() => new ViewModel().With(new ImageFeature(currentModel.ItemIcon)))
            .Patch("info.name", PartKeys.Label)
                .WithModel(() => Models.Label(currentModel.ItemName))
            .Patch("info.tier", PartKeys.Label)
                .WithModel(() => Models.Label(currentModel.TierText, 22).With(new ColorFeature(currentModel.TierColor)))
            .Patch("info.desc", PartKeys.Label)
                .WithModel(() => Models.Label(currentModel.ItemDesc, 22))


            // QtyRow → 가로 컨테이너: 캡션 + 마이너스(ButtonLabel) + 수량 + 플러스(ButtonLabel)
            .Patch("qty", PartKeys.Container).Layout(Direction.Horizontal, spacing: 8).Align(TextAnchor.MiddleLeft).Flexible(1)
                .WithModel(() => new ViewModel())
            .Patch("qty.caption", PartKeys.Label).WithModel(() => Models.Label("수량"))
            .Patch("qty.gap", PartKeys.Spacer).Flexible(1).WithModel(() => new ViewModel())
            .Patch("qty.minus", SindyKit.ButtonLabel).WithModel(() => new ViewModel().With(currentModel.MinusBtn))
            .Patch("qty.minus.label", PartKeys.Label).WithModel(() => Models.Label("−"))
            .Patch("qty.value", PartKeys.Label).WithModel(() => Models.Label(currentModel.Qty))
            .Patch("qty.plus", SindyKit.ButtonLabel).WithModel(() => new ViewModel().With(currentModel.PlusBtn))
            .Patch("qty.plus.label", PartKeys.Label).WithModel(() => Models.Label("+"))

            // BuyButton → ButtonLabel (버튼 허브: ButtonFeature + InteractableFeature, 라벨 자식)
            .Patch("buy", SindyKit.ButtonLabel).Layout(Direction.Horizontal, spacing: 0).Padding(8, 24, 8, 24).Align(TextAnchor.MiddleCenter).Size(-1, 96).Flexible(1)
                .WithModel(() => new ViewModel().With(currentModel.BuyBtn).With(new InteractableFeature(currentModel.CanBuy))
                    .With(new ColorFeature(new Color(0.231f, 0.510f, 0.965f))))
            .Patch("buy.label", PartKeys.Label).WithModel(() => Models.Label(currentModel.BuyLabel))

            // SkipRow → SindyKit.ToggleRow
            .Patch("skip", SindyKit.ToggleRow).Layout(Direction.Horizontal, spacing: 16).Align(TextAnchor.MiddleLeft).Size(-1, 96).Flexible(1).WithModel(() => new ViewModel())
            .Patch("skip.label", PartKeys.Label).WithModel(() => Models.Label("구매 확인 생략"))
            .Patch("skip.toggle", PartKeys.Toggle).WithModel(() => new ViewModel().With(currentModel.SkipConfirm))

            // 데모 버튼 — 생명주기 3패턴 직접 시연
            .Patch("demo", PartKeys.Container)
                .Layout(Direction.Horizontal, spacing: 8).Flexible(1)
                .WithModel(() => new ViewModel())
            .Patch("demo.swap", SindyKit.ButtonLabel).Layout(Direction.Horizontal, spacing: 0).Padding(8, 24, 8, 24).Align(TextAnchor.MiddleCenter).Size(-1, 96).Flexible(1).WithModel(() => DemoButton(SwapShop))
            .Patch("demo.swap.label", PartKeys.Label).WithModel(() => Models.Label("상점 교체", 28))
            .Patch("demo.reopen", SindyKit.ButtonLabel).Layout(Direction.Horizontal, spacing: 0).Padding(8, 24, 8, 24).Align(TextAnchor.MiddleCenter).Size(-1, 96).Flexible(1).WithModel(() => DemoButton(Reopen))
            .Patch("demo.reopen.label", PartKeys.Label).WithModel(() => Models.Label("재시작", 28))
            .Patch("demo.reinject", SindyKit.ButtonLabel).Layout(Direction.Horizontal, spacing: 0).Padding(8, 24, 8, 24).Align(TextAnchor.MiddleCenter).Size(-1, 96).Flexible(1).WithModel(() => DemoButton(Reinject))
            .Patch("demo.reinject.label", PartKeys.Label).WithModel(() => Models.Label("모델 재주입", 28));

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

        /// <summary>
        /// 생명주기 데모 버튼의 버튼 허브 모델 — 라벨은 ButtonLabel의 자식 패치가 별도로 주입한다.
        /// 클릭 구독은 버튼 모델의 ButtonFeature(Subject)에 묶여 모델과 함께 정리된다.
        /// </summary>
        private static ViewModel DemoButton(System.Action onClick)
        {
            var vm = new ViewModel().With(new ButtonFeature());
            vm.Feature<ButtonFeature>().OnClick.Subscribe(_ => onClick());
            return vm;
        }
    }
}
