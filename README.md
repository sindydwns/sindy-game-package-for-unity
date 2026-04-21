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

### View / MVVM 시스템 (SindyComponent)

UI 컴포넌트가 모델을 직접 관찰하고, 모델 값이 바뀌면 뷰가 자동으로 갱신됩니다. `SindyComponent<T>`가 SetModel → Init → Clear → OnDestroy 생명주기를 표준화하여 구독 누수를 방지합니다. 복합 컴포넌트는 `SetParent(this)`로 연결해 부모-자식 해제를 자동화합니다.

→ [SindyComponent & ViewComponent 상세](./SINDY_COMPONENT.md)

### RedDot 시스템

인벤토리, 메일, 알림처럼 트리 구조로 집계되는 뱃지 카운터입니다. 점 구분 경로(`"inventory.new_item.sword"`)로 노드를 선언하면, 자식 카운트가 바뀔 때 상위 노드에 자동으로 반영됩니다. `RedDotComponent`를 오브젝트에 붙이고 경로만 입력하면 코드 없이도 뱃지가 동작합니다.

→ [RedDot 시스템 상세](./REDDOT.md)

### Inventory 시스템

`Entity`(ScriptableObject) + `Inventory`(컨테이너)로 아이템을 관리합니다. Add/Remove/Set/Move 등 CRUD 연산과 함께 R3 기반 변경 이벤트를 제공합니다. `Contains`, `Intersect`, `Subtract` 집합 연산으로 재료 충분 여부 같은 게임 로직을 간결하게 표현할 수 있습니다.

### ScriptableObject 변수

`IntVariable`, `FloatVariable` 등 ScriptableObject 기반 공유 변수입니다. Inspector에서 상수/변수를 선택할 수 있는 `IntReference` 패턴을 함께 제공해 씬 간 데이터 공유와 에디터 튜닝을 쉽게 합니다.

### Editor Toolkit (SindyEdit)

씬·프리팹·ScriptableObject를 동일한 API로 편집하는 에디터 자동화 도구입니다. `SindyEdit.Open("path")` 한 줄로 에셋 타입에 관계없이 동일한 메서드 체이닝 패턴을 사용할 수 있으며, `using` 블록 종료 시 자동 저장됩니다. HTTP IPC를 통해 외부(터미널, AI)에서 Unity 에디터를 원격 조작할 수 있습니다.

```csharp
// 씬, 프리팹, SO 모두 동일한 패턴
using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
s.GOFind("Fill").SetColor("m_Color", Color.green);
```

→ [Editor Toolkit 상세](./EDITOR_TOOLKIT.md) · [실용 예시](./SINDY_EDIT_EXAMPLES.md)
