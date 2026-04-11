# Sindy Game Package

R3 기반 반응형 MVVM UI 프레임워크 for Unity 2021.3+

## Installation

1. **NuGetForUnity** — Package Manager > Add package from git URL:
   ```
   https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity
   ```
2. **R3** — NuGet > Manage NuGet Packages에서 `R3` 검색 후 설치
3. **R3.Unity** — Package Manager > Add package from git URL:
   ```
   https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity
   ```

> Assembly Version Validation 오류 시: Edit > Project Settings > Player > Other Settings > Assembly Version Validation 체크 해제

---

## 핵심 개념

```
PropModel<T>  — 상태 보유 (값 변경 → UI 자동 갱신)
SubjModel<T>  — 이벤트 발행 (버튼 클릭 등 단발 이벤트)
```

```csharp
// 모델 생성 → 컴포넌트 연결 → 값 변경 → 정리
var label = new LabelModel("Hello");
labelComponent.SetModel(label);
label.Value = "World";

labelComponent.SetModel(null);  // 구독 해제
label.Dispose();                // 모델 정리
```

---

## 컴포넌트 목록

| Component | Model | 바인딩 대상 |
|-----------|-------|------------|
| `LabelComponent` | `PropModel<string>` | TMP_Text |
| `ButtonComponent` | `SubjModel<Unit>` | Button 클릭 |
| `GaugeComponent` | `PropModel<float>` | Image.fillAmount (0~1) |
| `IconComponent` | `PropModel<Sprite>` | Image.sprite |
| `ColorComponent` | `PropModel<Color>` | Graphic.color |
| `ToggleComponent` | `PropModel<bool>` | Toggle (양방향) |
| `VisibilityComponent` | `PropModel<bool>` | GameObject.SetActive |
| `PageComponent` | `PropModel<int>` | 자식 인덱스 활성화 |
| `TabComponent` | `PropModel<int>` | 탭 선택 (양방향) |
| `ListComponent` | `ListViewModel` | 프리팹 풀링 리스트 |
| `PopupComponent` | `ViewModel` | 팝업 루트 |

### LabelComponent 확장 모델

| Model | 용도 | 주요 API |
|-------|------|----------|
| `LabelModel` | 단순 텍스트 | `Value` |
| `TimerModel` | 카운트다운 | `Remaining`, `IsFinished`, `Pause()`, `Resume()`, `Reset()` |
| `FormatNumberPropModel<T>` | 숫자 서식 | `Source`, `Text` |

```csharp
// Timer — LabelComponent에 직접 전달 가능
var timer = new TimerModel(10f);
timer.IsFinished.Where(v => v).Subscribe(_ => Debug.Log("종료"));
labelComponent.SetModel(timer);

// FormatNumber
var num = new FormatNumberPropModel<int>(0);
labelComponent.SetModel(num);
num.Source.Value = 9999; // "9,999"
```

### List

```csharp
var model = new ListViewModel();
listComponent.SetModel(model);

model.SetItems(new List<IViewModel>
{
    new PropModel<string>("A"),
    new PropModel<string>("B"),
});

// 타입 안전 버전
var typed = new ListViewModel<ShopItemModel>();
typed.SetItems(shopItems);
```

---

## 복합(Composite) 컴포넌트

```csharp
// NoticeComponent — 확인/취소 팝업
var notice = new NoticeModel("구매", "정말 구매하시겠습니까?", hasCancel: true);
notice.Confirm.Subscribe(_ => Debug.Log("확인"));
notice.Cancel.Subscribe(_ => Debug.Log("취소"));
noticeComponent.SetModel(notice);
```

### 커스텀 복합 모델 작성법

```csharp
public class ProfileModel : ViewModel
{
    public LabelModel        Name  { get; } = new();
    public PropModel<float>  Hp    { get; } = new();
    public ButtonModel       Close { get; } = new();

    public ProfileModel(string name, float hp)
    {
        Name.Value = name;
        Hp.Value   = hp;
        this["name"]  = Name;   // ViewComponent 자동 바인딩 경로
        this["hp"]    = Hp;
        this["close"] = Close;
    }
}
```

---

## Feature

