# SindyComponent & FeatureView

> **FeatureView 아키텍처** (2026-06 전환 완료). 설계 배경과 Decision Log는
> [FEATURE_VIEW_SCENARIO.md](FEATURE_VIEW_SCENARIO.md), 실습은 [SINDY_COMPONENT_TUTORIAL.md](SINDY_COMPONENT_TUTORIAL.md) 참고.

## 개념 한 장 요약

타입별 컴포넌트(TextComponent, ButtonComponent...)는 없다.
**SindyComponent(허브) + FeatureView(능력 단위)** 조합으로 모든 UI를 구성한다.

```
[모델 측]                            [뷰 측 - 한 GameObject]
ViewModel                            SindyComponent              ← 허브: ReactiveProperty<IViewModel>
 ├─ TextFeature("공격")        ◀──▶   TextFeatureView            ← TMP_Text 출력
 ├─ ButtonFeature(allowHold)   ◀──▶   ButtonFeatureView          ← 클릭+홀드 입력 (포인터 이벤트 단일 소유)
 ├─ GaugeFeature(0.7f)         ◀──▶   GaugeFeatureView           ← Image fillAmount 출력
 └─ InteractableFeature        ◀──▶   InteractableFeatureView    ← CanvasGroup 제어
```

- "이 오브젝트가 뭘 할 수 있는가" = Inspector의 FeatureView 목록 그 자체 (1:1이므로 자명)
- 모델 교체/해제는 ReactiveProperty 스트림으로 모든 FeatureView에 자동 전파

## 설계 4원칙

1. **1:1 대칭** — `ModelFeature`(모델) ↔ `FeatureView`(뷰)는 항상 1:1. N:1은 채택하지 않음.
2. **Feature 경계 기준** — 같은 저수준 입력 자원(포인터 제스처 공간 등)을 공유하는 동작은
   **한 Feature의 내부 옵션**으로 통합한다(예: 클릭/홀드 → `ButtonFeature(allowHold:)`).
   독립 동작 가능한 능력은 별도 Feature로 분리한다(예: Interactable, Visibility, Highlight).
3. **역방향 참조** — FeatureView가 `[RequireComponent(typeof(SindyComponent))]`로 허브를 보장하고,
   스스로 허브를 찾아 구독한다. 허브는 FeatureView의 존재를 모른다(릴리스 빌드 기준).
4. **ReactiveProperty 기반 모델 전파** — 허브는 `ReactiveProperty<IViewModel>`을 보유.
   R3의 "구독 즉시 현재 값 방출" 의미론이 비활성 Bind·늦은 Awake·런타임 AddComponent
   타이밍 문제를 전부 해결한다.

---

## MVVM 사용 원칙

> **Controller는 ViewModel(Feature)만 건드린다. Unity UI를 직접 호출하는 코드는 FeatureView의 `Bind()` 안에서만 작성한다.**

게임 로직은 항상 Feature의 reactive property를 바꾸는 것까지만 책임지고, 그 변경이 화면에 반영되는 일은 FeatureView에 등록된 구독이 처리합니다. 외부에서 GameObject API(`SetActive`, `text = ...` 등)를 직접 호출하지 않습니다. 모든 View 갱신이 FeatureView로 모이면 화면 변경의 진입점이 단일화되고, 모델만 보고도 UI 동작을 재구성할 수 있어 테스트와 디버깅이 단순해집니다.

---

## 핵심 클래스

### SindyComponent (허브)

```csharp
public class SindyComponent : MonoBehaviour
{
    public ReadOnlyReactiveProperty<IViewModel> Model { get; }   // FeatureView가 구독
    public IViewModel CurrentModel { get; }

    public SindyComponent Bind(IViewModel newModel);   // 같은 인스턴스면 방출 없음 (same-instance 스킵)
    public void Reload();                              // ForceNotify — 의도적 전체 재초기화
    public void SetParent(SindyComponent parent);      // 부모-자식 연쇄 해제 연결
}
```

- 모델은 **필드 초기화된** `ReactiveProperty<IViewModel>`이므로 허브의 Awake 실행 여부와
  무관하게 비활성 상태에서도 Bind 가능
