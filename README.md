# SindyGamePackage

## 소개

Unity UI 개발에서 반복되는 문제들 — 구독 누수, 모델-뷰 동기화 코드 중복, 알림 집계 로직 산재 — 을 해결하기 위해 만들어진 패키지입니다.

R3(Reactive Extensions for Unity) 기반 MVVM 패턴을 중심으로, 아이템 인벤토리·HTTP 통신·에디터 도구까지 게임 개발에서 자주 필요한 모듈을 함께 제공합니다.

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

### View / MVVM 시스템 (SindyComponent)

UI 컴포넌트가 모델을 직접 관찰하고, 모델 값이 바뀌면 뷰가 자동으로 갱신됩니다. `SindyComponent<T>`가 SetModel → Init → Clear → OnDestroy 생명주기를 표준화하여 구독 누수를 방지합니다. 복합 컴포넌트는 `SetParent(this)`로 연결해 부모-자식 해제를 자동화합니다.

### RedDot 시스템

인벤토리, 메일, 알림처럼 트리 구조로 집계되는 뱃지 카운터입니다. 점 구분 경로(`"inventory.new_item.sword"`)로 노드를 선언하면, 자식 카운트가 바뀔 때 상위 노드에 자동으로 반영됩니다. `RedDotComponent`를 오브젝트에 붙이고 경로만 입력하면 코드 없이도 뱃지가 동작합니다.

### Inventory 시스템

`Entity`(ScriptableObject) + `Inventory`(컨테이너)로 아이템을 관리합니다. Add/Remove/Set/Move 등 CRUD 연산과 함께 R3 기반 변경 이벤트를 제공합니다. `Contains`, `Intersect`, `Subtract` 집합 연산으로 재료 충분 여부 확인 같은 게임 로직을 간결하게 표현할 수 있습니다.

### Http 모듈

`ApiModel<TReq, TRes>`로 REST 엔드포인트를 ViewModel처럼 다룹니다. 요청 발행, 응답 구독, 로딩 상태, 에러 처리를 일관된 인터페이스로 제공하며, `RetryFeature`·`TimeoutFeature`·`OfflineCacheFeature`를 `.With()`로 조합합니다. Auth 시스템은 토큰 갱신과 인증된 클라이언트를 자동으로 처리합니다.

### ScriptableObject 변수

`IntVariable`, `FloatVariable` 등 ScriptableObject 기반 공유 변수입니다. Inspector에서 상수/변수를 선택할 수 있는 `IntReference` 패턴을 함께 제공해, 씬 간 데이터 공유와 에디터 튜닝을 쉽게 합니다.

### Editor Toolkit

Unity 에디터 확장 작업에 자주 필요한 유틸리티 모음입니다. 에디터 워크플로 개선을 위한 도구들이 포함되어 있습니다.

---

## 더 알아보기

- [SindyComponent & ViewComponent](./SINDY_COMPONENT.md)
- [RedDot 시스템](./REDDOT.md)
- [Editor Toolkit](./EDITOR_TOOLKIT.md)