`ViewModelFeature`를 `.With()`로 모델에 부착합니다.

| Feature | 용도 | 주요 프로퍼티 |
|---------|------|--------------|
| `InteractableFeature` | 상호작용 잠금 | `Interactable` (bool) |
| `HoldFeature` | 롱프레스 | `OnHold`, `AllowHold`, `Release()` |
| `VisibilityFeature` | 표시/숨김 | `Visibility` (bool) |
| `HighlightFeature` | 하이라이트 | — |
| `RaycastBlockFeature` | 레이캐스트 차단 | — |

```csharp
var button = new ButtonModel()
    .With(new InteractableFeature())
    .With(new HoldFeature(onHold: () => Debug.Log("Hold!")));

buttonComponent.SetModel(button);

// Feature 접근
button.Feature<InteractableFeature>().Interactable.Value = false;
```

---

## ComponentBlueprint

코드에서 프리팹을 조립하여 UI를 엽니다. 모델은 항상 팩토리로 주입하며, Blueprint는 재사용할 수 있습니다.

```
Create("프리팹") → WithModel(팩토리) → Patch("경로", "프리팹") → WithModel(팩토리) → Open(레이어)
```

```csharp
// 기본 팝업
ComponentBlueprint
    .Create("notice_popup").WithModel(() => new PopupModel())
    .Patch("header.title", "label").WithModel(() => new LabelModel("공지"))
    .Patch("footer.confirm", "button").WithModel(() => new ButtonModel())
    .Open(UILayer.Popup);

// 팩토리로 지연 생성 (Open 시점에 모델 생성)
ComponentBlueprint
    .Create("shop_popup").WithModel(() =>
    {
        var model = new ShopModel();
        model.Category.Subscribe(i => model.Items.SetItems(LoadItems(i)));
        return model;
    })
    .Open(UILayer.Popup);

// 재사용 가능한 Blueprint 템플릿
static readonly ComponentBlueprint Card = ComponentBlueprint.Create("card")
    .Layout(Direction.Vertical, spacing: 4)
    .Patch("icon", "icon_prefab")
    .Patch("title", "label");

ComponentBlueprint.Create(Card)
    .WithModel(() => new CardModel())
    .Open(UILayer.Popup);
```

---

## RedDot 시스템

트리 구조 알림 카운터. 하위 변경이 상위에 자동 집계됩니다.

```csharp
var sword = RedDotNode.Root.EnsureLeaf("inventory.new_item.sword");
sword.Count.Value = 3;

var parent = RedDotNode.Root.GetBranch("inventory.new_item");
parent.Count.CurrentValue;    // 1 (활성 자식 수, 기본)
parent.UseActiveCount.Value = false;
parent.Count.CurrentValue;    // 3 (자식 Count 합산)

sword.Clear(); // Count = 0
```

---

## Inventory 시스템

`Entity`(ScriptableObject) + `Inventory`(컨테이너) 기반 아이템 관리.

```csharp
var inventory = new Inventory();

// CRUD
inventory.Add(gold, 100);
inventory.Remove(gold, 30);          // 반환: 실제 제거량
inventory.Set(gold, 200);
long amount = inventory.GetAmount(gold);
bool has    = inventory.Contains(gold);

// 이벤트
inventory.OnChange.Subscribe(e => Debug.Log($"{e.stack.Entity.name}: {e.oldAmount} → {e.stack.Amount}"));
inventory.OnChangeStack.Subscribe(s => Debug.Log($"스택 생성/삭제: {s.Entity.name}"));

// 이동
playerInv.MoveTo(shopInv, gold, 500);

// 집합 연산
owned.Contains(required);        // 재료 충분 여부
owned.Intersect(required);       // 교집합 (min)
owned.Subtract(required);        // 차집합

// 직렬화
string s = inventory.Serialize();           // "1:100,2:5"
inventory.Deserialize(s, itemDictionary);
```

---

## Reactive 컬렉션

```csharp
var list = new ReactiveList<string>();
list.OnAdded += item => Debug.Log($"+{item}");
list.OnRemoved += item => Debug.Log($"-{item}");
list.Add("Sword");
list.Remove("Sword");

var set  = new ReactiveSet<int>();       // 중복 무시
var dict = new ReactiveDictionary<string, int>();
```