- `Bind(null)` → 스트림으로 null 전파 → 각 FeatureView가 스스로 정리
- LinkState(부모-자식 연쇄 해제)는 기존 구조 유지

### FeatureView\<TFeature\> — dispose-then-bind를 구조로 강제

```csharp
[RequireComponent(typeof(SindyComponent))]
public abstract class FeatureView<TFeature> : MonoBehaviour, IFeatureView where TFeature : ModelFeature
{
    protected abstract void Bind(TFeature feature, ICollection<IDisposable> disposables);
    protected virtual void Clear() { }
}
```

- Awake에서 허브의 `Model` 스트림을 구독. 모델이 바뀔 때마다 **항상 이전 구독을 먼저 해제**한 뒤
  Feature가 있으면 `Bind`, 없으면(또는 null 모델) `Clear` 호출.
- 구현자가 작성하는 것은 `Bind`/`Clear` 두 개뿐 — 생명주기 실수의 여지가 구조적으로 없다.
- 구독 시작 시 현재 값이 즉시 방출된다. 모델이 아직 없으면 `Clear()`가 1회 호출되어
  UI가 초기화 상태로 시작한다.

### 타이밍 보장 (ReactiveProperty 의미론)

| 시나리오 | 동작 |
|---|---|
| 비활성 오브젝트에 Bind 후 SetActive | 활성화 → FeatureView.Awake → 구독 → **현재 모델 즉시 수신** |
| 런타임 AddComponent로 FeatureView 추가 | Awake에서 구독 → 현재 모델 즉시 수신 |
| Scroller 셀 재활용 (재Bind 반복) | `model.Value = next`만 발생, 스캔 비용 0 |
| 같은 모델 인스턴스 재Bind | 방출 없음 (스킵). 강제 갱신은 `Reload()` |

---

## ModelFeature와 With

`ModelFeature`는 모델에 부착되는 능력 단위입니다. `ViewModel.With<TFeature>(feature)`로 등록하고,
FeatureView가 `model.Feature<TFeature>()`로 조회합니다.

```csharp
var vm = new ViewModel()
    .With(new TextFeature("파이어볼"))
    .With(new ButtonFeature(allowHold: true))
    .With(new InteractableFeature());

sindy.Bind(vm);
vm.Feature<InteractableFeature>().Interactable.Value = false;
```

### 내장 Feature ↔ FeatureView 쌍

| Feature (모델 측) | FeatureView (뷰 측) | 제어 대상 |
|---|---|---|
| `TextFeature` | `TextFeatureView` | TMP_Text |
| `ImageFeature` | `ImageFeatureView` | Image.sprite |
| `GaugeFeature` | `GaugeFeatureView` | Image.fillAmount |
| `ButtonFeature(allowHold:)` | `ButtonFeatureView` | 포인터 클릭+홀드 (uGUI Button 불필요) |
| `ToggleFeature` | `ToggleFeatureView` | uGUI Toggle (양방향) |
| `ColorFeature` | `ColorFeatureView` | Graphic.color |
| `TabFeature` | `TabFeatureView` | Toggle 리스트 선택 (양방향) |
| `PageFeature` | `PageFeatureView` | GameObject 리스트 중 1개 활성 |
| `ListFeature` | `ListFeatureView` | 비가상화 아이템 리스트 |
| `VisibilityFeature` | `VisibilityFeatureView` | GameObject.SetActive |
| `LayoutFeature` | `LayoutFeatureView` | RectTransform 레이아웃 (LayoutGroup/LayoutElement) — 보통 ComponentBlueprint 체인으로 선언 (아래 "프리팹 조합" 참조) |
| `InteractableFeature` | `InteractableFeatureView` | CanvasGroup interactable/blocksRaycasts/alpha |
| `HighlightFeature` | `HighlightFeatureView` | 하이라이트 오브젝트 표시 |
| `RaycastBlockFeature` | `RaycastBlockFeatureView` | CanvasGroup.blocksRaycasts |
| `RedDotFeature` | `RedDotFeatureView` | 알림 뱃지 ([REDDOT.md](REDDOT.md)) |
| `ScrollerFeature` | `ScrollerFeatureView` | 가상화 스크롤 (아래 참조) |
| `ScreenFeature` | `ResponsiveLayoutFeatureView` | 반응형 레이아웃 (아래 참조) |

