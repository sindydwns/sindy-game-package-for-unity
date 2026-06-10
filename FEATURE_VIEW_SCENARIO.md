# [설계 확정안] FeatureView 아키텍처 — 사용 시나리오

> **상태: 설계 확정 (2026-06-10 논의 수렴, 잔여 질문 3건 포함 전부 확정). 구현 전.**
> 핵심: 타입별 컴포넌트(TextComponent, ButtonComponent...)를 없애고,
> **SindyComponent(허브) + FeatureView(능력 단위)** 조합으로 모든 UI를 구성한다.

---

## 확정 설계 4원칙

1. **1:1 대칭** — `ModelFeature`(모델) ↔ `FeatureView`(뷰)는 항상 1:1. N:1은 채택하지 않음.
2. **Feature 경계 기준** — 같은 저수준 입력 자원(포인터 제스처 공간 등)을 공유하는 동작은 **한 Feature의 내부 옵션**으로 통합한다(예: 클릭/홀드 → `ButtonFeature(allowHold:)`). 독립 동작 가능한 능력은 별도 Feature로 분리한다(예: Interactable, Visibility, Highlight).
3. **역방향 참조** — FeatureView가 `[RequireComponent(typeof(SindyComponent))]`로 허브를 보장하고, 스스로 허브를 찾아 구독한다. 허브는 FeatureView의 존재를 모른다(릴리스 빌드 기준).
4. **ReactiveProperty 기반 모델 전파** — SindyComponent는 `IViewModel`이 아닌 `ReactiveProperty<IViewModel>`을 보유. FeatureView는 이 스트림을 구독한다. R3의 "구독 즉시 현재 값 방출" 의미론이 비활성 Bind·늦은 Awake·런타임 AddComponent 타이밍 문제를 전부 해결한다.

---

## 0. 개념 한 장 요약

```
[모델 측]                            [뷰 측 - 한 GameObject]
ViewModel                            SindyComponent              ← 허브: ReactiveProperty<IViewModel>
 ├─ TextFeature("공격")        ◀──▶   TextFeatureView            ← TMP_Text 출력
 ├─ ButtonFeature(allowHold)   ◀──▶   ButtonFeatureView          ← 클릭+홀드 입력 (포인터 이벤트 단일 소유)
 ├─ GaugeFeature(0.7f)         ◀──▶   GaugeFeatureView           ← Image fillAmount 출력
 └─ InteractableFeature        ◀──▶   InteractableFeatureView    ← CanvasGroup 제어
```

- "이 오브젝트가 뭘 할 수 있는가" = Inspector의 FeatureView 목록 그 자체 (1:1이므로 자명)
- 모델 교체/해제는 `ReactiveProperty` 스트림으로 모든 FeatureView에 자동 전파

---

## 1. 핵심 메커니즘

### SindyComponent (허브)

```csharp
public class SindyComponent : MonoBehaviour
{
    private readonly ReactiveProperty<IViewModel> model = new();
    public ReadOnlyReactiveProperty<IViewModel> Model => model;   // 읽기 전용 노출

    public SindyComponent Bind(IViewModel newModel)
    {
        // 검증·LinkState 처리 후
        model.Value = newModel;        // 같은 인스턴스면 방출 없음 = same-instance 스킵 공짜
        return this;
    }

    public void Reload() => model.ForceNotify();   // 의도적 재초기화 (기존 Reload 대체)
}
```

- 필드 초기화이므로 **허브의 Awake 실행 여부와 무관하게** 비활성 상태에서도 Bind 가능
- `Bind(null)` → 스트림으로 null 전파 → 각 FeatureView가 스스로 정리
- LinkState(부모-자식 연쇄 해제), ViewComponent 키 매핑은 기존 구조 유지

### FeatureView 베이스 — dispose-then-bind를 구조로 강제

```csharp
[RequireComponent(typeof(SindyComponent))]
public abstract class FeatureView<TFeature> : MonoBehaviour where TFeature : ModelFeature
{
    private readonly List<IDisposable> disposables = new();

    protected virtual void Awake()
    {
        GetComponent<SindyComponent>().Model
            .Subscribe(OnModelChanged)
            .AddTo(this);                          // 파괴 시 자동 해제
    }

    private void OnModelChanged(IViewModel model)
    {
        disposables.DisposeAllClear();             // 항상 해제 먼저 (구현자가 틀릴 수 없음)
        var feature = model?.Feature<TFeature>();
        if (feature != null) Bind(feature, disposables);
        else Clear();
    }

    protected abstract void Bind(TFeature feature, ICollection<IDisposable> disposables);
    protected virtual void Clear() { }
}
```

