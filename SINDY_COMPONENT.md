# SindyComponent & ViewComponent

## SindyComponent란

`SindyComponent`는 이 패키지의 모든 UI 컴포넌트가 상속받는 기반 MonoBehaviour입니다.  
Unity의 MonoBehaviour에 **모델 바인딩 생명주기**를 얹어, SetModel → Init → Clear → OnDestroy 흐름을 표준화합니다.

기존 Unity UI 코드에서 반복되던 "구독 등록/해제를 직접 관리해야 하는 부담"을 없애고,  
컴포넌트가 파괴되거나 모델이 교체될 때 구독 누수가 생기지 않도록 보장합니다.

---

## 클래스 구조

```
SindyComponent (MonoBehaviour)          ← 비제네릭 베이스. object Model 보유
└── SindyComponent<T> (abstract)        ← 타입 안전 레이어. T Model, abstract Init(T)
    ├── LabelComponent
    ├── ButtonComponent
    ├── GaugeComponent
    ├── ViewComponent                   ← ViewModel 키-컴포넌트 자동 매핑
    └── (기타 XxxComponent)
```

### SindyComponent (비제네릭)

`object Model`을 보유하며 `SetModel(object)`, `Init(object)`, `Clear(object)` 가상 메서드를 제공합니다.

내부 상태:
- `disposables` — `Init`에서 등록한 구독들을 모아두는 리스트
- `LinkState` — 부모-자식 컴포넌트 연결 관리 (`SindyComponentLinkState`)
- `handles` — 이름 있는 IDisposable 핸들 저장소 (`AddHandle` / `GetHandle`)
- `deferredActions` — GameObject 비활성 중 요청된 코루틴을 OnEnable 시점으로 지연

### SindyComponent\<T\>

```csharp
public abstract class SindyComponent<T> : SindyComponent where T : class
{
    public new T Model { get; }
    public virtual SindyComponent SetModel(T model) { ... }
    protected abstract void Init(T model);
    protected virtual void Clear(T model) { }
}
```

- `SetModel(object)`에서 타입 검사: `T`가 아닌 타입이 오면 `ArgumentException` throw
- `Init(T)` — abstract, 반드시 구현
- `Clear(T)` — virtual (기본 빈 구현), UI 초기화가 필요할 때만 오버라이드

---

## SindyComponent vs ViewComponent

| 항목 | SindyComponent\<T\> | ViewComponent |
|------|---------------------|---------------|
| 역할 | 단일 UI 요소 바인딩 | ViewModel의 자식들을 하위 컴포넌트에 매핑 |
| 제네릭 | `T` (자유) | `ViewModel` 고정 |
| Init 구현 | 직접 구독 코드 작성 | Inspector의 `views` 리스트 순회하며 `model[name]` 전달 |
| 모델 키 접근 | 없음 | `model["이름"]`으로 자식 ViewModel 조회 |
| 사용 위치 | 개별 컴포넌트 (라벨, 버튼 등) | 복잡한 UI 루트, 팝업 컨테이너 |

`ViewComponent`의 Init:

```csharp
protected override void Init(ViewModel model)
{
    foreach (var view in views)
    {
        var childModel = model[view.name];   // ViewModel 딕셔너리에서 조회
        if (childModel != null)
        {
            view.component.SetModel(childModel).SetParent(this);
        }
    }
}
```

Inspector에서 `(SindyComponent, "키이름")` 쌍을 등록해두면, ViewModel에 담긴 자식 모델이 자동으로 연결됩니다.

---

## 생명주기

```
SetModel(newModel)
  ├── 이미 같은 모델이면 조기 반환 (isInitialized && model == Model)
  ├── ClearModel()
  │   ├── Clear(이전 model)           ← 서브클래스 UI 초기화 훅
  │   ├── ClearDisposables()          ← disposables + handles 정리
  │   ├── 자식 컴포넌트 SetModel(null) ← LinkState의 모든 자식 연쇄 해제
  │   └── LinkState 정리              ← 자식 목록 클리어, 부모로부터 분리
  ├── Model = newModel
  └── model != null이면
      ├── BindCommonFeatures(viewModel)   ← VisibilityFeature, LayoutFeature 자동 바인딩
      └── Init(newModel)                 ← 서브클래스 구독 등록 훅

OnDestroy()
  └── ClearModel() + Model = null
```

### Cleanup 순서 (외부 코드에서 정리할 때)

```csharp
component.SetModel(null);   // 1. 컴포넌트의 모델 구독 해제
model.Dispose();            // 2. 모델 내부 구독 해제 (EveryUpdate, CombineLatest 등)
disposables.Dispose();      // 3. 외부 관찰 구독 해제
```

순서가 중요합니다. 컴포넌트가 먼저 구독을 끊고, 그 다음 모델이 Dispose되어야 합니다.  
역순이면 이미 Disposed된 Observable에 값이 흐를 수 있습니다.

---

## Composite 패턴과 SetParent

복잡한 UI는 여러 기본 컴포넌트를 조합합니다. `SetParent(this)`로 자식을 부모에 등록하면,  
부모가 `SetModel(null)` 또는 소멸될 때 자식도 연쇄적으로 `SetModel(null)`이 호출됩니다.

```csharp
public class NoticeComponent : SindyComponent<NoticeModel>
{
    [SerializeField] private LabelComponent title;
    [SerializeField] private ButtonComponent confirm;

    protected override void Init(NoticeModel model)
    {
        title.SetModel(model.Title).SetParent(this);
        confirm.SetModel(model.Confirm).SetParent(this);
    }
}
```

`SetParent(this)` 없이 `SetModel`만 호출하면, 부모가 교체될 때 자식 구독이 누수됩니다.

---

## BindCommonFeatures

`Init` 전에 자동 실행됩니다. 모델이 `ViewModel`이면 다음 Feature를 자동 처리합니다:

- `VisibilityFeature` → `gameObject.SetActive`
- `LayoutFeature` → `RectTransform`에 레이아웃 적용

개별 컴포넌트에서 이미 처리하는 경우 `BindCommonFeatures`를 오버라이드하여 비활성화할 수 있습니다.

---

## 새 컴포넌트 추가 체크리스트

1. `Components/XxxComponent.cs` 파일 생성
2. 같은 파일에 `XxxModel : PropModel<T>` (또는 `SubjModel<T>`) 정의
3. `XxxComponent : SindyComponent<XxxModel>` 정의
4. `Init(XxxModel model)` — 구독 등록 (`disposables`에 추가)
5. `Clear(XxxModel model)` — UI 초기화 (필요할 때만)
6. 복합 컴포넌트면: 자식 컴포넌트에 `.SetParent(this)` 반드시 호출
7. 테스트: `component.SetModel(null); model.Dispose();` cleanup 검증
