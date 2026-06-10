# SindyComponent & FeatureView 스텝 바이 스텝 튜토리얼

> 한 달 만에 돌아온 나(또는 AI)를 위한 실습 가이드. 모든 예제는 현재 패키지 코드 기준으로 검증된 실제 API만 사용합니다.
> 개념 레퍼런스는 [SINDY_COMPONENT.md](SINDY_COMPONENT.md) 참고.

---

## 0. 30초 요약

```
ViewModel + Feature  ──Bind()──▶  SindyComponent(허브)
       ▲                              │ ReactiveProperty<IViewModel> 스트림
       │ 값 변경 (Controller가 함)      ▼
       │                          FeatureView들  ← 능력 단위로 구독 등록 → UI 자동 갱신
```

- **전용 컴포넌트/모델 클래스를 만들지 않는다.** 모델은 `ViewModel + Feature 조합`, 뷰는 `FeatureView 부착`.
- **Controller는 Feature만 만진다.** `text = ...`, `SetActive(...)` 같은 View 직접 호출은 FeatureView의 `Bind()` 안에서만.
- `Bind(model)` 한 번이면 끝. 구독 해제는 `Bind(null)` 또는 컴포넌트 파괴 시 자동.
- 네임스페이스: `Sindy.View`, `Sindy.View.Features`, `Sindy.View.FeatureViews` + `R3`

---

## Step 1. 텍스트 라벨 — 최소 구성

**상황:** 플레이어 이름을 화면에 표시한다.

**씬 구성:**

1. GameObject 생성 → `TextMeshPro - Text (UI)` 추가
2. **Add Component → Sindy → Feature Views → Text Feature View**
   - `[RequireComponent]`로 **SindyComponent(허브)가 자동 추가됨**
   - `label` 필드는 `Reset()`에서 TMP_Text 자동 탐지

```csharp
using Sindy.View;
using Sindy.View.Features;

public class PlayerHud : MonoBehaviour
{
    [SerializeField] private SindyComponent nameHub;

    private ViewModel nameModel;

    void Start()
    {
        nameModel = Models.Label("신디");
        nameHub.Bind(nameModel);                              // 구독 시작, 즉시 "신디" 표시

        nameModel.Feature<TextFeature>().Text.Value = "Citrine";  // 이 한 줄로 UI 자동 갱신
    }

    void OnDestroy()
    {
        nameHub.Bind(null);     // 1. FeatureView 구독 해제
        nameModel.Dispose();    // 2. 모델 정리
    }
}
```

**핵심:** `Models.Label("신디")` = `new ViewModel().With(new TextFeature("신디"))`의 축약.
Feature의 `PropModel` 값 변경 → 구독자에게 자동 전파.

---

## Step 2. 사용자 입력 받기 — 버튼

**상황:** 공격 버튼 클릭을 게임 로직에 전달한다.

**씬 구성:** Image(레이캐스트 타겟)가 있는 GameObject에 **Button Feature View** 추가.
uGUI `Button`은 필요 없다 — ButtonFeatureView가 포인터 이벤트를 직접 구현한다.

```csharp
using R3;
using Sindy.View.Features;

[SerializeField] private SindyComponent attackHub;
private ViewModel attackModel;

void Start()
{
    attackModel = Models.Button();
    attackHub.Bind(attackModel);

    attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Attack());
}
```

**핵심:** View→로직 방향 흐름도 모델(Feature의 `Subject`)을 통한다.
코드로 클릭을 시뮬레이션할 수 있어 테스트가 쉬움: `attackModel.Feature<ButtonFeature>().OnClick.OnNext(Unit.Default)`

---

## Step 3. 버튼에 홀드 추가 — 에디터 작업 0

클릭과 홀드는 같은 포인터 제스처 공간을 공유하므로 별도 Feature가 아니라 **ButtonFeature의 옵션**이다. 홀드 업그레이드는 모델 한 줄:

```csharp
var attackModel = Models.Button(allowHold: true);    // 이게 전부
attackHub.Bind(attackModel);

attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Attack());
attackModel.Feature<ButtonFeature>().OnHold.Subscribe(repeat => AttackRepeat(repeat));

// 런타임 토글도 가능
attackModel.Feature<ButtonFeature>().AllowHold.Value = false;
```