구현자가 작성하는 것은 `Bind`/`Clear` 두 개뿐. 생명주기 실수의 여지가 구조적으로 없다.

### 타이밍 보장 (ReactiveProperty 의미론)

| 시나리오 | 동작 |
|---|---|
| 비활성 오브젝트에 Bind 후 SetActive | 활성화 → FeatureView.Awake → 구독 → **현재 모델 즉시 수신** |
| 런타임 AddComponent로 FeatureView 추가 | Awake에서 구독 → 현재 모델 즉시 수신 |
| Scroller 셀 재활용 (재Bind 반복) | `model.Value = next` 만 발생, 스캔 비용 0 |
| 같은 모델 인스턴스 재Bind | 방출 없음 (스킵). 강제 갱신은 `Reload()` |

---

## Step 1. 텍스트 라벨 — 최소 구성

**Unity 에디터 작업:**

1. GameObject 생성 → `TextMeshPro - Text (UI)` 추가
2. **Add Component → Sindy → Feature Views → Text Feature View**
   - `[RequireComponent]`로 **SindyComponent가 자동 추가됨** (별도 작업 불필요)
   - `label` 필드는 `Reset()`에서 같은 오브젝트의 TMP_Text 자동 탐지

**코드:**

```csharp
var nameModel = new ViewModel().With(new TextFeature("신디"));
sindy.Bind(nameModel);

// 값 변경 → UI 자동 반영
nameModel.Feature<TextFeature>().Text.Value = "Citrine";
```

> 팩토리로 축약: `sindy.Bind(Models.Label("신디"));`

**TextFeature 내부 [확정]:** 텍스트 상태는 `ReactiveProperty<string>` 직접 노출이 아닌 **`PropModel<string>` 보유**.
기존 Feature들(`VisibilityFeature.Show`, `InteractableFeature.Interactable`)이 모두 PropModel을 쓰는 것과 일관되고,
`TimerModel`·`FormatNumberPropModel` 등 PropModel<string> 파생 모델을 그대로 주입할 수 있다:

```csharp
public class TextFeature : ModelFeature
{
    public PropModel<string> Text { get; }
    public TextFeature(string text) { ... }              // 단순 값
    public TextFeature(PropModel<string> external) { ... }  // TimerModel 등 자가 갱신 모델 주입
}

// 카운트다운 라벨 — 수동 배선·구독 관리 없이 동작
sindy.Bind(new ViewModel().With(new TextFeature(new TimerModel(60f))));
```

---

## Step 2. 클릭 버튼

**Unity 에디터 작업:**

1. GameObject에 `Image` 추가 (레이캐스트 타겟)
2. **Add Component → Sindy → Feature Views → Button Feature View**
   - SindyComponent 자동 추가. uGUI `Button`/`HoldButton` 불필요 — ButtonFeatureView가 `IPointerDownHandler` 등을 직접 구현
   - Inspector: `holdTime: 0.5`, `repeatInterval: 0.05` (홀드 사용 시에만 의미)

**코드:**

```csharp
var attackModel = new ViewModel().With(new ButtonFeature());   // allowHold 기본 false
sindy.Bind(attackModel);

attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Attack());

// 테스트: 코드로 클릭 시뮬레이션
attackModel.Feature<ButtonFeature>().OnClick.OnNext(Unit.Default);
```

---

## Step 3. 버튼에 홀드 추가 — 에디터 작업 0

클릭과 홀드는 같은 포인터 제스처 공간을 공유하므로 별도 Feature가 아니라 **ButtonFeature의 옵션**이다(설계 원칙 2). 따라서 홀드 업그레이드는 모델 한 줄:

```csharp
var attackModel = new ViewModel()
    .With(new ButtonFeature(allowHold: true));    // 이게 전부

sindy.Bind(attackModel);

attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Attack());
attackModel.Feature<ButtonFeature>().OnHold.Subscribe(repeat => AttackRepeat(repeat));

// 런타임 토글도 가능
attackModel.Feature<ButtonFeature>().AllowHold.Value = false;
```