대부분의 출력형 Feature는 두 가지 생성자를 제공합니다:
단순 값(`new TextFeature("신디")`)과 **외부 모델 주입**(`new TextFeature(new TimerModel(60f))`).
`TimerModel`·`FormatNumberPropModel` 등 `PropModel<string>` 파생 자가 갱신 모델을 그대로 재사용할 수 있습니다.

### Models 팩토리

자주 쓰는 조합은 `Models` 정적 팩토리로 축약합니다.

```csharp
sindy.Bind(Models.Label("신디"));
sindy.Bind(Models.Button(allowHold: true));

var notice = Models.Notice("알림", "정말 삭제할까요?");   // 키: title/content/confirm/cancel
notice["confirm"].Feature<ButtonFeature>().OnClick.Subscribe(_ => Delete());
```

---

## UI 트리 구조 — ViewComponent

Feature는 "한 오브젝트의 능력" 축, ViewModel 자식은 "UI 트리 구조" 축. 공존한다.

루트에 `ViewComponent`를 두고 Inspector의 `views` 리스트에 (자식 허브, "키이름") 쌍을 등록하면,
모델의 `model["키"]` 자식이 각 허브에 자동 주입되고 `SetParent`로 연쇄 해제가 연결됩니다.

```csharp
var shop = new ViewModel();
shop["title"] = Models.Label("상점");
shop["gold"]  = Models.Label(new FormatNumberPropModel<long>(12345));
shop["buy"]   = new ViewModel().With(new TextFeature("구매")).With(new ButtonFeature());

shopView.Bind(shop);
```

- 키가 ViewModel에 없으면 `ViewComponent: Model for view 'xxx' not found` 경고가 출력됩니다.
- Inspector 각 항목 옆에 해당 오브젝트의 **FeatureView 목록**(예: `Text, Button`)이 회색 라벨로 표시됩니다.

### Composite와 SetParent

코드로 자식 허브를 직접 바인딩하는 경우 `SetParent(this)`를 호출해야
부모가 `Bind(null)` 또는 재바인딩/소멸될 때 자식도 연쇄적으로 해제됩니다.

```csharp
childHub.Bind(childModel).SetParent(parentHub);
```

---

## 프리팹 조합 — ComponentBlueprint & LayoutFeature

사전 제작된 **부품 프리팹**(라벨, 아이콘, 버튼, 카드 틀...)을 코드에서 조합해
원하는 UI를 만들어내는 시스템입니다. 전제: 부품 프리팹들이 `ComponentManager`의
프리팹 카탈로그(GameObjectCollection)에 등록되어 있어야 합니다.

**Blueprint는 설계도(데이터)입니다.** 선언 시점에는 아무것도 생성되지 않고,
`Open()` 시점에 루트 프리팹이 인스턴스화된 뒤 각 `Patch`가 가리키는 프리팹이
해당 경로의 자식으로 인스턴스화·부착·바인딩됩니다.

```csharp
// 재사용 템플릿 — 한 번 정의하고 상태 보존한 채 여러 번 Open
static readonly ComponentBlueprint Card = ComponentBlueprint
    .Create("card_frame")
        .Layout(Direction.Vertical, spacing: 4)
        .Padding(8)
    .Patch("icon", "icon_part")
    .Patch("title", "label_part");

// 조합 + 실행
ComponentBlueprint
    .Create("popup_frame").WithModel(() => BuildPopupModel())
    .Patch("header", Card).WithModel(() => BuildHeaderModel())
    .Patch("footer.confirm", "button_part").WithModel(() => Models.Button())
    .Open(layer: 1);
```

### 디자인/기능 분리 원칙

코드만으로 UI를 제어하면 기능 코드와 디자인 코드가 섞이기 쉽습니다.
이를 막는 것이 LayoutFeature와 Blueprint의 존재 이유입니다.

