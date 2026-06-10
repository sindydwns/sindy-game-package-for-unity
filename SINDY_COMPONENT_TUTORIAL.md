# SindyComponent 스텝 바이 스텝 튜토리얼

> 한 달 만에 돌아온 나(또는 AI)를 위한 실습 가이드. 모든 예제는 현재 패키지 코드 기준으로 검증된 실제 API만 사용합니다.
> 개념 레퍼런스는 [SINDY_COMPONENT.md](SINDY_COMPONENT.md) 참고.

---

## 0. 30초 요약

```
Model (ViewModel)  ──Bind()──▶  Component (SindyComponent<T>)
     ▲                                │
     │ 값 변경 (Controller가 함)        │ Init()에서 구독 등록 → UI 자동 갱신
```

- **Controller는 Model만 만진다.** `text = ...`, `SetActive(...)` 같은 View 직접 호출은 컴포넌트의 `Init()` 안에서만.
- `Bind(model)` 호출 한 번이면 끝. 구독 해제는 `Bind(null)` 또는 컴포넌트 파괴 시 자동.
- 네임스페이스: `Sindy.View`, `Sindy.View.Components`, `Sindy.View.Features` + `R3`

---

## Step 1. 가장 단순한 바인딩 — 텍스트 라벨

**상황:** 플레이어 이름을 화면에 표시한다.

**씬 구성:** TMP Text가 붙은 GameObject에 `TextComponent` 추가 → Inspector에서 `label` 필드에 TMP_Text 연결.

```csharp
using Sindy.View;
using Sindy.View.Components;

public class PlayerHud : MonoBehaviour
{
    [SerializeField] private TextComponent nameLabel;

    private PropModel<string> playerName;

    void Start()
    {
        playerName = new PropModel<string>("신디");
        nameLabel.Bind(playerName);          // 구독 시작, 즉시 "신디" 표시

        playerName.Value = "Citrine";        // 이 한 줄로 UI가 자동 갱신됨
    }

    void OnDestroy()
    {
        nameLabel.Bind(null);                // 1. 컴포넌트 구독 해제
        playerName.Dispose();                // 2. 모델 정리
    }
}
```

**핵심:** `PropModel<T>` = 값을 갖는 상태 모델. `Value` 변경 → 구독자에게 자동 전파.

---

## Step 2. 사용자 입력 받기 — 버튼

**상황:** 공격 버튼 클릭을 게임 로직에 전달한다.

**씬 구성:** uGUI Button 대신 `ButtonComponent` 추가 (HoldButton이 자동으로 RequireComponent됨).

```csharp
using R3;
using Sindy.View.Components;

[SerializeField] private ButtonComponent attackButton;
private ButtonModel attackModel;

void Start()
{
    attackModel = new ButtonModel();
    attackButton.Bind(attackModel);

    attackModel.Subscribe(_ => Attack());    // 클릭 = SubjModel<Unit> 이벤트
}
```

**핵심:** `SubjModel<T>` = 이벤트 모델(상태 없음). View→로직 방향 흐름도 모델을 통한다.
- 모델에서 발행하고 싶으면 `attackModel.OnNext(Unit.Default)` — 코드로 클릭을 시뮬레이션할 수 있어 테스트가 쉬움.

---

## Step 3. Feature로 동작 확장하기

**상황:** 버튼을 쿨다운 동안 비활성화하고, 길게 누르면 연사한다.

```csharp
using Sindy.View.Features;

var attackModel = new ButtonModel()
    .With(new InteractableFeature())   // 활성/비활성
    .With(new HoldFeature());          // 홀드 연사 (홀드 시간/반복 주기는 HoldButton Inspector의 holdTime/holdingRepeatTime)

attackButton.Bind(attackModel);

// 쿨다운: Controller는 Feature의 프로퍼티만 바꾼다
attackModel.Feature<InteractableFeature>().Interactable.Value = false;

// 홀드 반복 이벤트 구독 (repeat = 누적 반복 횟수)
attackModel.Feature<HoldFeature>().OnHold.Subscribe(repeat => AttackRepeat(repeat));
```

