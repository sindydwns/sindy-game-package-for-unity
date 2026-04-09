# SindyGamePackage — View 아키텍처 문서

## 개요

이 패키지는 Unity UI를 **반응형(Reactive) MVVM 패턴**으로 구성합니다.
R3(Reactive Extensions for Unity)를 기반으로 하며, 단방향·양방향 데이터 바인딩을 컴포넌트 단위로 관리합니다.

---

## 핵심 클래스 계층

```
IViewModel (interface)
└── ViewModel                          # 기본 뷰모델 (자식 뷰모델 트리 + Dispose 관리)
    └── ObservableModel<T>             # Observable<T> Obs 추상 프로퍼티 제공
        ├── PropModel<T>               # ReactiveProperty<T> Prop — 상태 보유
        │   ├── FormatNumberPropModel<T>  # 수치 + 서식함수 → string 자동 변환
        │   └── TimerModel             # 카운트다운 → string 자동 변환
        └── SubjModel<T>              # Subject<T> Subj — 이벤트 발행

SindyComponent (MonoBehaviour)
└── SindyComponent<T>                  # 타입 안전 SetModel(T) / Init(T) / Clear(T)
    └── (각 XxxComponent)
```

---

## 핵심 원칙

### 1. 컴포넌트 ↔ 모델 1:1 대응 규칙

**모든 `XxxComponent`에는 같은 파일에 `XxxModel`이 함께 존재해야 합니다.**

- `XxxModel`은 `XxxComponent`가 허용하는 모델 타입을 상속합니다.
- 사용자는 `XxxModel`을 기본 모델로 쓰고, 필요하면 직접 상속해 확장합니다.

```csharp
// ButtonComponent.cs
public class ButtonModel : SubjModel<Unit> { }

public class ButtonComponent : SindyComponent<SubjModel<Unit>> { ... }
```

### 2. PropModel vs SubjModel

| 구분 | 타입 | 용도 | 예시 |
|------|------|------|------|
| **PropModel\<T\>** | `ReactiveProperty<T>` | 상태 표시 (값 구독) | 텍스트, 색상, 수치, 가시성 |
| **SubjModel\<T\>** | `Subject<T>` | 이벤트 전달 (단발 발행) | 버튼 클릭 |

컴포넌트 제네릭은 **최대한 `PropModel<T>` 또는 `SubjModel<T>` 직접 형태를 유지**합니다.

```csharp
SindyComponent<PropModel<float>>   // GaugeComponent
SindyComponent<PropModel<bool>>    // VisibilityComponent, ToggleComponent
SindyComponent<PropModel<string>>  // LabelComponent (+ TimerModel, FormatNumberPropModel<T>)
SindyComponent<SubjModel<Unit>>    // ButtonComponent
```

### 3. ObservableModel\<T\>

`PropModel<T>`와 `SubjModel<T>`의 공통 추상 기반.
`Observable<T> Obs` 프로퍼티로 두 타입을 통일된 방식으로 구독할 수 있습니다.

```csharp
public abstract class ObservableModel<T> : ViewModel
{
    public abstract Observable<T> Obs { get; }
}
```

---

## 컴포넌트 & 모델 목록

### 기본 컴포넌트 (`Runtime/View/Components/`)

각 파일에 컴포넌트와 모델이 함께 정의됩니다.

| 파일 | Component | 수용 모델 타입 | 함께 정의된 모델 |
|------|-----------|----------------|-----------------|
| `ButtonComponent.cs` | `ButtonComponent` | `SubjModel<Unit>` | `ButtonModel` |
| `LabelComponent.cs` | `LabelComponent` | `PropModel<string>` | `LabelModel`, `TimerModel`, `FormatNumberPropModel<T>` |
| `GaugeComponent.cs` | `GaugeComponent` | `PropModel<float>` | `GaugeModel` |
| `ColorComponent.cs` | `ColorComponent` | `PropModel<Color>` | `ColorModel` |
| `IconComponent.cs` | `IconComponent` | `PropModel<Sprite>` | `IconModel` |
| `VisibilityComponent.cs` | `VisibilityComponent` | `PropModel<bool>` | `VisibilityModel` |
| `ToggleComponent.cs` | `ToggleComponent` | `PropModel<bool>` | `ToggleModel` |
| `PageComponent.cs` | `PageComponent` | `PropModel<int>` | `PageModel` |
| `TabComponent.cs` | `TabComponent` | `PropModel<int>` | `TabModel` |
| `ListComponent.cs` | `ListComponent` | `ListViewModel` | `ListModel`, `ListViewModel`, `ListViewModel<T>` |
| `PopupComponent.cs` | `PopupComponent` | `ViewModel` | `PopupModel` |