- 홀드 시간/반복 주기는 ButtonFeatureView Inspector의 `holdTime`/`repeatInterval`
- "이 버튼이 홀드 가능한가?"의 답은 모델 생성 인자가 곧 답이다
- 홀드가 발생한 프레스의 릴리스에서는 클릭이 발행되지 않는다

---

## Step 4. 조합 폭발 해소 — 스킬 버튼

아이콘 + 스킬명 + 클릭 + 쿨다운 게이지 + 쿨다운 중 비활성화. **작성 클래스 0개.**

**씬 구성 (한 GameObject):**

| Add Component | 연결 대상 |
|---|---|
| ImageFeatureView | 아이콘 Image |
| TextFeatureView | 스킬명 TMP_Text (자식 참조 가능) |
| ButtonFeatureView | — |
| GaugeFeatureView | 쿨다운 fill Image |
| InteractableFeatureView | — (CanvasGroup 자동) |

(SindyComponent는 첫 FeatureView 추가 시 자동 부착)

```csharp
var skill = new ViewModel()
    .With(new ImageFeature(fireballSprite))
    .With(new TextFeature("파이어볼"))
    .With(new ButtonFeature())
    .With(new GaugeFeature(0f))
    .With(new InteractableFeature());

skillHub.Bind(skill);

skill.Feature<ButtonFeature>().OnClick.Subscribe(_ =>
{
    CastFireball();
    skill.Feature<InteractableFeature>().Interactable.Value = false;
    StartCooldown(3f,
        ratio => skill.Feature<GaugeFeature>().Ratio.Value = ratio,
        onDone: () => skill.Feature<InteractableFeature>().Interactable.Value = true);
});
```

미스매치(모델에 Feature가 있는데 View가 없거나 그 반대)는 Editor/Dev 빌드에서 Bind 시점에 경고 로그로 알려준다.

---

## Step 5. ViewComponent — 코드 없이 키로 매핑

**상황:** 자식이 많은 UI 루트. 자식 바인딩 코드를 일일이 쓰기 싫을 때.

**씬 구성:** 루트에 `ViewComponent` 추가 → Inspector의 `views` 리스트에 (자식 허브, "키이름") 쌍 등록.
각 항목 옆에 그 오브젝트의 FeatureView 목록(예: `Text, Button`)이 회색 라벨로 표시됨.

```csharp
var shop = new ViewModel();
shop["title"]  = Models.Label("상점");
shop["gold"]   = Models.Label(new FormatNumberPropModel<long>(12345));   // "12,345" 자동 포맷
shop["buy"]    = new ViewModel().With(new TextFeature("구매")).With(new ButtonFeature());
shop["hp.bar"] = Models.Gauge(0.7f);                                     // "." 으로 중첩 경로 지원

shopView.Bind(shop);   // views 리스트 순회하며 shop[키]를 각 허브에 자동 Bind + SetParent
```

키가 ViewModel에 없으면 `ViewComponent: Model for view 'xxx' not found` 경고가 출력됩니다.

확인/취소 팝업은 `Models.Notice`로:

```csharp
var notice = Models.Notice("알림", "정말 삭제할까요?", hasCancel: true);
noticeView.Bind(notice);

notice["confirm"].Feature<ButtonFeature>().OnClick.Subscribe(_ => DeleteItem());
notice["cancel"].Feature<ButtonFeature>().OnClick.Subscribe(_ => ClosePopup());
```

---

## Step 6. 나만의 Feature 쌍 만들기 — HP 바 예제

**상황:** 체력에 따라 게이지 + 위험 시 빨간색을 보여주는 커스텀 능력.