**규칙:**
- Feature는 **Bind 전에** `.With(...)`로 등록.
- 컴포넌트가 지원하는 Feature는 클래스의 `[SupportedFeature(...)]` 어트리뷰트 또는 Inspector 하단 목록에서 확인. 미지원 Feature를 넣으면 Editor/Dev 빌드에서 경고 로그가 뜸.
- `VisibilityFeature`/`LayoutFeature`는 모든 컴포넌트가 묵시적 지원 (BindCommonFeatures에서 자동 처리):

```csharp
var vm = new ButtonModel().With(new VisibilityFeature(initialValue: true));
vm.Feature<VisibilityFeature>().Show.Value = false;   // gameObject.SetActive(false) 자동
```

---

## Step 4. 복합 컴포넌트 — 여러 자식 묶기

**상황:** 확인/취소 팝업. 패키지에 내장된 `NoticeComponent`가 좋은 예제이자 그대로 쓸 수 있는 부품.

```csharp
using Sindy.View.Components.Composite;

var notice = new NoticeModel("알림", "정말 삭제할까요?", hasCancel: true);
noticeComponent.Bind(notice);

notice.Confirm.Subscribe(_ => DeleteItem());
notice.Cancel.Subscribe(_ => ClosePopup());
```

내부 구현을 보면 복합 컴포넌트 작성 패턴이 보입니다:

```csharp
public class NoticeComponent : SindyComponent<NoticeModel>
{
    [SerializeField] private TextComponent title;
    [SerializeField] private ButtonComponent confirm;

    protected override void Init(NoticeModel model)
    {
        title.SetModel(model.Title).SetParent(this);      // ★ SetParent 필수
        confirm.SetModel(model.Confirm).SetParent(this);
    }
}
```

**★ `SetParent(this)`를 빠뜨리면** 부모가 `Bind(null)` 될 때 자식 구독이 해제되지 않아 누수됩니다. 복합 컴포넌트의 자식 바인딩에는 반드시 붙이세요.

---

## Step 5. ViewComponent — 코드 없이 키로 매핑

**상황:** 자식이 많은 UI 루트. `Init()`에 바인딩 코드를 일일이 쓰기 싫을 때.

**씬 구성:** 루트에 `ViewComponent` 추가 → Inspector의 `views` 리스트에 (자식 컴포넌트, "키이름") 쌍 등록. 각 항목 옆에 필요한 모델 타입이 회색 라벨로 표시됨.

```csharp
var vm = new ViewModel();
vm["title"]  = new PropModel<string>("상점");
vm["gold"]   = new FormatNumberPropModel<long>(12345);   // "12,345"로 자동 포맷
vm["buy"]    = new ButtonModel();
vm["hp.bar"] = new GaugeModel(0.7f);                     // "." 으로 중첩 경로 지원

rootView.Bind(vm);   // views 리스트를 순회하며 vm[키]를 각 컴포넌트에 자동 Bind + SetParent
```

키가 ViewModel에 없으면 `ViewComponent: Model for view 'xxx' not found` 경고가 출력됩니다.

---

## Step 6. 나만의 컴포넌트 만들기 — HP 바 예제

**상황:** 체력에 따라 게이지 + 수치 텍스트 + 위험 시 빨간색을 보여주는 커스텀 컴포넌트.