```csharp
public class ButtonFeature : ModelFeature
{
    public Subject<Unit> OnClick { get; }
    public Subject<int> OnHold { get; }           // repeat 횟수 전달
    public PropModel<bool> AllowHold { get; }     // 런타임 제어 (기존 HoldFeature.AllowHold 계승)

    public ButtonFeature(bool allowHold = false) { ... }
}
```

**기존 구조와 비교:**

| | 기존 | 확정안 |
|---|---|---|
| 홀드 감지 | `HoldButton`(Button 상속) — ButtonComponent 전용 | `ButtonFeatureView` 내부 — 포인터 이벤트 단일 소유로 클릭/홀드 판별 충돌 없음 |
| 홀드 부여 비용 | HoldButton 세팅 + `.With(new HoldFeature())` | `allowHold: true` 인자 하나 |
| "홀드 가능한가?" | `[SupportedFeature]` 어트리뷰트 확인 | ButtonFeature 생성 인자 = 모델이 곧 답 |

---

## Step 4. 조합 폭발 해소 — 스킬 버튼

아이콘 + 스킬명 + 클릭 + 쿨다운 게이지 + 쿨다운 중 비활성화. **작성 클래스 0개.**

**Unity 에디터 작업 (한 GameObject):**

| Add Component | 연결 대상 |
|---|---|
| ImageFeatureView | 아이콘 Image |
| TextFeatureView | 스킬명 TMP_Text (자식 참조 가능) |
| ButtonFeatureView | — |
| GaugeFeatureView | 쿨다운 fill Image |
| InteractableFeatureView | — (CanvasGroup 자동) |

(SindyComponent는 첫 FeatureView 추가 시 자동 부착)

**코드:**

```csharp
var skill = new ViewModel()
    .With(new ImageFeature(fireballSprite))
    .With(new TextFeature("파이어볼"))
    .With(new ButtonFeature())
    .With(new GaugeFeature(0f))
    .With(new InteractableFeature());

sindy.Bind(skill);

skill.Feature<ButtonFeature>().OnClick.Subscribe(_ =>
{
    CastFireball();
    skill.Feature<InteractableFeature>().Interactable.Value = false;
    StartCooldown(3f,
        ratio => skill.Feature<GaugeFeature>().Ratio.Value = ratio,
        onDone: () => skill.Feature<InteractableFeature>().Interactable.Value = true);
});
```

---

## Step 5. UI 트리 구조 — ViewComponent는 그대로

Feature는 "한 오브젝트의 능력" 축, ViewModel 자식은 "UI 트리 구조" 축. 공존한다.

**Unity 에디터 작업:** 팝업 루트에 `ViewComponent` → `views` 리스트에 자식들의 SindyComponent를 키와 함께 등록 (기존과 동일).

```csharp
var shop = new ViewModel();
shop["title"] = Models.Label("상점");
shop["gold"]  = Models.Label("12,345");
shop["buy"]   = new ViewModel()
    .With(new TextFeature("구매"))
    .With(new ButtonFeature())
    .With(new InteractableFeature());

shopView.Bind(shop);   // 키 매핑 → 각 허브의 ReactiveProperty에 모델 주입 → FeatureView들이 수신
```

> ViewBehaviourDrawer는 "모델 타입" 대신 **해당 오브젝트의 FeatureView 목록**(예: `Text, Button, Interactable`)을 회색 라벨로 표시.

---

## Step 6. 검증

릴리스 빌드의 허브는 FeatureView 명단을 모르므로(순수 pub/sub), 검증은 Editor/Dev 빌드 한정 일회 스캔으로 수행:

**(1) Bind 타임 — Editor/Dev 빌드에서만 `GetComponents<IFeatureView>()` 스캔:**

```
[SindyComponent] 모델의 GaugeFeature에 매칭되는 FeatureView가 없습니다. (SkillSlot)
[SindyComponent] TextFeatureView가 있으나 모델에 TextFeature가 없습니다. (SkillSlot)
```

릴리스 빌드에는 비용 0.

**(2) 에디터 타임 — SindyComponent Inspector:**

