# RedDot 시스템

## 왜 만들었는가

게임 UI에서 "인벤토리에 새 아이템이 있다", "메일이 읽지 않았다" 같은 알림은 트리 구조로 집계됩니다.  
예를 들어 `인벤토리 > 장비 > 새 아이템`에 카운트가 생기면, 상위 `인벤토리` 탭에도 뱃지가 표시되어야 합니다.

이 집계 로직을 매번 수동으로 구현하면 UI마다 중복 코드가 생기고, 경로가 깊어질수록 관리가 어렵습니다.  
RedDot 시스템은 **점 구분 경로로 트리를 구성하고, 자식 변경을 상위로 자동 집계**하는 반응형 카운터입니다.

---

## 핵심 클래스 구조

```
IRedDotNode (interface)
└── RedDotNode (abstract)           ← Name, Path, Parent, Count, IsActive 공통 보유
    ├── RedDotBranch                ← 자식 노드를 보유하는 중간 노드
    │   └── RedDotBranch.Root       ← static 전역 루트
    └── RedDotLeaf                  ← 직접 값을 설정하는 말단 노드
```

### RedDotNode (공통)

| 프로퍼티 | 타입 | 설명 |
|----------|------|------|
| `Name` | `string` | 노드 단일 이름 |
| `Path` | `string` | 점 구분 전체 경로 (부모 변경 시 자동 업데이트) |
| `Count` | `ReadOnlyReactiveProperty<int>` | 이 노드의 카운트 |
| `IsActive` | `ReadOnlyReactiveProperty<bool>` | `Count > 0` |
| `UserData` | `object` | 임의 데이터 첨부 가능 |

### RedDotBranch

자식 노드를 보유합니다. `Count` 계산 방식을 두 가지로 전환할 수 있습니다:

| `UseActiveCount` | Count 계산 |
|------------------|------------|
| `true` (기본) | 활성(`IsActive == true`) 자식의 수 |
| `false` | 모든 자식 Count의 합산 |

자식 추가/삭제 시 `ReactiveList` 이벤트로 구독이 자동 연결/해제됩니다.

### RedDotLeaf

직접 `Count.Value`를 설정하는 말단 노드입니다.  
`Count`가 `ReactiveProperty<int>`로 쓰기 가능하며, `Clear()`는 `Count.Value = 0`으로 초기화합니다.

### RedDotComponent

`SindyComponent<RedDotModel>` 을 상속한 Unity 컴포넌트입니다.

- Inspector 필드: `dot` (GameObject), `text` (TMP_Text), `scaler`, `defaultPath`, `isLeaf`
- `defaultPath`가 지정되면 `SetModel` 없이도 `Awake`에서 자동으로 해당 경로의 노드를 구독합니다
- `SetModel(RedDotModel)`이 호출되면 모델의 Count로 소스를 교체합니다
- 표시 규칙:
  - `count == 0`: dot 숨김
  - `count == 1`: dot 표시, text 없음 (dot 크기 `scaler` 적용)
  - `count >= 2`: dot 표시, text에 숫자 표시 (dot 크기 원래대로)

### RedDotModel

`PropModel<int>`를 상속합니다. 노드의 `Count`를 구독해 자신의 `Prop`에 흘려보냅니다.

```csharp
public class RedDotModel : PropModel<int>
{
    public RedDotNode Node { get; private set; }

    public RedDotModel(RedDotNode node) { ... }
    public RedDotModel(string path, bool isLeaf = false) { ... }  // 경로로 노드 자동 조회/생성
}
```

---

## 사용법

### 기본 — Leaf 카운트 설정

```csharp
// 말단 노드 확보 (없으면 자동 생성)
var sword = RedDotNode.Root.EnsureLeaf("inventory.new_item.sword");
sword.Count.Value = 3;

// 상위 Branch 카운트 확인
var newItem = RedDotNode.Root.GetBranch("inventory.new_item");
newItem.Count.CurrentValue;  // 1 (기본: 활성 자식 수 = sword 1개가 활성)

newItem.UseActiveCount.Value = false;
newItem.Count.CurrentValue;  // 3 (합산 모드)

// 초기화
sword.Clear();  // Count = 0
```

### Branch 계층 구성

```csharp
// 경로를 명시하면 중간 Branch도 자동 생성됨
var mailUnread = RedDotNode.Root.EnsureLeaf("social.mail.unread");
var friendReq  = RedDotNode.Root.EnsureLeaf("social.friend.request");

mailUnread.Count.Value  = 5;
friendReq.Count.Value   = 2;

var social = RedDotNode.Root.GetBranch("social");
social.Count.CurrentValue;  // 2 (활성 자식 Branch 수: mail, friend)
```

### RedDotComponent — Inspector 연결

1. 오브젝트에 `RedDotComponent` 추가
2. `dot` 필드에 뱃지 GameObject 연결
3. `defaultPath`에 `"inventory.new_item"` 등 경로 입력
4. 코드에서 별도 SetModel 없이 노드 카운트가 바뀌면 자동 갱신됨

### RedDotComponent — 코드로 연결

```csharp
var model = new RedDotModel("inventory.new_item");
// 또는 노드 직접 전달
var node  = RedDotNode.Root.EnsureBranch("inventory.new_item");
var model = new RedDotModel(node);

redDotComponent.SetModel(model);
```

### 노드 조회 API

```csharp
// 있으면 반환, 없으면 null
RedDotNode   node   = RedDotNode.Root.GetNode("a.b.c");

// 있으면 반환, 없으면 예외
RedDotBranch branch = RedDotNode.Root.GetBranch("a.b");
RedDotLeaf   leaf   = RedDotNode.Root.GetLeaf("a.b.c");

// 없으면 자동 생성
RedDotBranch branch = RedDotNode.Root.EnsureBranch("a.b");
RedDotLeaf   leaf   = RedDotNode.Root.EnsureLeaf("a.b.c");

// 절대 경로 조회 (Root 기준)
RedDotNode node = RedDotBranch.GetNodeAbs("a.b.c");
```

---

## 주의사항

- **`Root`는 static 싱글턴**입니다. 테스트 간 격리가 필요하면 `branch.Reset()`을 호출하세요.  
  `Reset()`은 자식 노드를 전부 제거하고 카운트를 0으로 초기화합니다.
- **Leaf와 Branch는 같은 경로에 공존할 수 없습니다.** 이미 Leaf인 경로에 `EnsureBranch`를 호출하면 `InvalidOperationException`이 발생합니다.
- **`UseActiveCount`는 Branch 단위로 설정됩니다.** 하위 Branch가 독립적으로 모드를 가질 수 있습니다.
- **`RedDotNode.Dispose()`는 노드 자체를 해제합니다.** 트리에서 제거하려면 부모 Branch의 `Reset()` 또는 트리 재구성이 필요합니다.
- **`RedDotComponent`의 `defaultPath`는 Awake에서 한 번만 해석됩니다.** 런타임에 경로를 바꾸려면 `SetModel`을 사용하세요.