- **디자인**(배치 방향·간격·여백·크기)은 **Blueprint 체인**에 선언한다 — `Layout/Padding/Align/Size`.
- **기능**(모델 상태·구독·로직)은 모델 구성 함수에 둔다 — Feature 조합과 reactive 구독.

```csharp
// ✅ 디자인은 설계도에
ComponentBlueprint.Create("panel")
    .Patch("rows", "container_part").Layout(Direction.Vertical, spacing: 14).Padding(12)
    ...

// ❌ 안티패턴: 기능 코드(BuildModel) 안에 디자인 선언 — 두 관심사가 다시 섞인다.
//    모델에 이미 LayoutFeature가 있으면 Blueprint 적용 시 Dev 빌드 경고가 출력된다.
vm["rows"] = new ViewModel().With(new LayoutFeature().Layout(Direction.Vertical, 14));
```

디자인 값의 거처 기준: **구조적 플로우**(행/열 배치, 간격)는 Blueprint(코드)에,
**화면 변형별 절대 좌표·앵커**는 뷰의 `ResponsiveLayoutFeatureView`(씬 직렬화)에 둡니다.
모델은 variant 키만 압니다 (반응형 레이아웃 섹션 참조).

### 조립 규칙 (Open)

1. **하이브리드** — 패치 경로의 키가 루트 프리팹(ViewComponent)에 이미 있으면
   인스턴스화를 생략하고 모델만 주입한다. 틀은 프리팹에, 가변 부품은 코드에.
2. **자동 컨테이너** — 중간 경로(`"footer.confirm"`의 `footer`)가 없으면
   RectTransform+ViewComponent 빈 컨테이너가 자동 생성된다.
3. **재정의 승계** — 같은 경로를 다시 패치하면 마지막 선언이 우선하되, 지정하지 않은
   모델 팩토리/레이아웃은 이전 선언에서 승계한다. 재정의된 base 팩토리는 실행되지 않는다.
4. **형제 순서** = 같은 깊이에서의 패치 선언 순서 (프리팹 기존 자식이 먼저).
5. **패치 프리팹 미등록** = 즉시 예외. 설계도 오류는 빨리 실패한다.

파생 설계도는 `Create(템플릿)`으로 만들고 일부 경로만 재정의합니다.
`Patch(path, blueprint)`로 Blueprint를 중첩하면 하위 패치가 경로 접두어와 함께 전개되고,
해당 Blueprint의 루트 레이아웃이 패치 노드의 레이아웃이 됩니다.

### LayoutFeature 의미론

`Apply`는 **full-spec**입니다: Feature의 전체 상태를 대상에 반영하고, 지정하지 않은
속성(padding, alignment, size)은 기본값으로 리셋합니다. 방향 전환 시 기존 반대 방향
LayoutGroup은 제거됩니다. 따라서 셀 풀링·재바인딩에서 이전 모델의 디자인이 잔존하지 않습니다.
모델 해제 시 `LayoutFeatureView.Clear`가 레이아웃 영향을 비활성화합니다(파괴 아님 — 풀링 성능).

`Margin`은 제공하지 않습니다. uGUI에서 offset 기반 margin은 부모 LayoutGroup이 즉시
덮어써 무효이므로, 간격은 부모의 `Padding`/`Layout(spacing:)`으로 해결합니다.

### Dev 빌드 경고 (릴리스 비용 0)

| 상황 | 경고 |
|---|---|
| 패치 모델이 ViewModel이 아닌데 Layout/Size 지정 | 레이아웃 무시 경고 |
| 루트 모델이 ViewModel이 아닌데 패치 존재 | 패치 무시 경고 |
| 모델에 이미 LayoutFeature가 있는데 Blueprint 레이아웃 지정 | 덮어쓰기 경고 (분리 원칙 위반 가시화) |

---

## 생명주기와 정리(cleanup)

