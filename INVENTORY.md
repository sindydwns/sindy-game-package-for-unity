# Inventory 모듈

> 저장(Store)·런타임(Core)·관심사(Feature)를 분리한 제네릭 인벤토리 `Inventory<TKey>`.
> 키가 문자열 id든 `Entity`(ScriptableObject)든, 제한이 무게든 슬롯 수든, 서버 원장이 있든 없든
> **코어 하나에 Feature를 꽂아** 조립합니다. View/MVVM 시스템([SINDY_COMPONENT.md](SINDY_COMPONENT.md))의
> `ViewModel.With(feature)` / `Feature<T>()`와 같은 관용구를 씁니다.
> 네임스페이스: `Sindy.Inven`. 메인 스레드 전용(R3 `ReactiveProperty`와 같음).

## 폴더 구조

```
Runtime/Inven/
├── Core/       ItemChange, IInventoryStore, IInventoryFeature(+InventoryFeature 기본 구현),
│               IInventory/IReadOnlyInventory(+InventoryReason), Inventory<TKey>
├── Features/   CapacityFeature, FilterFeature, HookFeature
├── Stores/     DictionaryStore, SerializedListStore(Entity)
└── (루트)      Entity, EntityStack, Inventory(Entity 기반 구형), InventoryEntity/Variable,
                Checkpoint, Mission, EntityAmount, EntityRate — 그대로 유지
```

테스트: `Tests/Runtime/InventoryTest/TestInventoryCore·Features·Stores.cs`, `Tests/Editor/InventoryTests.cs` (NUnit).

---

## 왜 나눴나

구형 `Inventory`/`EntityStack`은 저장 필드(`[SerializeField] amount`)와 반응형(`ReactiveProperty`)이 한 객체에 있어
역직렬화 후 재동기화·`pool` 재사용이 필요했고, 키가 `Entity`에 묶여 있었으며, `MoveTo`가 `Remove` 후 `Add`라 받는 쪽이
거부할 수 없었고, 무게·슬롯·원장 같은 관심사를 꽂을 자리가 없었습니다. 새 코어는 셋을 분리합니다.

| 층 | 타입 | 누가 소유 |
|---|---|---|
| 저장 | `IInventoryStore<TKey>` — `TryGet`/`Set`/`All` | **프로젝트** (직렬화·마이그레이션·에셋 보관은 프로젝트 몫) |
| 런타임 | `Inventory<TKey>` — 게이트, `CountProp`, `Changes`, 원자적 `TryMove` | 패키지 |
| 관심사 | `IInventoryFeature<TKey>` — `CanAccept`(사전 게이트) + `OnChanged`(사후 훅) | 공통은 패키지, 고유는 프로젝트 |

판단 기준은 하나 — **패키지가 모르는 관심사를 프로젝트가 밖에서 꽂을 수 있는가.**
서버 원장, 게임별 회분 규칙 같은 것은 패키지에 넣지 않고 프로젝트가 `IInventoryFeature<TKey>`로 구현합니다.

---

## 빠른 시작

```csharp
using Sindy.Inven;

// 문자열 id + Dictionary 저장, 무게 제한
var bag = new Inventory<string>(new DictionaryStore<string>(saveData.Items))
    .With(new CapacityFeature<string>(id => db[id].Weight, () => stats.MaxWeight, reason: "bag.weight_full"))
    .With(new FilterFeature<string>(id => !db[id].IsCurrency, "bag.no_currency"))
    .With(new HookFeature<string>(c => events.Publish(c)));

bag.Add("wood", 3, reason: "gather");             // 게이트 통과 시 true
if (!bag.CanAdd("stone", 5, out var why))          // 거부 이유 — 거부한 Feature가 채운다
    toast.Show(why);                               // "bag.weight_full"

bag.CountProp("wood").Subscribe(n => label.text = n.ToString());   // 키별 반응형, 첫 요청 때 생성
bag.Changes.Subscribe(c => Debug.Log(c));                          // "wood +3 (0 → 3) [gather]"
bag.Feature<CapacityFeature<string>>().UsedProp.Subscribe(...);    // UI가 Feature 상태를 얻는 통로

bag.TryMove(chest, "wood", 2);                     // 양쪽 게이트 통과 시에만 제거→추가, 실패 시 양쪽 무변경
bag.Pay(recipe.Costs, "craft");                    // 전부 CanRemove 통과 시에만 전부 제거

bag.Rebind(new DictionaryStore<string>(loaded));   // 저장 데이터 교체 — 구독은 살아 있고 값만 재방출
bag.Dispose();                                     // Changes OnCompleted → CountProp 해제 → Feature Detach
```

---

## 계약