```csharp
using R3;
using Sindy.View;
using UnityEngine;
using UnityEngine.UI;

// 1) 모델 측: 순수 데이터 — 도메인 상태 → UI용 파생값 (Unity API 없음 → 단위 테스트 가능)
public class HealthFeature : ModelFeature
{
    public ReactiveProperty<float> Current { get; } = new(100f);
    public ReactiveProperty<float> Max { get; } = new(100f);
    public ReadOnlyReactiveProperty<float> Ratio { get; }

    public HealthFeature()
    {
        Ratio = Current.CombineLatest(Max, (c, m) => m > 0 ? c / m : 0f)
                       .ToReadOnlyReactiveProperty();
        disposables.Add(Ratio);
    }

    public override void Dispose()
    {
        base.Dispose();
        Current.Dispose();
        Max.Dispose();
    }
}

// 2) 뷰 측: Bind/Clear만 구현 — 모든 구독은 disposables에
public class HealthBarFeatureView : FeatureView<HealthFeature>
{
    [SerializeField] private Image fill;
    [SerializeField] private Color normal = Color.green;
    [SerializeField] private Color danger = Color.red;

    protected override void Bind(HealthFeature feature, ICollection<IDisposable> disposables)
    {
        feature.Ratio.Subscribe(r =>
        {
            fill.fillAmount = Mathf.Clamp01(r);
            fill.color = r < 0.25f ? danger : normal;
        }).AddTo(disposables);
    }

    protected override void Clear()
    {
        fill.fillAmount = 0f;   // 모델 교체/해제 시 UI 초기화 (필요할 때만 오버라이드)
    }
}

// 3) 사용: Controller는 모델만 만진다
var hp = new ViewModel().With(new HealthFeature());
hpHub.Bind(hp);
hp.Feature<HealthFeature>().Current.Value -= damage;   // UI는 알아서 갱신
```

**체크리스트 (새 Feature 쌍 추가 시):**

1. `XxxFeature : ModelFeature` — 내부 모델은 `AddTo(this)`, 직접 만든 ReactiveProperty는 `Dispose()`에서 정리
2. `XxxFeatureView : FeatureView<XxxFeature>` — `Bind()`의 모든 구독에 `.AddTo(disposables)`
3. `[AddComponentMenu("Sindy/Feature Views/...")]` 선언
4. 테스트: `hub.Bind(null); model.Dispose();` 후 값 변경해도 예외/갱신 없는지 확인

---

## Step 7. 정리(cleanup)와 흔한 실수

**정리 순서 — 반드시 이 순서대로:**

```csharp
hub.Bind(null);     // 1. FeatureView 구독 해제
model.Dispose();    // 2. 모델 내부 구독 해제 (EveryUpdate 등)
```

역순이면 Disposed된 Observable에 값이 흘러 예외가 날 수 있습니다.
허브가 파괴되면 1번은 자동 수행되지만, 모델 Dispose는 모델 소유자(Controller)의 책임입니다.

**흔한 실수 Top 5:**

| 실수 | 증상 | 해법 |
|------|------|------|
| FeatureView `Bind()`에서 `.AddTo(disposables)` 누락 | 모델 교체 후에도 옛 구독이 살아 이중 갱신 | 모든 Subscribe에 AddTo |
| 코드 바인딩한 자식 허브에 `SetParent(this)` 누락 | 부모 해제 시 자식 구독 누수 | 자식 허브 바인딩에는 항상 호출 |
| Controller가 View 직접 호출 (`label.text=...`) | MVVM/MVP 혼재, 추적 비용 폭발 | View 갱신은 FeatureView `Bind()` 안에서만 |
| FeatureView 없는 Feature 또는 그 반대 | 능력이 조용히 동작 안 함 | Editor/Dev 빌드 경고 확인, Inspector 매칭(✓/✗) 확인 |
| 같은 모델 인스턴스로 재Bind 후 갱신 기대 | same-instance 스킵으로 무시됨 | `hub.Reload()` 사용 |

---

## 다음 단계

- 리스트/스크롤: `ScrollerFeature` + `ScrollerFeatureView` (SINDY_COMPONENT.md §ScrollerFeatureView — 셀 키/CellCatalog)
- 알림 뱃지: [REDDOT.md](REDDOT.md) — `RedDotFeature` + `RedDotFeatureView`
- 에디터 툴: [EDITOR_TOOLKIT.md](EDITOR_TOOLKIT.md)
- 내장 Feature 쌍 둘러보기: `Runtime/View/Features/` + `Runtime/View/FeatureViews/`
- 실전 예시 모음: `Tests/Runtime/ViewComponentTest/FeatureViewUseCases.cs`