```
hub.Bind(newModel)
  ├── 같은 인스턴스면 조기 반환 (same-instance 스킵)
  ├── LinkState 연쇄: 자식 허브들 Bind(null), 부모로부터 분리
  ├── (Editor/Dev) FeatureView ↔ Feature 매칭 검증
  └── model.Value = newModel  → 각 FeatureView: 이전 구독 해제 → Bind(feature) 또는 Clear()

hub.OnDestroy()
  └── 자식 연쇄 해제 → 스트림으로 null 전파 → 스트림 Dispose
```

외부 코드에서 정리할 때의 순서:

```csharp
hub.Bind(null);    // 1. FeatureView 구독 해제
model.Dispose();   // 2. 모델 내부 구독 해제 (EveryUpdate, CombineLatest 등)
```

순서가 중요합니다. 역순이면 이미 Disposed된 Observable에 값이 흐를 수 있습니다.
허브가 파괴되면 1번은 자동 수행되지만, 모델 Dispose는 모델 소유자(Controller)의 책임입니다.

---

## 검증 (Editor/Dev 빌드 한정, 릴리스 비용 0)

릴리스 빌드의 허브는 FeatureView 명단을 모르므로(순수 pub/sub), 검증은 Bind 타임 일회 스캔으로 수행:

```
[SindyComponent] 모델의 GaugeFeature에 매칭되는 FeatureView가 없습니다. (SkillSlot)
[SindyComponent] TextFeatureView가 있으나 모델에 TextFeature가 없습니다. (SkillSlot)
```

FeatureView가 하나도 없는 허브(ViewComponent 트리 노드 등)는 검증 대상에서 제외됩니다.
SindyComponent Inspector 하단에는 부착된 FeatureView 목록이 표시되고,
플레이 중에는 바인딩된 모델과의 매칭 상태(✓/✗)가 함께 표시됩니다.

---

## ScrollerFeatureView (가상화 스크롤)

`ScrollerFeature`(섹션 데이터) ↔ `ScrollerFeatureView`(가상화 엔진) 쌍입니다.
뷰포트에 보이는 셀만 인스턴스화하며, 다수 섹션 적층과 그리드 레이아웃을 지원합니다.

### 셀 키 + 카탈로그

prefab은 VM 타입 키 대신 **명시적 셀 키(문자열)**로 해상합니다. 해상 우선순위:

1. `Section`의 명시 prefab (`ContentPrefab` 등)
2. `SectionOption`의 prefab 오버라이드 (보조)
3. 셀 키 → 인스턴스 등록(`RegisterCell`) → `CellCatalog` 에셋 → 전역 등록(`RegisterGlobalCell`)
4. 미등록 → Bind 시점 throw (누락 키를 메시지에 명시, atomic)

```csharp
// 키 선언 — 오타 방지 const 모음
public static class CellKeys
{
    public const string Title = "title";
    public const string Item  = "shop.item";
}

// 공용 셀: 전역 등록(또는 CellCatalog 에셋) → 모든 스크롤러에서 키로 참조
ScrollerFeatureView.RegisterGlobalCell(CellKeys.Title, titlePrefab);

// 셀 모델은 순수 ViewModel + Feature (전용 클래스 불필요, 팩토리로 충분)
static ViewModel ItemCell(ItemData d) => new ViewModel()
    .With(new ImageFeature(d.Icon))
    .With(new TextFeature(d.Name))
    .With(new ButtonFeature());

// 섹션 — 공용 셀은 키 참조, 일회성 셀은 prefab 직접 지정
var section = new Section(itemList, option)        // ObservableList<IViewModel>
{
    ContentKey = CellKeys.Item,
    Header = titleVM,
    HeaderKey = CellKeys.Title,
};
var oneOff = new Section(eventList, option)
{
    ContentPrefab = eventBannerPrefab,             // 키 등록 없이 명시 지정
};

scroller.Bind(new ViewModel().With(new ScrollerFeature(new[] { section, oneOff })));
```

`CellCatalog`는 ScriptableObject 에셋(`Sindy/Scroller/Cell Catalog`)으로,
등록 타이밍 제약이 없고 디자이너가 Inspector에서 매핑을 확인/수정할 수 있습니다.

### 주의사항