```csharp
public readonly struct ItemChange<TKey>
{
    TKey Key; long Delta; long Before; long After; string Reason;
    bool IsAdd, IsRemove, BecameNonEmpty /*0→양수*/, BecameEmpty /*양수→0*/;
}

public interface IInventoryStore<TKey>
{
    bool TryGet(TKey key, out long count);
    void Set(TKey key, long count);                  // 0일 때 항목을 지울지는 스토어가 정한다
    IEnumerable<KeyValuePair<TKey, long>> All();
}

public interface IInventoryFeature<TKey>
{
    void Attach(IInventory<TKey> owner);
    bool CanAccept(TKey key, long delta, out string reason);   // delta<0 = 제거. 통과면 reason = null
    void OnChanged(in ItemChange<TKey> change);                // 안에서 인벤을 다시 바꾸면 예외(재진입 가드)
    void OnRebind();                                           // 스토어 교체 후 캐시 재계산
    void Detach();
}
public abstract class InventoryFeature<TKey> : IInventoryFeature<TKey>   // 전부 virtual no-op, Owner 보관

public interface IReadOnlyInventory<TKey>
{
    long Count(TKey key);
    ReadOnlyReactiveProperty<long> CountProp(TKey key);
    Observable<ItemChange<TKey>> Changes { get; }
    IEnumerable<KeyValuePair<TKey, long>> Entries { get; }
    T Feature<T>() where T : class, IInventoryFeature<TKey>;
    bool HasAll(IEnumerable<KeyValuePair<TKey, long>> costs);  // 수량만 검사, 같은 키 합산
}

public interface IInventory<TKey> : IReadOnlyInventory<TKey>
{
    bool CanAdd(TKey key, long n, out string reason);
    bool CanRemove(TKey key, long n, out string reason);       // 부족이면 InventoryReason.Insufficient
    bool Add(TKey key, long n, string reason = null);
    bool Remove(TKey key, long n, string reason = null);
    bool TryMove(IInventory<TKey> to, TKey key, long n, string reason = null);
    bool Pay(IEnumerable<KeyValuePair<TKey, long>> costs, string reason = null);   // 전부-아니면-전무
    void Rebind(IInventoryStore<TKey> store);
}

public sealed class Inventory<TKey> : IInventory<TKey>, IDisposable
{
    Inventory(IInventoryStore<TKey> store, IEqualityComparer<TKey> comparer = null);
    Inventory<TKey> With(IInventoryFeature<TKey> feature);     // 체이닝. 같은 타입 중복은 예외
    bool CanMove(IInventory<TKey> to, TKey key, long n, out string reason);
    IInventoryStore<TKey> Store; IReadOnlyList<IInventoryFeature<TKey>> Features; bool IsDisposed;
}

public static class InventoryReason { Insufficient = "inventory.insufficient"; Full = "inventory.full"; Rejected = "inventory.rejected"; }
```

### Add 실행 순서 (동기, 같은 호출 안)

```
1. 전 Feature CanAccept(key, +n)   — 하나라도 false면 즉시 false 반환, 상태 무변경
2. store.Set(key, before + n)
3. CountProp(key)가 있으면 Value 갱신  (같은 값이면 R3가 방출 생략)
4. 전 Feature OnChanged(change)      — 등록 순서, 재진입 가드 ON
5. Changes.OnNext(change)
```

`TryMove(to, key, n)`: `this.CanRemove` ∧ `to.CanAdd` 둘 다 통과 → `this` 2~5 → `to` 2~5.
`to`가 `Inventory<TKey>`면 게이트를 다시 보지 않고 바로 적용하고, 다른 구현이면 `to.Add`로 넘긴 뒤 거부되면 되돌립니다.

### 규칙

- **거부는 예외가 아니라 `false` + `reason`.** 예외는 인자 오류(`n <= 0`, null)·재진입·Feature 타입 중복·Dispose 후 사용에만.
- **`OnChanged`는 예외를 던지지 않는 것이 계약.** 던지면 `Debug.LogException`으로 남기고 다음 Feature로 진행합니다(상태 일관성 우선).
- **`OnChanged`·`HookFeature` 안에서 인벤을 바꾸면 `InvalidOperationException`.** `Changes` 구독자는 가드 밖이라 바꿀 수 있지만 권장하지 않습니다.
- **`CountProp`는 키당 최대 1개, Dispose 전까지 유지.** 키 종류가 수천 개면 `Entries` + `Changes`로 목록을 갱신하세요.
- **비교자.** `new Inventory<string>(store, StringComparer.OrdinalIgnoreCase)`처럼 넘기면 `CountProp` 캐시·`HasAll`/`Pay` 합산이 그 비교자를 씁니다. 스토어에도 같은 비교자를 주는 것은 프로젝트 몫입니다(`DictionaryStore(comparer:)`).
- **할당.** `ItemChange`는 `readonly struct`, Feature 순회는 인덱스 루프, `HasAll`/`Pay` 합산 사전은 재사용 — 초당 수백 회 호출에도 GC 0을 목표로 합니다.

---

## 기본 제공 Feature

