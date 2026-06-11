# FeatureView 아키텍처 — 설계 결정 기록 (Decision Log)

> **상태: 구현 완료 (2026-06-11, Phase 1~4 전체 반영).**
> 이 문서는 전환 당시의 의사결정과 마이그레이션 근거를 보존하는 기록입니다.
> **현행 아키텍처/API는 [SINDY_COMPONENT.md](SINDY_COMPONENT.md)**, 실습은
> [SINDY_COMPONENT_TUTORIAL.md](SINDY_COMPONENT_TUTORIAL.md)를 참고하세요.
> (이전 버전에 있던 사용 시나리오 Step 1~8은 위 두 문서로 통합되어 제거되었습니다.)

핵심: 타입별 컴포넌트(TextComponent, ButtonComponent...)를 없애고,
**SindyComponent(허브) + FeatureView(능력 단위)** 조합으로 모든 UI를 구성한다.

구현 노트:

- ButtonFeature는 기존 HoldFeature의 KeepHold/Release()를 계승했다.
- 명시되지 않았던 Toggle/Color/Tab/Page/List/RedDot도 Feature 쌍으로 이식되었다.
- Notice/ItemSlot 복합 컴포넌트는 Models 팩토리(Models.Notice/Models.ItemSlot)로 대체되었다.

---

## 확정 설계 4원칙

1. **1:1 대칭** — `ModelFeature`(모델) ↔ `FeatureView`(뷰)는 항상 1:1. N:1은 채택하지 않음.
2. **Feature 경계 기준** — 같은 저수준 입력 자원(포인터 제스처 공간 등)을 공유하는 동작은 **한 Feature의 내부 옵션**으로 통합한다(예: 클릭/홀드 → `ButtonFeature(allowHold:)`). 독립 동작 가능한 능력은 별도 Feature로 분리한다(예: Interactable, Visibility, Highlight).
3. **역방향 참조** — FeatureView가 `[RequireComponent(typeof(SindyComponent))]`로 허브를 보장하고, 스스로 허브를 찾아 구독한다. 허브는 FeatureView의 존재를 모른다(릴리스 빌드 기준).
4. **ReactiveProperty 기반 모델 전파** — SindyComponent는 `IViewModel`이 아닌 `ReactiveProperty<IViewModel>`을 보유. FeatureView는 이 스트림을 구독한다. R3의 "구독 즉시 현재 값 방출" 의미론이 비활성 Bind·늦은 Awake·런타임 AddComponent 타이밍 문제를 전부 해결한다.

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
| `TimerModel`/`FormatNumberPropModel` (PropModel<string> 파생) | `new TextFeature(model)` 생성자 주입으로 그대로 재사용 |
| `RegisterCellType<TVM>` / `RegisterGlobalCellType<TVM>` (타입 키) | `RegisterCell(key, prefab)` / 전역 CellCatalog 에셋 (셀 키) |
| `Section<TVM>` (제네릭) | `Section` (비제네릭) + `ContentKey` 또는 `ContentPrefab` |
| Scroller `SetModel` same-instance 우회 해킹 | 삭제 — ReactiveProperty 의미론 + `Reload()` |

---

## Scroller 전환 상세 (셀 키 + 카탈로그)

가상화 엔진(풀·레이아웃·LateUpdate 패스)은 비제네릭 `SindyComponent` 기준으로 동작하므로 무사했다.
충돌 지점은 하나 — **VM 타입을 키로 쓰는 prefab 해상**. 셀 모델이 전부 `ViewModel + Feature`가 되면
타입 키가 붕괴한다. 해법: 3단계 해상 구조는 유지하고 키만 타입 → 명시적 셀 키(문자열 상수)로 교체.

| 기존 (타입 키) | 확정안 (셀 키) |
|---|---|
| 1) 섹션 명시 prefab | 1) 섹션 명시 prefab (동일) |
| 2) 인스턴스 레지스트리 `RegisterCellType<TVM>` | 2) 인스턴스 카탈로그 `RegisterCell(key, prefab)` |
| 3) 전역 레지스트리 `RegisterGlobalCellType<TVM>` | 3) 전역 카탈로그 `RegisterGlobalCell(key, prefab)` / CellCatalog 에셋 |
| 4) 미등록 throw | 4) 미등록 throw (Init 시점 일괄 검증, atomic 유지) |

수반 변경:

- `PrewarmPool<TVM>` 폐기 (`PrewarmPool(prefab, count)`는 유지)
- 전역 카탈로그는 정적 맵 대신 **CellCatalog ScriptableObject 에셋** 권장: 등록 타이밍 제약(FR-CELL-07) 소멸, 디자이너가 Inspector에서 매핑 확인/수정 가능, 정적 가변 상태(도메인 리로드 이슈) 제거. 코드 등록 API와 병행 가능
- prefab은 `Section`(코드 객체) 프로퍼티에 우선 두고 `SectionOption`(SO) 오버라이드는 보조 — "레이아웃은 같고 prefab만 다른" 경우의 에셋 복제 결합 방지

트레이드오프: 문자열 키는 타입 키의 컴파일 타임 안전을 잃는다. 보완 3중 —
`CellKeys` const 클래스 관례 + Init 시점 미등록 키 throw(누락 키를 메시지에 명시) + Editor에서 CellCatalog 에셋 검사.

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
| TextFeature는 `PropModel<string>` 보유 | `ReactiveProperty<string>` 직접 노출 | 기존 Feature들(Visibility/Interactable)과 일관. TimerModel·FormatNumberPropModel 등 자가 갱신 모델을 생성자 주입으로 재사용 — B안은 수동 배선 보일러플레이트·누수 위험 |
| Scroller는 셀 키 + 카탈로그 하이브리드 | (a) 셀 모델 서브클래스 유지 / (b) 섹션 명시 prefab 단독 | (a)는 "전용 모델 클래스 제거" 철학에 영구 예외 잔류, (b) 단독은 공용 셀(타이틀·필터)을 섹션마다 지정하는 번거로움. 키 교체로 3단계 해상 구조와 전역 등록 편의를 모두 보존 |
| 네이밍: FeatureView | FeatureBinder / Ability | 직관성. Feature(모델)–FeatureView(뷰) 대칭이 이름에서 드러남 |
| Blueprint 조립은 Open() 빌드 후처리 | 뷰 측 동적 해상 (모델에 프리팹 이름 탑재) | 모델에 뷰 정보가 실리면 MVVM 방향성 위반. 조립이 코드 한 곳에 모여 추적 용이 (2026-06: 기존 Open은 모델 트리만 만들고 PrefabName을 무시 — 조립 단계 미구현이었음을 평가에서 발견) |
| 중간 경로 컨테이너 자동 생성 | 컨테이너 프리팹 명시 강제 | `"a.b"` 패치의 `a`는 대부분 구조용 빈 노드 — 강제 시 보일러플레이트 프리팹 양산. 명시가 필요하면 `Patch("a", "container")`로 여전히 가능 |
| LayoutFeature.Apply는 full-spec | 부분 갱신 (지정 속성만 반영) | 부분 갱신은 셀 풀링·재바인딩에서 이전 모델의 padding/방향 잔존 — 실제 버그로 확인. Clear는 파괴 대신 비활성 토글 (풀링 성능) |
| Margin API 제거 | offset 기반 유지 / wrapper 재구현 | offset 방식은 부모 LayoutGroup이 즉시 덮어써 가장 자연스러운 사용처에서 무효 — 오해 유발 API. 간격은 Padding/Spacing으로 충분, wrapper는 필요해질 때 재설계 |
| 디자인 값 거처: 플로우=코드, variant 좌표=뷰 | 한쪽으로 통일 | 구조적 플로우(행·열·간격)는 Blueprint와 함께 선언돼야 조합이 자기완결적. 화면 변형별 절대 좌표는 디자이너가 씬에서 조정하므로 ResponsiveLayoutFeatureView(직렬화)에 |

---

## 구현 단계 (완료 기록)

1. **Phase 1 — 코어**: `ReactiveProperty<IViewModel>` 허브 + `FeatureView<TFeature>` 베이스 + Dev 빌드 검증. 기존 컴포넌트와 공존 ✓
2. **Phase 2 — 기본 Feature 이식**: Text/Image/Gauge/Button(`allowHold` 옵션)/Interactable/Visibility 쌍 구현 ✓
3. **Phase 3 — Scroller**: 셀 키 카탈로그 전환, `Section` 비제네릭화, ScrollerFeature/FeatureView 이식 ✓
4. **Phase 4 — 정리**: 기존 컴포넌트·HoldButton·SupportedFeatureAttribute 제거, 문서/튜토리얼 갱신 ✓

UPM 배포이므로 Phase별 마이너 버전 릴리스로 의존 프로젝트가 점진 이행하도록 했다.
