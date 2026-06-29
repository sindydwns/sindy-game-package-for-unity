# View/Core 가독성 기준 & 리팩토링 실행계획

> 대상: `Runtime/View/Core/` 전체 (16개 파일) + 연관 Editor 코드
> 원칙: **기능 불변(behavior-preserving), 공개 API 호환 유지, 현행 다수파 스타일로 통일**
> 작성일: 2026-06-29

---

## Part 1 — 현황 진단

검토 결과 코드 품질 자체는 양호하나, 파일마다 스타일이 갈려 "일관성"이 가장 큰 가독성 부채다.

| 영역 | 관찰된 불일치 | 근거 파일 |
|---|---|---|
| 죽은 코드 | 참조 0건 파일 2개 | `SindyComponentNamedHandleStore.cs`, `SindyComponentDeferredActionQueue.cs` |
| private 필드 명명 | `_camelCase`(언더스코어) vs `camelCase` | `ComponentBlueprint`만 `_patches/_prefabName`, 나머지 전부 `disposables/children/model` |
| 에디터/런타임 분리 | 런타임 파일 안에 90줄 PropertyDrawer 내장 | `SindyComponent.cs` (`ViewBehaviourDrawer`) vs 별도 파일인 `SindyComponentEditor.cs` |
| XML 문서 주석 밀도 | 0줄 ~ 90줄로 극단적 편차 | `ComponentBlueprint`(90)·`SindyComponent`(53) ↔ `PropModel/SubjModel/ViewModel/ComponentPreset`(0~1) |
| 가드/분기 스타일 | `if (x) return;` 한 줄 vs `if (x) { return; }` 블록 혼용, 중첩 삼항 | `SindyComponent` ↔ `LinkState/ComponentManager`, `ComponentPreset.Build` |
| 불리언 표현 | `IsPrefab == false` vs `!flag` | `ComponentPreset` ↔ 나머지 |
| 지역변수 명명 | 같은 `ViewBehaviour` 순회에 `view` vs `vb` | `SindyComponent` ↔ `SindyComponentEditor` |
| 로직 중복 | Feature 이름 축약·목록 생성이 두 곳에 | `SindyComponent.ViewBehaviourDrawer` ↔ `SindyComponentEditor` |

---

## Part 2 — 가독성 기준 (재사용 표준)

새 규칙을 발명하지 않고 **이미 다수 파일이 따르는 관습**을 표준으로 못 박는다. 변경 폭을 최소화하고 회귀 위험을 낮추기 위함이다.

### S1. 명명
- **private/internal 필드**: 언더스코어 없는 `camelCase`. (`_patches` → `patches`)
- **상수·static readonly**: `PascalCase`.
- **public 타입/멤버**: `PascalCase`, 풀네임 지향.
- **지역변수**: 의미 있는 이름. `ViewBehaviour` 순회 변수는 `view`로 통일.
- **약어 정책**: 신규 코드는 풀네임. 단 **이미 배포된 공개 이름**(`PropModel`, `SubjModel`, `Obs`, `Prop`)은 호환성 때문에 개명하지 않는다 → Part 5 참조.

### S2. 멤버 순서
상수 → static 필드 → 인스턴스 필드 → 프로퍼티 → 생성자/Unity 메시지(Awake·OnDestroy) → public 메서드 → private 메서드 → 중첩 타입.

### S3. 분기·가드 스타일
- 한 줄 가드(`if (cond) return;`) 허용하되 **한 파일 내에서 일관**. 다중 정리(여러 줄)는 중괄호 블록.
- **중첩 삼항 금지** → 명시 분기 또는 지역 함수로 (`ComponentPreset.Build` 대상).
- 불리언은 `!flag` / `is null` / `is not null`로 통일. `== false`·`== null` 지양.

### S4. XML 문서 주석
- **public 타입·public 멤버에 `///` 한 줄 요약 필수** — 특히 패키지 소비자와 AI가 직접 쓰는 API(`PropModel`, `SubjModel`, `ObservableModel`, `ViewModel`, `ComponentPreset`).
- private/internal은 "왜"가 비자명할 때만 (현행 `FrameDispatcher`·`LinkState` 수준 유지).
- 언어는 **한국어** 통일 (프로젝트 관습).

### S5. 에디터/런타임 분리
- 대형 `#if UNITY_EDITOR` 블록(PropertyDrawer·CustomEditor)은 **Editor 어셈블리 파일로 분리**. 런타임 파일에는 직렬화 타입과 런타임 로직만 남긴다.
- 런타임 타입이 에디터 표시용으로 노출하는 최소 훅(`ViewsForEditor` 같은 `internal`)만 `#if UNITY_EDITOR`로 잔류 허용.