```
SindyComponent
├─ 부착된 FeatureView: Text, Button, Interactable
└─ (플레이 중 Bind된 모델) 매칭: ✓ Text  ✓ Button  ✗ Gauge(View 없음)
```

**(3) 테스트 타임:**

```csharp
SindyAssert.FeaturesMatch(prefab, Models.SkillButton());   // 프리팹 ↔ 모델 팩토리 계약 검증
```

---

## Step 7. 커스텀 Feature/FeatureView 만들기 — 점멸(Blink) 예제

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

**사용:** 아무 오브젝트에 `BlinkFeatureView` 부착 + 모델에 `.With(new BlinkFeature())`.

---

## Step 8. Scroller 통합 — 셀 키 + 카탈로그 [확정]

가상화 엔진(풀·레이아웃·LateUpdate 패스)은 이미 비제네릭 `SindyComponent` 기준으로 동작하므로 무사하다.
충돌 지점은 하나 — **VM 타입을 키로 쓰는 prefab 해상**. 셀 모델이 전부 `ViewModel` + Feature가 되면 타입 키가 붕괴한다.
해법: **3단계 해상 구조는 유지하고 키만 타입 → 명시적 셀 키(문자열 상수)로 교체.**

### 해상 우선순위 (기존 구조와 1:1 대응)

| 현재 (타입 키) | 확정안 (셀 키) |
|---|---|
| 1) 섹션 명시 prefab | 1) 섹션 명시 prefab (동일) |
| 2) 인스턴스 레지스트리 `RegisterCellType<TVM>` | 2) 인스턴스 카탈로그 `RegisterCell(key, prefab)` |
| 3) 전역 레지스트리 `RegisterGlobalCellType<TVM>` | 3) 전역 카탈로그 `RegisterGlobalCell(key, prefab)` / CellCatalog 에셋 |
| 4) 미등록 throw | 4) 미등록 throw (Init 시점 일괄 검증, atomic 유지) |

### 사용 예

```csharp
// 키 선언 — 오타 방지 const 모음
public static class CellKeys
{
    public const string Title  = "title";
    public const string Filter = "filter";
    public const string Item   = "shop.item";
}

// 공용 셀: 전역 카탈로그에 부트스트랩 1회 등록 → 모든 스크롤러에서 키로 참조
ScrollerComponent.RegisterGlobalCell(CellKeys.Title, titlePrefab);

// 셀 모델은 순수 ViewModel + Feature (클래스 불필요, 팩토리로 충분)
static ViewModel ItemCell(ItemData d) => new ViewModel()
    .With(new ImageFeature(d.Icon))
    .With(new TextFeature(d.Name))
    .With(new ButtonFeature());

// 섹션 — 공용 셀은 키 참조, 일회성 셀은 prefab 직접 지정
var section = new Section(itemList, option)        // Section 비제네릭화
{
    ContentKey = CellKeys.Item,
    Header = titleVM,
    HeaderKey = CellKeys.Title,
};
var oneOff = new Section(eventList, option)
{
    ContentPrefab = eventBannerPrefab,             // 키 등록 없이 명시 지정
};

scroller.Bind(new ScrollerViewModel(new[] { section, oneOff }));
```

### 수반 변경

- `CellTypeRegistry`의 타입 키 → 문자열 키. `RegisterCellType<TVM>` / `RegisterGlobalCellType<TVM>` / `PrewarmPool<TVM>` 폐기 (`PrewarmPool(prefab, count)`는 유지)
- `Section<TVM>` 비제네릭화 — 제네릭의 존재 이유(`ContentVMType` 레지스트리 조회)가 소멸
- `SetModel` 오버라이드의 same-instance 가드 우회 해킹 삭제 — ReactiveProperty 의미론 + `Reload()`(ForceNotify)로 대체
- `Init`/`Clear` 훅 → `ScrollerFeature`(섹션 데이터) + `ScrollerFeatureView`(가상화 엔진) 쌍으로 이식
- 전역 카탈로그는 정적 맵 대신 **CellCatalog ScriptableObject 에셋** 권장: 등록 타이밍 제약(FR-CELL-07) 소멸, 디자이너가 Inspector에서 매핑 확인/수정 가능, 정적 가변 상태(도메인 리로드 이슈) 제거. 코드 등록 API와 병행 가능
- prefab은 `Section`(코드 객체) 프로퍼티에 우선 두고 `SectionOption`(SO) 오버라이드는 보조 — "레이아웃은 같고 prefab만 다른" 경우의 에셋 복제 결합 방지