```csharp
using R3;
using Sindy.View;
using Sindy.View.Components;
using UnityEngine;
using UnityEngine.UI;

// 1) 모델: 도메인 상태 → UI용 파생값
public class HealthModel : ViewModel
{
    public ReactiveProperty<float> Current { get; } = new(100f);
    public ReactiveProperty<float> Max { get; } = new(100f);
    public ReadOnlyReactiveProperty<float> Ratio { get; }

    public HealthModel()
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

// 2) 컴포넌트: 구독은 전부 Init() 안, 반드시 .AddTo(disposables)
public class HealthBarComponent : SindyComponent<HealthModel>
{
    [SerializeField] private Image fill;
    [SerializeField] private TMPro.TMP_Text label;
    [SerializeField] private Color normal = Color.green;
    [SerializeField] private Color danger = Color.red;

    protected override void Init(HealthModel model)
    {
        model.Ratio.Subscribe(r =>
        {
            fill.fillAmount = Mathf.Clamp01(r);
            fill.color = r < 0.25f ? danger : normal;
        }).AddTo(disposables);

        model.Current.CombineLatest(model.Max, (c, m) => $"{c:0}/{m:0}")
            .Subscribe(t => label.text = t)
            .AddTo(disposables);
    }

    protected override void Clear(HealthModel model)
    {
        fill.fillAmount = 0f;   // 모델 교체/해제 시 UI 초기화 (필요할 때만 오버라이드)
    }
}

// 3) 사용: Controller는 모델만 만진다
healthBar.Bind(healthModel);
healthModel.Current.Value -= damage;   // UI는 알아서 갱신
```

**체크리스트 (새 컴포넌트 추가 시):**

1. `XxxComponent : SindyComponent<XxxModel>` — 모델은 `ViewModel`(또는 `PropModel<T>`/`SubjModel<T>`) 상속
2. `Init(XxxModel)`의 모든 구독에 `.AddTo(disposables)`
3. 자식 컴포넌트 바인딩에는 `.SetParent(this)`
4. 모델의 `Dispose()`에서 직접 만든 ReactiveProperty 정리
5. Feature 지원 시 `[SupportedFeature(typeof(...))]` 선언
6. 테스트: `component.Bind(null); model.Dispose();` 후 값 변경해도 예외/갱신 없는지 확인

---

## Step 7. 정리(cleanup)와 흔한 실수

**정리 순서 — 반드시 이 순서대로:**

```csharp
component.Bind(null);   // 1. 컴포넌트 구독 해제
model.Dispose();        // 2. 모델 내부 구독 해제 (EveryUpdate 등)
```

역순이면 Disposed된 Observable에 값이 흘러 예외가 날 수 있습니다.
컴포넌트가 파괴되면 `OnDestroy`에서 1번은 자동 수행되지만, 모델 Dispose는 모델 소유자(Controller)의 책임입니다.

**흔한 실수 Top 5:**

| 실수 | 증상 | 해법 |
|------|------|------|
| `Init()`에서 `.AddTo(disposables)` 누락 | 모델 교체 후에도 옛 구독이 살아 이중 갱신 | 모든 Subscribe에 AddTo |
| 자식 바인딩에 `SetParent(this)` 누락 | 부모 해제 시 자식 구독 누수 | 복합 컴포넌트에선 항상 호출 |
| Controller가 View 직접 호출 (`label.text=...`) | MVVM/MVP 혼재, 추적 비용 폭발 | View 갱신은 `Init()` 안에서만 |
| Feature를 Bind 후에 `.With()` | 컴포넌트가 Feature를 못 봄 | Bind 전에 등록 |
| 같은 모델 인스턴스로 재Bind 후 갱신 기대 | same-instance 스킵으로 무시됨 | `Reload()` 사용 (Scroller는 예외적으로 항상 재초기화) |

---

## 다음 단계

- 리스트/스크롤: `ScrollerComponent` (SINDY_COMPONENT.md §ScrollerComponent — RegisterCellType은 Bind **이전**에)
- 알림 뱃지: [REDDOT.md](REDDOT.md)
- 에디터 툴: [EDITOR_TOOLKIT.md](EDITOR_TOOLKIT.md)
- 내장 컴포넌트 둘러보기: `Runtime/View/Components/` — Text, Button, Gauge, Image, Toggle, Tab, Page, Popup, List 등