- 코드 등록(`RegisterCell`/`RegisterGlobalCell`)은 `Bind()` **이전**에 호출해야 합니다 (FR-CELL-07).
  CellCatalog 에셋은 이 제약이 없습니다.
- 섹션 갱신은 새 `ScrollerFeature`를 가진 모델로 재Bind하거나, 같은 모델이면 허브의 `Reload()`를 호출합니다.
- `RegisterCellType<TVM>` / `Section<TVM>` / `SetSections` 등 타입 키 API는 제거되었습니다.

---

## 반응형 레이아웃 (ScreenFeature ↔ ResponsiveLayoutFeatureView)

모바일의 다양한 해상도·회전에 대응한다. **모델은 "지금 어떤 변형(variant)인가"라는
의미적 상태만 알고, 실제 앵커·오프셋 좌표는 뷰의 직렬화 데이터에 둔다** (MVVM 경계 유지).

```csharp
// 모델 측 — 기본 selector는 가로/세로 2종 ("landscape"/"portrait")
vm.With(new ScreenFeature());

// 3종 이상이 필요하면 selector 주입
vm.With(new ScreenFeature(s => s.Aspect >= 2f ? "wide" : s.IsLandscape ? "landscape" : "portrait"));
```

뷰 측은 루트 허브에 `ResponsiveLayoutFeatureView`를 부착하고 variant별로
대상 RectTransform들의 RectState(앵커·오프셋·피벗)를 등록한다.
코드 구성 시 `RectState.From(rect)`으로 현재 배치를 캡처할 수 있다.

- `ScreenStateModel`은 `PropModel<ScreenState>` 파생 자가 갱신 모델(TimerModel 패턴) —
  EveryUpdate 폴링하되 값이 바뀔 때만 방출. 테스트에서는 외부 주입으로 회전을 시뮬레이션한다.
- 노치·홈바 보정은 `SafeAreaView`(모델 불필요, 순수 뷰 유틸)를 전체 화면 루트에 부착.
- 변형 전환 후 ScrollRect 등 크기에 민감한 컴포넌트는 소비자가 재계산을 트리거한다
  (예: 스크롤러 허브 `Reload()` — 구독 순서상 레이아웃 적용 이후에 실행되도록 Bind 후 구독).

---

## 커스텀 Feature/FeatureView 만들기 — 점멸(Blink) 예제

작성할 것은 Feature/FeatureView 한 쌍. SindyComponent 수정 없음(Open/Closed).

```csharp
// 1) 모델 측 — 순수 데이터 (Unity API 없음 → 단위 테스트 가능)
public class BlinkFeature : ModelFeature
{
    public PropModel<bool> Blinking { get; }
    public BlinkFeature(bool initial = false)
    {
        Blinking = new PropModel<bool>(initial);
        Blinking.AddTo(this);
    }
}

// 2) 뷰 측 — Bind/Clear만 구현하면 생명주기는 베이스가 처리
public class BlinkFeatureView : FeatureView<BlinkFeature>
{
    [SerializeField] private CanvasGroup target;
    [SerializeField] private float interval = 0.3f;

    protected override void Bind(BlinkFeature feature, ICollection<IDisposable> disposables)
    {
        feature.Blinking
            .Subscribe(on => { if (on) StartBlink(); else StopBlink(); })
            .AddTo(disposables);
    }

    protected override void Clear() => StopBlink();
}
```

**체크리스트 (새 Feature 쌍 추가 시):**

1. `Features/XxxFeature.cs` — `ModelFeature` 상속, 상태는 `PropModel<T>`, 이벤트는 `Subject<T>`
2. 내부 모델은 생성자에서 `AddTo(this)` (Feature와 함께 Dispose)
3. 가능하면 외부 모델 주입 생성자도 제공 (`XxxFeature(PropModel<T> external)`)
4. `FeatureViews/XxxFeatureView.cs` — `Bind`의 모든 구독에 `.AddTo(disposables)`
5. `[AddComponentMenu("Sindy/Feature Views/...")]` 선언
6. 테스트: `hub.Bind(null); model.Dispose();` 후 값 변경해도 예외/갱신 없는지 확인