### 트레이드오프와 보완

문자열 키는 타입 키의 컴파일 타임 안전을 잃는다. 보완 3중:
`CellKeys` const 클래스 관례 + Init 시점 미등록 키 throw(누락 키를 메시지에 명시) + Editor에서 CellCatalog 에셋 검사.

---

## 마이그레이션 대응표

| 기존 | 확정안 |
|---|---|
| `IViewModel Model` 필드 + `Init`/`Clear` 훅 | `ReactiveProperty<IViewModel>` + FeatureView 구독 |
| `Reload()` (ClearModel 후 재Init) | `model.ForceNotify()` |
| `TextComponent` + `PropModel<string>` | `TextFeatureView` + `TextFeature` |
| `ButtonComponent` + `ButtonModel` + `HoldFeature` + `HoldButton` | `ButtonFeatureView` + `ButtonFeature(allowHold:)` |
| `GaugeComponent` + `GaugeModel` | `GaugeFeatureView` + `GaugeFeature` |
| `ImageComponent` | `ImageFeatureView` + `ImageFeature` |
| `[SupportedFeature(...)]` + ValidateSupportedFeatures | FeatureView 부착 = 구조적 선언, Dev 빌드 일회 스캔 검증 |
| `BindCommonFeatures` (Visibility/Layout 하드코딩) | VisibilityFeatureView / LayoutFeatureView로 일반화 |
| 허브 disposables 단일 리스트 | FeatureView 각자 disposables 소유 |
| 복합 컴포넌트 (NoticeComponent 등) | 단순 조합 → FeatureView 조합, 트리 구조 → ViewComponent 유지 |

---

## 설계 결정 기록 (Decision Log)

| 결정 | 기각된 대안 | 이유 |
|---|---|---|
| 1:1 대칭 | N:1 (한 View가 여러 Feature 커버) | N:1은 지원 선언·병합 기준·중복 클레임 정책이 추가로 필요. 클릭/홀드 충돌은 Feature 설계(ButtonFeature 옵션화)로 해결하는 것이 근본적 |
| ButtonFeature에 홀드 옵션 내장 | ClickFeature + HoldFeature 분리 | 같은 포인터 제스처 공간을 공유 → 별개 능력이 아니라 한 능력의 동작 모드. 디자이너의 사고 단위도 "버튼" |
| FeatureView → 허브 역방향 참조 + RequireComponent | 허브가 GetComponents로 View 스캔 + 캐싱 | 허브 부재가 저작 시점에 차단되고, 런타임 AddComponent도 자기등록으로 자연 처리. 캐시 무효화 문제 소멸 |
| `ReactiveProperty<IViewModel>` 보유 | `IViewModel` 필드 + "늦은 등록=즉시 바인딩" 수동 구현 | R3 구독 의미론이 타이밍 문제(비활성 Bind, 늦은 Awake)를 플랫폼 차원에서 해결. 특수 케이스 코드 불필요 |
| dispose-then-bind를 베이스 클래스에 고정 | 구현자 재량 | 모델 교체 시 구독 누수는 가장 흔한 실수 — 구조적으로 차단 |
| 검증은 Dev 빌드 일회 스캔 | 허브가 View 명단 상시 보유 | 릴리스 빌드 비용 0, 순수 pub/sub 유지 |

---

## 남은 질문 (구현 시 확정)

1. **TextFeature 내용물** — `PropModel<string>` 보유 vs `ReactiveProperty<string>` 직접 노출. 기존 TimerModel, FormatNumberPropModel 등 파생 모델의 이식 경로에 영향.
2. **SindyComponent\<T\> 폐기 범위** — 완전 폐기 vs ScrollerComponent/ViewComponent처럼 구조적 역할(트리·가상화)이 있는 것은 제네릭 유지.
3. **네이밍 확정** — FeatureView / FeatureBinder / Ability 등. SindyComponent 자동 부착 시 메뉴 구조(Add Component → Sindy → ...)도 함께.
