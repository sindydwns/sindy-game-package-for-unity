# Editor Toolkit

씬·프리팹·ScriptableObject를 메서드 체이닝 + `using` 블록 패턴으로 편집하고, HTTP IPC로 외부에서 원격 실행하기 위한 통합 가이드.

---

## 목차

1. [빠른 시작](#1-빠른-시작)
2. [SindyEdit — 통합 파사드](#2-sindyedit--통합-파사드)
3. [AssetEditSession API](#3-asseteditsession-api)
4. [ComponentScope API](#4-componentscope-api)
5. [개별 클래스 레퍼런스](#5-개별-클래스-레퍼런스)
6. [HTTP IPC](#6-http-ipc)
7. [배치 모드](#7-배치-모드)
8. [주의사항](#8-주의사항)

---

## 1. 빠른 시작

```csharp
// 씬 편집
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
s.GO("Canvas/Panel/Title").SetString("m_text", "Hello World");
// Dispose → 자동 저장

// 프리팹 편집 (코드 패턴 완전히 동일)
using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
s.FindGameObject("Fill").SetColor("m_Color", Color.green);

// ScriptableObject 편집
using var s = SindyEdit.Open("Assets/Config/Game.asset");
s.SetInt("maxHealth", 200).SetFloat("gravity", 9.81f);

// 이름으로 자동 탐색 (프리팹 → 씬 → SO 순서)
using var s = SindyEdit.Find("GaugeBar");
s.FindGameObject("Fill").SetColor("m_Color", Color.cyan);
```

---

## 2. SindyEdit — 통합 파사드

`namespace Sindy.Editor.EditorTools`

확장자를 보고 에셋 타입을 자동 판별합니다. `.unity` → SceneEditor, `.prefab` → PrefabEditor, 나머지 → SerializedObject 직접.

### 에셋 열기 / 생성 / 삭제

| 메서드 | 설명 |
|--------|------|
| `Open(string assetPath)` | 경로로 세션 열기. 확장자로 타입 자동 감지. 실패 시 `null` |
| `Find(string nameOrPath)` | 이름으로 에셋 탐색. 프리팹 → 씬 → SO 순서. 경로면 `Open`과 동일 |
| `Create<T>(string assetPath)` | 새 ScriptableObject 생성 후 세션 반환. 이미 있으면 덮어씀. 디렉터리 없으면 자동 생성 |
| `NewScene(string assetPath)` | 빈 씬 파일 생성 후 세션 반환. **이미 있으면 `InvalidOperationException`** |
| `NewPrefab(string assetPath, string rootName = "Root")` | 빈 프리팹 파일 생성 후 세션 반환. **이미 있으면 `InvalidOperationException`** |
| `NewAsset<T>(string assetPath)` | 새 ScriptableObject `.asset` 생성 후 세션 반환. **이미 있으면 `InvalidOperationException`** |
| `Delete(string assetPath)` | `.unity` / `.prefab` / `.asset` 파일 삭제. 없으면 `InvalidOperationException` |
| `Exists(string assetPath)` | 파일 존재 여부 확인. `bool` 반환 |

```csharp
// ScriptableObject 신규 생성 (파일 이미 있으면 throw)
using var s = SindyEdit.NewAsset<FloatVariable>("Assets/Data/Speed.asset");
s.SetFloat("Value", 5f);

// 빈 씬 생성 (파일 이미 있으면 throw)
using var s = SindyEdit.NewScene("Assets/Scenes/Empty.unity");

// 빈 프리팹 생성 후 GO 추가
using var s = SindyEdit.NewPrefab("Assets/Prefabs/NewButton.prefab", "Button");
s.CreateGameObject("Label").AddComponent<TextMeshProUGUI>();

// 파일 존재 확인 후 조건부 생성
if (!SindyEdit.Exists("Assets/Data/Speed.asset"))
    using var s = SindyEdit.NewAsset<FloatVariable>("Assets/Data/Speed.asset");

// 에셋 삭제
SindyEdit.Delete("Assets/Data/OldConfig.asset");
```

---

## 3. AssetEditSession API

`SindyEdit.Open()` 등이 반환하는 편집 컨텍스트입니다. `IDisposable` — `using` 블록 종료 시 변경사항을 자동 저장합니다.

**FP 설계:** `GO()`, `Root()`, `FindGameObject()`, `Child()` 등 탐색 메서드는 `this`를 변경하지 않고 새로운 `AssetEditSession` 인스턴스를 반환합니다. 반환값을 변수에 받아야 합니다.

### GO 탐색

| 메서드 | 설명 |
|--------|------|
| `GO(string goPath)` | 씬 루트 기준 계층 경로로 GO 탐색. `/` 또는 `.` 구분자 허용 |
| `Root()` | 씬의 첫 번째 루트 GO 또는 프리팹 루트 GO를 가리키는 새 세션 반환 |
| `FindGameObject(string name)` | 이름으로 전체 계층 재귀 탐색. 위치를 모를 때 사용 |
| `Child(int index)` | 현재 GO의 인덱스로 직계 자식을 가리키는 새 세션 반환 |
| `Child(string name)` | 현재 GO의 이름으로 직계 자식을 가리키는 새 세션 반환 |

> `.asset` 파일에서 `GO()` 계열 메서드를 호출하면 LogWarning 후 무시됩니다.

**GO() 탐색 방식**
- 씬: 씬 루트 기준 경로. `"Canvas/Panel/Title"` ≡ `"Canvas.Panel.Title"`
- 프리팹: 프리팹 루트의 **자식** 기준 경로
- 탐색 실패 시 LogWarning 출력, 이후 체이닝은 무시됨

**FP 주의:** `Root()` / `FindGameObject()` / `Child()`는 새 세션을 반환합니다. 반환값을 사용하지 않으면 탐색 결과가 버려집니다.

```csharp
// 올바른 사용
var title = s.FindGameObject("Title");
var root  = s.Root();
var first = s.GO("Canvas").Child(0);

// 잘못된 사용 — 반환값을 버림
s.Root();                   // root 세션이 버려짐
s.FindGameObject("Title");  // 이 세션으로 이어지지 않음
```

### GO 생성 / 삭제

| 메서드 | 설명 |
|--------|------|
| `CreateGameObject(string name)` | 현재 GO의 자식으로 새 GO 생성. `_currentGO` null이면 씬/프리팹 루트에 생성. 생성 후 새 GO를 가리키는 세션 반환 |
| `DeleteGameObject()` | 현재 GO 삭제. 부모 GO 세션 반환 (부모 없으면 null GO 세션) |

### 컴포넌트

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `HasComponent<T>(int index = 0)` | `bool` | 현재 GO에 T 타입 컴포넌트가 있는지 확인 |
| `GetComponent<T>(Action<ComponentScope> action = null, int index = 0)` | `ComponentScope?` | 현재 GO에서 n번째 T 컴포넌트에 접근. 없으면 `null`. 콜백 전달 시 즉시 실행 |
| `GetOrAddComponent<T>(Action<ComponentScope> action = null, int index = 0)` | `ComponentScope` | 컴포넌트가 없으면 추가 후 `ComponentScope` 반환. Undo 등록 |
| `AddComponent<T>()` | `ComponentScope` | 컴포넌트를 추가하고 `ComponentScope` 반환. Undo 등록 |
| `RemoveComponent<T>(int index = 0)` | `AssetEditSession` | 현재 GO에서 n번째 컴포넌트 제거 |

> `GetComponent<T>()`, `GetOrAddComponent<T>()`, `AddComponent<T>()`는 모두 `ComponentScope`를 반환합니다. `AssetEditSession` 세터 체이닝이 필요하면 반환 전 세션 변수를 따로 유지하거나 콜백 패턴을 사용하세요.

### 세터 (Write)

| 메서드 | 설명 |
|--------|------|
| `SetString(string prop, string value)` | `stringValue` 세터 |
| `SetInt(string prop, int value)` | `intValue` 세터 |
| `SetFloat(string prop, float value)` | `floatValue` 세터 |
| `SetBool(string prop, bool value)` | `boolValue` 세터 |
| `SetColor(string prop, Color value)` | `colorValue` 세터 |
| `SetVector3(string prop, Vector3 value)` | `vector3Value` 세터 |
| `SetVector2(string prop, Vector2 value)` | `vector2Value` 세터 |
| `SetRef(string prop, Object value)` | `objectReferenceValue` 세터 |
| `SetProperty(string prop, object value)` | 타입 자동 판별 세터. HTTP IPC `/edit` 엔드포인트용 |

세터를 호출하면 현재 GO의 **모든 컴포넌트를 순회**하며 해당 프로퍼티를 가진 첫 번째 컴포넌트를 찾습니다. `.asset` 파일에서는 SO 에셋에서 직접 탐색합니다. 프로퍼티를 찾지 못하거나 타입이 맞지 않으면 `InvalidOperationException`을 던집니다.

### 값 읽기 (Read)

| 메서드 | 반환 |
|--------|------|
| `GetFloat(string prop)` | `float` |
| `GetString(string prop)` | `string` |
| `GetInt(string prop)` | `int` |
| `GetBool(string prop)` | `bool` |
| `GetColor(string prop)` | `Color` |
| `GetRef<T>(string prop)` | `T?` |

컴포넌트 타입을 명시한 오버로드도 제공합니다:

```csharp
float alpha  = s.GO("Panel").GetFloat<CanvasGroup>("m_Alpha");
Color color  = s.GO("Fill").GetColor<Image>("m_Color");
```

### 저장 / 삭제

| 메서드 | 설명 |
|--------|------|
| `Save()` | 명시적 저장. Dispose 시 자동 저장되므로 생략 가능 |
| `DeleteAsset()` | `.asset` 파일 삭제 후 세션 무효화. `.asset` 모드에서만 사용 가능 |
| `Dispose()` | `using` 블록 종료 시 자동 호출. 미저장 변경사항 저장 + 리소스 정리 |

**저장 동작**

| 에셋 타입 | 저장 방법 |
|----------|----------|
| `.unity` | `EditorSceneManager.SaveScene` |
| `.prefab` | `PrefabUtility.SaveAsPrefabAsset` + `UnloadPrefabContents` |
| `.asset` | `AssetDatabase.SaveAssets` |

### 직렬화 필드명 (SerializedProperty 경로)

Unity 내부 필드명은 C# 프로퍼티명과 다릅니다. 모를 때는 `Sindy/Tools/Field Peeker Window` 사용.

| 컴포넌트 | 프로퍼티 | SO 경로 |
|----------|---------|---------|
| `TextMeshProUGUI` | `text` | `"m_text"` |
| `TextMeshProUGUI` | `fontSize` | `"m_fontSize"` |
| `TextMeshProUGUI` | `color` | `"m_fontColor"` |
| `Image` | `color` | `"m_Color"` |

---

## 4. ComponentScope API

`GetComponent<T>()`, `GetOrAddComponent<T>()`, `AddComponent<T>()`에서 반환되는 컴포넌트 편집 컨텍스트입니다. `SetProperty` / `SetRef` 호출 시 `ApplyModifiedPropertiesWithoutUndo()`가 즉시 실행됩니다.

| 메서드 | 설명 |
|--------|------|
| `SetProperty(string prop, object value)` | 타입 자동 판별 세터. string / bool / int / float / Color / Vector3 / Vector2 지원 |
| `SetRef(string prop, Object value)` | `objectReferenceValue` 세터 |
| `GetProperty<T>(string prop)` | 타입 T로 프로퍼티 읽기 |
| `GetFloat(string prop)` | float 읽기 |
| `GetString(string prop)` | string 읽기 |
| `GetInt(string prop)` | int 읽기 |
| `GetBool(string prop)` | bool 읽기 |
| `GetColor(string prop)` | Color 읽기 |
| `GetRef<TRef>(string prop)` | ObjectReference 읽기 |

```csharp
// 콜백 패턴 — 가장 간결
s.FindGameObject("Icon").GetComponent<Image>(img =>
{
    img.SetProperty("m_Color", new Color(1f, 0.8f, 0.2f, 1f));
    img.SetRef("m_Sprite", mySprite);
});

// 반환값 패턴 — 읽기 또는 추가 작업이 필요할 때
var scope = s.GO("Fill").GetComponent<Image>();
Color current = scope?.GetColor("m_Color") ?? Color.clear;
scope?.SetProperty("m_Color", Color.green);

// AddComponent 후 체이닝
s.Root().CreateGameObject("Overlay")
    .AddComponent<Image>()
    .SetProperty("m_Color", new Color(0f, 0f, 0f, 0.5f))
    .SetRef("m_Sprite", bgSprite);
```

---

## 5. 개별 클래스 레퍼런스

SindyEdit 파사드가 내부적으로 사용하는 저수준 클래스들입니다. 일반적으로는 SindyEdit을 통해 작업하고, 어셈블리 경계 우회나 문자열 타입 지정(`AddComp(string)`) 같은 특수한 경우에만 직접 접근합니다.

### SceneEditor

씬 파일을 열어서 GO를 탐색/생성/수정하는 컨텍스트.

`sealed class SceneEditor : IDisposable`

| 메서드 | 설명 |
|--------|------|
| `Open(string scenePath)` _(static)_ | 씬 열기. 이미 열린 씬 재사용. 실패 시 `null` |
| `GO(string path)` | `.` 구분 경로로 GO 탐색/생성 (없으면 자동 생성) |
| `GOFind(string path)` | `.` 구분 경로로 GO 탐색만. 없으면 `null` + LogWarning |
| `MarkDirty()` | Dispose 시 `SaveScene` 호출 예약. **변경 후 반드시 호출** |
| `Dispose()` | `MarkDirty()` 호출된 경우 씬 자동 저장 |

> `MarkDirty()`를 빠뜨리면 Dispose 시 저장이 일어나지 않습니다.

```csharp
using (var ctx = SceneEditor.Open("Assets/Scenes/MyScene.unity"))
{
    if (ctx == null) return;

    ctx.GO("Canvas.HUD.Title")
       .AddComp<TextMeshProUGUI>()
       .SetStr("m_text", "Hello")
       .Apply();                 // ← 반드시 호출

    ctx.MarkDirty();             // ← 저장 예약
}
```

### GOEditor

특정 GO에 컴포넌트를 추가하거나 SerializedProperty 값을 변경하는 체인 빌더. SceneEditor·PrefabEditor에서 반환받아 사용합니다.

`sealed class GOEditor : IDisposable`

| 메서드 | 설명 |
|--------|------|
| `AddComp<T>()` | 없으면 추가, 있으면 재사용. SerializedObject 편집 대상으로 설정. Undo 등록 |
| `EditComp<T>()` | 기존 컴포넌트를 SerializedObject 편집 대상으로 전환. 없으면 LogWarning |
| `AddComp(string typeFullName)` | 타입 FullName으로 추가 (어셈블리 경계 우회용) |
| `Child(string path)` | 현재 GO 기준 상대 경로 자식 탐색/생성 |
| `ChildFind(string path)` | 현재 GO 기준 탐색만. 없으면 `null` |
| `SetRef / SetStr / SetBool / SetInt / SetFloat / SetColor / SetVector2/3/4` | SerializedProperty 세터 |
| `Apply()` | `ApplyModifiedProperties()` + `SetDirty()`. **체인 마지막에 반드시 호출** |

> `Apply()` 없이 Dispose하면 변경사항이 저장되지 않습니다.

### PrefabEditor

프리팹 파일을 직접 열어 수정하는 컨텍스트.

`sealed class PrefabEditor : IDisposable`

| 메서드 | 설명 |
|--------|------|
| `Open(string assetPath)` _(static)_ | `LoadPrefabContents`로 로드. 실패 시 `null` |
| `GO(string path)` | 루트 기준 경로로 자식 GO 탐색/생성 |
| `GOFind(string path)` | 루트 기준 탐색만. 없으면 `null` |
| `Root()` | 프리팹 루트 GO에 대한 GOEditor 반환 |
| `Dispose()` | `SaveAsPrefabAsset` + `UnloadPrefabContents` 자동 호출 |

### SOEditor\<T\>

ScriptableObject 에셋 편집 컨텍스트.

`sealed class SOEditor<T> : IDisposable where T : ScriptableObject`

| 메서드 | 설명 |
|--------|------|
| `Open(string assetPath)` _(static)_ | 기존 에셋 로드. 실패 시 `null` |
| `Create(string assetPath)` _(static)_ | 새 에셋 생성. 이미 있으면 덮어씀 |
| SetRef / SetStr / SetBool / SetInt / SetFloat / SetColor | GOEditor와 동일한 세터 세트 |
| `Apply()` | `ApplyModifiedProperties()` + `SetDirty()` |
| `Dispose()` | `Apply()` 호출 시 `AssetDatabase.SaveAssets()` |

> SindyEdit의 `NewAsset<T>()`, `Create<T>()`, `Open()` 사용 시 Dispose에서 자동 저장. SOEditor 직접 사용 시 `Apply()` 필수.

### AssetFinder

에셋 경로를 모르거나 동적으로 찾아야 할 때 사용하는 탐색 유틸.

`static class AssetFinder`

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `Prefab<T>(string inFolder = null)` | `T?` | T 컴포넌트를 가진 프리팹의 첫 번째 컴포넌트 반환 |
| `AllPrefabs<T>(string inFolder = null)` | `List<GameObject>` | T 컴포넌트를 가진 프리팹 전체 반환 |
| `Prefab(string typeFullName, string hint = null, ...)` | `Component?` | FullName으로 탐색. hint 이름 포함 프리팹 우선 |
| `PrefabByName(params string[] patterns)` | `GameObject?` | 이름 패턴으로 탐색 (점수 기반 정렬) |
| `Asset<T>(string inFolder = null)` | `T?` | T 타입 SO 에셋 중 첫 번째 반환 |
| `AllAssets<T>(string inFolder = null)` | `List<T>` | T 타입 SO 에셋 전체 반환 |
| `ClearCache()` | `void` | 에디터 세션 캐시 삭제 |

> `Prefab<T>()` 는 T 컴포넌트 자체를 반환합니다 (GameObject 아님). `AllPrefabs<T>()` 는 `List<GameObject>` 반환.

### FieldPeeker

SerializedProperty 경로를 모를 때 확인하는 진단 도구.

- **에디터 메뉴**: `Sindy/Tools/Field Peeker Window` — 컴포넌트 드래그 → 경로 목록 표시 → [복사] 버튼
- **코드에서**: `FieldPeeker.Print<T>(go)` 또는 `FieldPeeker.Print(component)` → Console 출력
- **선택한 GO에서**: `Sindy/Tools/Print Field Names (Selected)`

### BatchEntryPoint

Unity 배치 모드(-batchmode)에서 에디터 작업을 자동화하는 베이스 클래스.

`abstract class BatchEntryPoint`

| 멤버 | 설명 |
|------|------|
| `protected static void RunTask<T>()` | 진입점. Refresh → Execute → 예외 처리 → Exit 자동 |
| `protected abstract void Execute()` | 구현 대상 |
| `protected static void Log/LogError/Success/Fail` | 로그 + 배치 결과 파일 기록 |

배치 결과: `Logs/batch_result.txt` (타임스탬프 포함 요약), `Logs/batch_*.log` (Unity 전체 로그)

### BatchRunner

에디터 스크립트에서 Unity 배치 서브프로세스를 실행하는 헬퍼.

`static class BatchRunner`

| 메서드 | 설명 |
|--------|------|
| `FindUnityPath()` | 현재 버전 Unity 실행 파일 경로 반환 |
| `BuildCommand(string methodName, ...)` | 쉘 명령어 문자열 생성 |
| `Run(string methodName, int timeoutSeconds = 120)` | 배치 서브프로세스 실행 (블로킹). exit code 반환 |

---

## 6. HTTP IPC

Unity 에디터가 열려 있는 상태에서 외부(터미널, AI)가 에디터를 원격 제어합니다.

기본 포트: **6060** (Edit > Preferences > Sindy에서 변경)

컴파일 완료 확인:
```
[SindyCmd] HTTP 서버 시작됨 → http://localhost:6060
```

### /ping — 동작 확인

```bash
curl http://localhost:6060/ping
# {"id":"","success":true,"message":"pong","timestamp":"..."}
```

### /execute — static 메서드 실행

```bash
curl -X POST http://localhost:6060/execute \
  -H "Content-Type: application/json" \
  -d '{"method":"Sindy.Editor.Examples.Example_PrefabEdit.RunBatchEdit"}'
```

- 메서드는 `static`, 인수 없음이어야 함
- `Namespace.TypeName.MethodName` 형식

### /edit — 에셋 프로퍼티 직접 편집

```bash
# 씬 텍스트 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"Assets/Scenes/Main.unity","go":"Canvas/Header/Title","prop":"m_text","value":"Hello"}'

# 이름으로 에셋 자동 탐색 후 색상 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"GaugeBar","go":"Fill/Image","prop":"m_Color","value":[0.2,0.8,0.4,1.0]}'

# SO 필드 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"Assets/Config/Game.asset","prop":"maxHealth","value":200}'
```

**요청 필드**

| 필드 | 필수 | 설명 |
|------|------|------|
| `asset` | ✅ | 에셋 이름(자동 탐색) 또는 전체 경로(`Assets/...`) |
| `go` | — | 씬/프리팹의 GO 경로. `.asset` 편집 시 생략 |
| `prop` | ✅ | SerializedProperty 경로 |
| `value` | ✅ | string / number / bool / float 배열(2=Vector2, 3=Vector3, 4=Color) |

**응답 형식**
```json
{"id":"","success":true,"message":"OK — GaugeBar.GO(Fill/Image).m_Color","timestamp":"..."}
```

---

## 7. 배치 모드

Unity를 headless로 실행해 CI/CD에서 에디터 작업을 자동화합니다.

```bash
"/Applications/Unity/Hub/Editor/6000.0.x/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "$(pwd)" \
  -executeMethod MyTask.Run \
  -quit \
  -logFile "Logs/batch_MyTask.log"
```

```csharp
public class MyTask : BatchEntryPoint
{
    public static void Run() => RunTask<MyTask>();

    protected override void Execute()
    {
        using var s = SindyEdit.Open("Assets/Config/Game.asset");
        if (s == null) throw new Exception("에셋 없음");
        s.SetInt("maxHealth", 300);
        Log("완료");
    }
}
```

---

## 8. 주의사항

### 흔한 실수

- `SindyEdit.Open()` 반환값이 `null`일 수 있습니다. 항상 null 체크 후 사용하세요.
- `.asset` 파일에서 `GO()` 계열을 호출하면 LogWarning 후 무시됩니다. SO 편집은 `GO()` 없이 `SetInt()` 등 직접 호출.
- `Root()` / `FindGameObject()` / `Child()`는 **새 세션을 반환**합니다. 반환값을 변수로 받지 않으면 탐색 결과가 버려집니다.
- 동일한 이름의 프로퍼티가 여러 컴포넌트에 있으면 첫 번째 컴포넌트만 수정됩니다. 특정 컴포넌트를 명시하려면 `GetComponent<T>()` 사용.
- `NewScene()` / `NewPrefab()` / `NewAsset<T>()` 는 파일이 이미 있으면 `InvalidOperationException`을 던집니다. 먼저 `SindyEdit.Exists()` 또는 `SindyEdit.Delete()`로 처리하세요.
- `SceneEditor`를 직접 사용할 때 `MarkDirty()` 없이 Dispose하면 저장 안 됩니다.
- `GOEditor`를 직접 사용할 때 `Apply()` 없이 Dispose하면 변경사항이 사라집니다.

### 에러 대처

| 증상 | 원인 | 대처 |
|------|------|------|
| HTTP 연결 거부 | 에디터 닫힘 / 컴파일 중 | Unity 열려있는지 확인, Console에서 `[SindyCmd]` 로그 확인 |
| `GO를 찾을 수 없습니다` LogWarning | GO 경로 오타 또는 존재하지 않는 경로 | 경로 재확인, `FindGameObject()` 사용 |
| `InvalidOperationException: 프로퍼티 찾을 수 없음` | 필드명 오타 또는 타입 불일치 | FieldPeeker로 정확한 직렬화 경로 확인 |
| `InvalidOperationException: 이미 존재하는 파일` | `NewScene`/`NewPrefab`/`NewAsset` 중복 호출 | `SindyEdit.Exists()` 확인 후 필요 시 `SindyEdit.Delete()` 먼저 실행 |
| `NullReferenceException` | `Open()` null 체크 누락 | `if (s == null) return;` 추가 |

### 배치 모드 필수 규칙

```csharp
// 단독 static 메서드 패턴에서 명시적 종료 필요
if (Application.isBatchMode) EditorApplication.Exit(0);
// BatchEntryPoint.RunTask 사용 시 자동 처리됨

// 배치 모드에서 Dialog 사용 금지
// EditorUtility.DisplayDialog(...)  ← 배치 모드에서 자동 무시됨
```

### PackagePathHelper

예제 코드는 `PackagePathHelper.Resolve()`로 설치 방식에 무관하게 경로를 해결합니다.

| 설치 방식 | 에셋 쓰기 |
|----------|----------|
| Embedded (Assets/ 직접) | ✅ |
| 로컬 참조 | ✅ |
| Git URL | ⚠️ 읽기 전용 (패키지 내부 경로 불가) |