### S6. 파일·타입 구성과 죽은 코드
- 한 파일에 1급 타입 하나 (밀접한 인터페이스+구현, 제네릭 변형은 예외 — 현행 `ComponentPreset<T>` 등 유지).
- **참조 0 코드는 제거.** git 이력으로 복구 가능.

### S7. 중복 제거
- 에디터 진단 표시 로직(Feature 이름 축약, 목록 문자열 생성)은 공용 헬퍼 1곳으로 모은다.

---

## Part 3 — 리팩토링 실행계획 (단계별)

각 Phase는 **독립 커밋** 단위. Phase 종료 시마다 컴파일 + 테스트 그린을 확인하고 다음으로 넘어간다. 위험 낮은 순서로 배치했다.

### Phase 0 — 안전망 구축
- 현재 상태에서 컴파일 클린(`read_console`) + 기존 테스트 그린(`run_tests`) 기준선 확보.
- 기준선이 빨간색이면 리팩토링 시작 전 사용자에게 보고.

### Phase 1 — 죽은 코드 제거 *(승인됨)*
- `SindyComponentNamedHandleStore.cs`(+`.meta`), `SindyComponentDeferredActionQueue.cs`(+`.meta`) 삭제.
- 위험: 거의 없음(참조 0 확인). → 컴파일/테스트.

### Phase 2 — 에디터/런타임 분리 + 중복 제거 (S5, S7)
- `SindyComponent.cs`의 중첩 `ViewBehaviourDrawer`를 `Editor/View/ViewBehaviourDrawer.cs`로 이동.
- `ShortFeatureName`·Feature 목록 생성 로직을 `SindyComponentEditor`와 공유하는 공용 헬퍼로 통합.
- 런타임 파일에는 `ViewBehaviour`(직렬화 타입)와 `ViewsForEditor` 훅만 잔류.
- 위험: 중간(에디터 표시 동작 회귀 가능) → 인스펙터 표시 수동 확인 항목 포함.

### Phase 3 — 명명 통일 (S1)
- `ComponentBlueprint`의 `_` 접두 필드 전부 → `camelCase`. (순수 내부 식별자, diff는 크지만 안전)
- `vb` → `view` 등 지역변수 통일.

### Phase 4 — 분기·가독성 정리 (S3)
- `ComponentPreset.Build`의 중첩 삼항을 명시 분기로 해체.
- 파일별 가드 스타일 일관화, `== false`/`== null` → `!`/`is null`.

### Phase 5 — 문서 주석 보강 (S4)
- `PropModel`, `SubjModel`, `ObservableModel`, `ViewModel`, `ComponentPreset`, `ComponentManager` public API에 한국어 `///` 요약 추가.
- 기능 변경 없음(주석만) — 가장 안전, 마지막 직전.

### Phase 6 — 멤버 순서 정리 (S2)
- 파일별로 S2 순서에 맞게 재배치. 기계적·저위험이나 diff가 크므로 가장 마지막.

> Phase 3·6은 diff가 커서 리뷰 부담이 있다. 원하면 Phase 1·2·4·5만 먼저 적용하고 3·6은 보류하는 선택지도 가능.

---

## Part 4 — 검증 전략

- **컴파일**: 각 Phase 후 `read_console`로 에러/경고 0 확인.
- **테스트**: `run_tests`(Runtime + Editor). 특히 `ComponentBlueprintOpenTests`가 `AddView/TryGetView` 공개 API를 커버하므로 회귀 감지에 유효.
- **호출처 동반 점검**: 공개 시그니처를 건드릴 경우 `ComponentBlueprint.cs`·Tests의 호출부를 함께 확인 (이번 계획은 공개 시그니처 불변이 원칙).
- **수동 확인(Phase 2 한정)**: SindyComponent 인스펙터의 Feature/자식 매칭 표가 동일하게 그려지는지 육안 확인.
- **커밋 분리**: Phase별 한글 커밋(`commit` 스킬)으로 되돌리기 쉽게.

---

## Part 5 — 결정·주의 사항

1. **공개 약어 개명 보류**: `PropModel`/`SubjModel`/`Obs`/`Prop` 등은 UPM으로 배포돼 의존 프로젝트가 직접 참조한다. 개명 시 다운스트림 컴파일이 깨진다. 이번 리팩토링에서는 **건드리지 않음**을 기본값으로 잡았다. 개명을 원하면 별도 메이저 버전 작업으로 분리 필요 — 진행 전 확인 요망.
2. **죽은 코드 삭제는 승인됨** (Phase 1).
3. **Phase 3·6의 큰 diff**: 동작은 불변이나 PR 리뷰 비용이 크다. 분할 적용 여부는 선택 가능.

---

## 다음 단계

이 계획대로 진행해도 되는지, 그리고 (a) 전체 Phase 0~6 / (b) 저위험만(0·1·2·4·5) 중 어느 범위로 실행할지 알려주시면 시작하겠습니다.