### LabelComponent 계열 모델

`LabelComponent`는 `PropModel<string>`을 상속하는 모든 모델을 수용합니다.

| 모델 | 용도 | 주요 API |
|------|------|----------|
| `LabelModel` | 단순 텍스트 | `Value` |
| `TimerModel` | 카운트다운 (`float` → 포맷 문자열) | `Remaining`, `IsFinished`, `Pause()`, `Resume()`, `Reset(duration)` |
| `FormatNumberPropModel<T>` | 수치 + 서식 함수 (`T` → 포맷 문자열) | `Source`, `Format`, `Text` |

```csharp
// 세 모델 모두 LabelComponent에 직접 전달 가능
labelComponent.SetModel(new LabelModel("Hello"));
labelComponent.SetModel(new TimerModel(60f));
labelComponent.SetModel(new FormatNumberPropModel<int>(1000));
```

### 복합(Composite) 컴포넌트 (`Runtime/View/Components/Composite/`)

| 파일 | Component | Model | 설명 |
|------|-----------|-------|------|
| `NoticeComponent.cs` | `NoticeComponent` | `NoticeModel` | 타이틀/내용/확인/취소 팝업 |
| `ItemSlotComponent.cs` | `ItemSlotComponent` | `ItemSlotModel` | 아이콘+수량+레드닷 슬롯 |

---

## SindyComponent 생명주기

```
SetModel(model)
  ├── Clear(이전 model)        ← 이전 구독 해제
  ├── ClearDisposables()       ← disposables 정리
  └── Init(새 model)           ← 새 구독 등록

OnDestroy()
  └── ClearModel()             ← Clear + ClearDisposables
```

### Cleanup 순서 (테스트 코드 등)

```csharp
component.SetModel(null);   // 1. 컴포넌트의 모델 구독 해제
model.Dispose();            // 2. 모델 내부 구독 해제 (EveryUpdate, CombineLatest 등)
disposables.Dispose();      // 3. 외부 관찰 구독 해제
```

---

## Composite 패턴

복잡한 UI 블록은 여러 기본 컴포넌트를 조합해 구성합니다.

```csharp
public class NoticeModel : ViewModel
{
    public PropModel<string> Title   { get; } = new();
    public SubjModel<Unit>   Confirm { get; } = new();
    // ...
}

public class NoticeComponent : SindyComponent<NoticeModel>
{
    protected override void Init(NoticeModel model)
    {
        title.SetModel(model.Title).SetParent(this);    // 자식 연결
        confirm.SetModel(model.Confirm).SetParent(this);
    }
}
```

`SetParent(this)`로 자식 컴포넌트를 부모에 연결하면, 부모의 `SetModel(null)` 시 자식도 자동으로 해제됩니다.

---

## 파일 구성

```
Runtime/View/
├── Core/            # 프레임워크 핵심
│   ├── ViewModel.cs
│   ├── ObservableModel.cs
│   ├── PropModel.cs
│   ├── SubjModel.cs
│   ├── SindyComponent.cs
│   └── ViewComponent.cs
├── Components/      # 컴포넌트 (XxxComponent + XxxModel 같은 파일)
│   ├── LabelComponent.cs    ← LabelModel, TimerModel, FormatNumberPropModel<T> 포함
│   ├── ListComponent.cs     ← ListModel, ListViewModel, ListViewModel<T> 포함
│   ├── ...
│   └── Composite/   # 복합 컴포넌트 (XxxComponent + XxxModel 같은 파일)
└── RedDot/          # 레드닷 시스템
```

### 네임스페이스

| 네임스페이스 | 내용 |
|---|---|
| `Sindy.View` | 프레임워크 코어 (`ViewModel`, `PropModel<T>`, `SubjModel<T>`, `ObservableModel<T>`) |
| `Sindy.View.Components` | 모든 컴포넌트, XxxModel, `TimerModel`, `FormatNumberPropModel<T>`, `ListViewModel` |
| `Sindy.View.Components.Composite` | 복합 컴포넌트 |
| `Sindy.RedDot` | 레드닷 시스템 |

### 새 컴포넌트 추가 체크리스트

1. `Components/XxxComponent.cs` 파일 생성
2. 파일 안에 `XxxModel : PropModel<T>` (또는 `SubjModel<T>`) 정의
3. `XxxComponent : SindyComponent<PropModel<T>>` 정의
4. `Init(T model)`에서 구독, `Clear(T model)`에서 UI 초기화
5. 테스트: `TestXxxComponentWork.cs`에 `Cleanup()` 오버라이드 포함