---

## ScriptableObject 변수

`IntVariable`, `FloatVariable`, `BoolVariable`, `LongVariable`,
`Vector2Variable`, `Vector3Variable`, `Vector2IntVariable`, `Vector3IntVariable`, `ObjectVariable`

```csharp
[SerializeField] private IntVariable playerHp;

playerHp.Value = 100;
playerHp.OnChange += v => Debug.Log($"HP: {v}");

// Reference — Inspector에서 상수/변수 선택
[SerializeField] private IntReference maxHp;
int value = maxHp.Value;
```

---

## Http 모듈

R3 Observable 기반의 HTTP 클라이언트 시스템. View 모듈의 ViewModel/Feature 패턴을 그대로 활용합니다.

### ApiModel — REST 엔드포인트 ViewModel

```csharp
var api = new ApiModel<LoginReq, LoginRes>(client, "/api/login");

// 요청 발행
api.Request.Send(new LoginReq { Id = "user", Password = "pw" });

// 응답 구독
api.Response.Data.Subscribe(res => Debug.Log(res.Token)).AddTo(disposables);
api.Response.IsLoading.Subscribe(v => spinner.SetActive(v));
api.Response.Error.Subscribe(err => Debug.LogError(err.Message));
```

### Http Feature

ApiModel에 `.With()`로 조합하여 횡단 관심사를 추가합니다.

| Feature | 용도 | 주요 설정 |
|---------|------|----------|
| `RetryFeature` | 자동 재시도 (Network/Timeout만) | `maxRetry`, `baseDelay` (지수 백오프) |
| `TimeoutFeature` | 요청 타임아웃 | `seconds` |
| `OfflineCacheFeature<T>` | 오프라인 캐시 (유효시간 기반) | `maxAge` |

```csharp
var api = new ApiModel<Unit, DataDto>(client, "/api/data", HttpMethod.GET)
    .With(new RetryFeature(maxRetry: 3, baseDelay: 1f))
    .With(new TimeoutFeature(seconds: 10f));
```

### Auth — 인증 시스템

```csharp
// OAuth 로그인
var auth = new AuthService(tokenModel);
auth.LoginWith(googleProvider).Subscribe();
auth.IsLoggedIn.Subscribe(v => loginButton.SetActive(!v));

// 인증된 HTTP 클라이언트 — 토큰 자동 첨부 + 만료 시 자동 갱신
var authClient = new AuthenticatedHttpClient(baseClient, tokenModel, refreshService);
```

- **TokenModel** — Access/Refresh 토큰 관리
- **TokenRefreshService** — 401 응답 시 자동 토큰 갱신
- **ITokenStore / PlayerPrefsTokenStore** — 토큰 영속 저장소

### PaginatedApiModel — 페이지네이션

```csharp
var paged = new PaginatedApiModel<RankingDto>(client, "/api/ranking",
    dto => { var vm = new ViewModel(); vm["name"] = new PropModel<string>(dto.Name); return vm; });

paged.GoToPage(1);
paged.CurrentPage.Subscribe(p => pageLabel.Value = $"{p}/{paged.TotalPages.Value}");
paged.PrevButton  // ← 자동 비활성화 (1페이지)
paged.NextButton  // ← 자동 비활성화 (마지막 페이지)
```

---

## 클래스 계층

```
ViewModel
├── ObservableModel<T>
│   ├── PropModel<T>  ← LabelModel, TimerModel, FormatNumberPropModel<T>
│   └── SubjModel<T>  ← ButtonModel
├── ApiModel<TReq, TRes>
├── PaginatedApiModel<TItem>
├── AuthService
└── ViewModelFeature
    ├── InteractableFeature, HoldFeature, VisibilityFeature, ...
    ├── RetryFeature, TimeoutFeature, OfflineCacheFeature<T>
    └── (사용자 정의 Feature)

SindyComponent<T> (MonoBehaviour)
├── LabelComponent, ButtonComponent, GaugeComponent, IconComponent, ...
├── ListComponent, PopupComponent, ViewComponent
└── NoticeComponent, ItemSlotComponent (Composite)
```
