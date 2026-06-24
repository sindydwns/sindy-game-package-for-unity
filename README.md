# SindyGamePackage

Unity UI 개발에서 반복되는 문제들 — 구독 누수, 모델-뷰 동기화 코드 중복, 알림 집계 로직 산재 — 을 해결하기 위해 만들어진 패키지입니다.

R3(Reactive Extensions for Unity) 기반 MVVM 패턴을 중심으로, 에디터 자동화 도구까지 게임 개발에서 자주 필요한 모듈을 함께 제공합니다.

---

## 설치

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

## 모듈 소개

### View / MVVM 시스템 (SindyComponent + FeatureView)

타입별 UI 컴포넌트 대신 **SindyComponent(허브) + FeatureView(능력 단위)** 조합으로 모든 UI를 구성합니다. 모델은 `ViewModel + Feature 조합`(전용 클래스 불필요), 뷰는 GameObject에 FeatureView를 부착하는 것으로 끝납니다. 허브의 `ReactiveProperty<IViewModel>` 스트림이 모델 교체/해제를 모든 FeatureView에 자동 전파하여 구독 누수를 구조적으로 차단합니다. 자식 허브는 `SetParent(this)`로 연결해 부모-자식 해제를 자동화합니다.

→ [상세 문서](./SINDY_COMPONENT.md)

패키지를 받자마자 조합을 시작할 수 있도록 라벨·아이콘·버튼·컨테이너 등 **기본 부품 프리팹 9종 + 합성 Blueprint(SindyKit)**를 동봉합니다. 무채색 Variant base로 제공되어 프로젝트가 스타일만 덮어쓰면 됩니다.

→ [기본 부품 키트](./Runtime/View/Parts/README.md)

### Scroller (ScrollerFeatureView)

뷰포트에 보이는 셀만 인스턴스화하는 가상화 스크롤 리스트입니다. 다수 섹션 적층, 헤더/푸터/빈 콘텐츠 처리, 그리드 자동 산출(컬럼 수 동적 계산), Easing 기반 스크롤 점프를 지원합니다.

`ScrollerFeature`(섹션 데이터) ↔ `ScrollerFeatureView`(가상화 엔진) 쌍이며, prefab은 명시적 셀 키(문자열) 또는 CellCatalog 에셋으로 해상합니다.

```csharp
ScrollerFeatureView.RegisterGlobalCell("shop.item", itemPrefab);   // 또는 CellCatalog 에셋

var section = new Section(itemList, option) { ContentKey = "shop.item" };
scrollerHub.Bind(new ViewModel().With(new ScrollerFeature(new[] { section })));
```

→ [상세 문서](./SINDY_COMPONENT.md#scrollerfeatureview-가상화-스크롤)

### RedDot 시스템

인벤토리, 메일, 알림처럼 트리 구조로 집계되는 뱃지 카운터입니다. 점 구분 경로(`"inventory.new_item.sword"`)로 노드를 선언하면, 자식 카운트가 바뀔 때 상위 노드에 자동으로 반영됩니다. `RedDotFeatureView`를 오브젝트에 붙이고 경로만 입력하면 코드 없이도 뱃지가 동작합니다.

→ [상세 문서](./REDDOT.md)

### HTTP 모듈

R3 + UnityWebRequest 기반 서버 통신입니다. `ApiModel<TReq, TRes>`가 엔드포인트 하나를 ViewModel로 대표하며, `Request.Send(body)` 발행 → `Response.Data/IsLoading/Error` 자동 갱신 흐름을 UI에 그대로 바인딩합니다. `RetryFeature`/`TimeoutFeature`/`OfflineCacheFeature`를 `.With()`로 합성하고, `AuthenticatedHttpClient`가 토큰 주입과 401 자동 갱신을 처리합니다.

→ [상세 문서](./HTTP.md)

### Inventory 시스템

`Entity`(ScriptableObject) + `Inventory`(컨테이너)로 아이템을 관리합니다. Add/Remove/Set/Move 등 CRUD 연산과 함께 R3 기반 변경 이벤트를 제공합니다. `Contains`, `Intersect`, `Subtract` 집합 연산으로 재료 충분 여부 같은 게임 로직을 간결하게 표현할 수 있습니다.

### ScriptableObject 변수

`IntVariable`, `FloatVariable` 등 ScriptableObject 기반 공유 변수입니다. Inspector에서 상수/변수를 선택할 수 있는 `IntReference` 패턴을 함께 제공해 씬 간 데이터 공유와 에디터 튜닝을 쉽게 합니다.

### Editor Toolkit (SindyEdit)

씬·프리팹·ScriptableObject를 동일한 API로 편집하는 에디터 자동화 도구입니다. `SindyEdit.Open("path")` 한 줄로 에셋 타입에 관계없이 동일한 메서드 체이닝 패턴을 사용할 수 있으며, `using` 블록 종료 시 자동 저장됩니다. HTTP IPC를 통해 외부(터미널, AI)에서 Unity 에디터를 원격 조작할 수 있습니다.

```csharp
// 씬, 프리팹, SO 모두 동일한 패턴
using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
s.FindGameObject("Fill").SetColor("m_Color", Color.green);
```

→ [API 레퍼런스](./EDITOR_TOOLKIT.md) · [튜토리얼](./SINDY_EDIT.md)

---

## 문서 가이드

| 문서 | 내용 |
|------|------|
| [SINDY_COMPONENT.md](./SINDY_COMPONENT.md) | **현행 레퍼런스** — SindyComponent(허브·트리 노드)·FeatureView 아키텍처, Feature 쌍 목록, 키 매핑, ScrollerFeatureView |
| [SINDY_COMPONENT_TUTORIAL.md](./SINDY_COMPONENT_TUTORIAL.md) | View/MVVM 스텝 바이 스텝 실습 — 라벨부터 커스텀 Feature까지 |
| [Runtime/View/Parts/README.md](./Runtime/View/Parts/README.md) | 기본 부품 키트 — 원자 부품 9종·SindyKit Blueprint·Variant 스타일·카탈로그 연결 |
| [FEATURE_VIEW_SCENARIO.md](./FEATURE_VIEW_SCENARIO.md) | FeatureView 전환 설계 결정 기록(Decision Log)·마이그레이션 대응표 — 보존용 |
| [HTTP.md](./HTTP.md) | HTTP 모듈 — ApiModel, Retry/Timeout/OfflineCache 합성, 토큰 자동 갱신, 페이지네이션 |
| [REDDOT.md](./REDDOT.md) | RedDot 트리 집계 시스템, 경로 선언, RedDotFeature/RedDotFeatureView 연결 방법 |
| [EDITOR_TOOLKIT.md](./EDITOR_TOOLKIT.md) | SindyEdit 전체 API 레퍼런스 — 메서드 목록, ComponentScope, HTTP IPC |
| [SINDY_EDIT.md](./SINDY_EDIT.md) | 씬·프리팹·SO 편집 단계별 튜토리얼 — 생성·탐색·삭제·참조 연결까지 |