| Feature | 역할 | 생성자 |
|---|---|---|
| `CapacityFeature<TKey>` | 수치 용량. 무게·슬롯 수·부피 전부 이것 하나 — 키별 비용과 상한을 델리게이트로 받는다. `UsedProp`/`Used`/`Capacity`/`Free`/`IsFullProp` 제공. 증분 캐시, `OnRebind`·`Refresh()`에서 전체 재계산. 비용 0인 키는 용량과 무관, 제거는 항상 허용 | `(Func<TKey,long> costOf, Func<long> capacity, Func<bool> ignoreCap = null, string reason = InventoryReason.Full)` |
| `FilterFeature<TKey>` | 받을 수 있는 키 제한(재화 거부, 연료만 허용 등). 제거는 항상 허용 | `(Func<TKey,bool> accept, string reason = InventoryReason.Rejected)` |
| `HookFeature<TKey>` | 사후 훅을 델리게이트로 — 프로젝트 이벤트 버스 발행용 | `(Action<ItemChange<TKey>> onChanged, Action onRebind = null)` |

> `CapacityFeature`의 상한(`capacity`)이 바깥에서 바뀌면(스탯 변화 등) `Refresh()`를 호출해야 `IsFullProp`가 따라옵니다. 게이트(`CanAdd`)는 매번 최신 상한을 봅니다.

넣지 않은 것: "한 번이라도 보유"(Seen), 서버 원장, 게임별 회분 규칙, 전역 이벤트 버스 타입. 프로젝트가 `InventoryFeature<TKey>`를 상속해 구현합니다.

```csharp
// 프로젝트 고유 Feature 예 — 한 번이라도 보유한 키 기록
sealed class SeenFeature : InventoryFeature<string>
{
    readonly HashSet<string> seen;
    public SeenFeature(HashSet<string> seen) => this.seen = seen;
    public override void Attach(IInventory<string> owner) { base.Attach(owner); OnRebind(); }
    public override void OnChanged(in ItemChange<string> c) { if (c.BecameNonEmpty) seen.Add(c.Key); }
    public override void OnRebind() { foreach (var kv in Owner.Entries) if (kv.Value > 0) seen.Add(kv.Key); }
}
```

---

## 기본 제공 Store

| Store | 용도 |
|---|---|
| `DictionaryStore<TKey>` | `Dictionary<TKey,long>`을 그대로 감싼다. 수량 0이면 항목 제거(`keepZero: true`로 유지). `Dictionary` 프로퍼티로 직렬화 |
| `SerializedListStore` | `Entity` 키. `[SerializeField] List<EntityStack>`를 감싸거나(`EntityStackDrawer`로 인스펙터 편집 가능), 구형 `Inventory`를 감싸 읽기·쓰기를 위임한다(그쪽 `OnChange`도 그대로 발생 — 옮기는 동안 두 API 병행 가능) |

```csharp
// Entity SO + 무게 용량 (구형 Inventory를 감싸는 경우)
var inven = new Inventory<Entity>(new SerializedListStore(inventoryEntity.Inventory))
    .With(new CapacityFeature<Entity>(e => (long)(((Item)e).Weight * 100), () => capacityCentis));

// 슬롯 수 제한 — 키 하나 = 슬롯 하나
var slots = new Inventory<int>(new DictionaryStore<int>())
    .With(new CapacityFeature<int>(_ => 1, () => 30, reason: "bag.full"));

// POCO 저장을 감싸는 프로젝트 스토어
sealed class ItemEntryStore : IInventoryStore<string>
{
    readonly Dictionary<string, ItemEntry> items;
    public bool TryGet(string key, out long count) { var ok = items.TryGetValue(key, out var e); count = ok ? e.Count : 0; return ok; }
    public void Set(string key, long count) { if (!items.TryGetValue(key, out var e)) items[key] = e = new ItemEntry(key); e.Count = count; }
    public IEnumerable<KeyValuePair<string, long>> All() { foreach (var e in items.Values) yield return new(e.Id, e.Count); }
}
```

---

## 구형 API와의 관계

`Entity`·`EntityStack`·`Inventory`(Entity 기반)·`InventoryEntity`·`InventoryVariable`·`InventoryDrawer`는 **그대로 유지**됩니다(alpha.28은 추가만).
옮기고 싶을 때 `Inventory<Entity>` + `SerializedListStore`로 갈아타면 되고, 구형 `Inventory`를 감싸는 동안은 두 API를 함께 쓸 수 있습니다.
구형 `Inventory`를 새 코어의 어댑터로 재구현할지는 alpha.29 이후 별도 판단입니다.

| 구형 | 새 코어 |
|---|---|
| `inv.GetAmount(entity)` | `inv.Count(entity)` |
| `inv.GetEntityStack(entity).OnChange` | `inv.CountProp(entity)` / `inv.Changes` |
| `inv.OnChange` (`ChangeEvent`) | `inv.Changes` (`ItemChange<TKey>` — Before/After/Reason) |
| `inv.MoveTo(other, entity, n)` (비원자) | `inv.TryMove(other, entity, n)` (양쪽 게이트, 원자) |
| `inv.Contains(costInventory)` / 직접 Remove | `inv.HasAll(costs)` / `inv.Pay(costs)` |
| `Serialize()` / `Deserialize(json, map)` | 스토어가 프로젝트 몫 — `Rebind(store)` |
| 래퍼 클래스로 무게 계산 | `CapacityFeature` |
