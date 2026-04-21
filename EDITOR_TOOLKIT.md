# Editor Toolkit

씬·프리팹·ScriptableObject를 메서드 체이닝 + `using` 블록 패턴으로 편집하고, HTTP IPC로 외부에서 원격 실행하기 위한 통합 가이드.

---

## 목차

1. [빠른 시작](#1-빠른-시작)
2. [SindyEdit — 통합 파사드](#2-sindyedit--통합-파사드)
3. [AssetEditSession API](#3-asssteditsession-api)
4. [ComponentEditScope API](#4-componenteditscope-api)
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
s.GOFind("Fill").SetColor("m_Color", Color.green);

// ScriptableObject 편집
using var s = SindyEdit.Open("Assets/Config/Game.asset");
s.SetInt("maxHealth", 200).SetFloat("gravity", 9.81f);

// 이름으로 자동 탐색 (프리팹 → 씬 → SO 순서)
using var s = SindyEdit.Find("GaugeBar");
s.GOFind("Fill").SetColor("m_Color", Color.cyan);
```

---

## 2. SindyEdit — 통합 파사드

`namespace Sindy.Editor.EditorTools`

확장자를 보고 에셋 타입을 자동 판별합니다. `.unity` → SceneEditor, `.prefab` → PrefabEditor, 나머지 → SerializedObject 직접.

### 에셋 열기 / 생성

| 메서드 | 설명 |
|--------|------|
| `Open(string assetPath)` | 경로로 세션 열기. 확장자로 타입 자동 감지. 실패 시 `null` |
| `Find(string nameOrPath)` | 이름으로 에셋 탐색. 프리팹 → 씬 → SO 순서. 경로면 `Open`과 동일 |
| `Create<T>(string assetPath)` | 새 ScriptableObject 생성 후 세션 반환. 디렉터리 없으면 자동 생성 |
| `NewScene(string assetPath)` | 빈 씬 파일 생성 후 세션 반환 |
| `NewPrefab(string assetPath, string rootName = "Root")` | 빈 프리팹 파일 생성 후 세션 반환 |

```csharp
// ScriptableObject 신규 생성
using var s = SindyEdit.Create<FloatVariable>("Assets/Data/Speed.asset");
s.SetFloat("Value", 5f);

// 빈 씬 생성
using var s = SindyEdit.NewScene("Assets/Scenes/Empty.unity");

// 빈 프리팹 생성
using var s = SindyEdit.NewPrefab("Assets/Prefabs/NewButton.prefab", "Button");
s.CreateGO("Label").AddComp<TextMeshProUGUI>();
```

---

## 3. AssetEditSession API

`SindyEdit.Open()` 등이 반환하는 편집 컨텍스트입니다. `IDisposable` — `using` 블록 종료 시 변경사항을 자동 저장합니다.

### GO 탐색

| 메서드 | 설명 |
|--------|------|
| `GO(string goPath)` | 씬 루트 기준 계층 경로로 GO 탐색. `/` 또는 `.` 구분자 허용 |
| `Root()` | 씬의 첫 번째 루트 GO 또는 프리팹 루트 GO로 이동 |
| `GOFind(string name)` | 이름으로 전체 계층 재귀 탐색. 위치를 모를 때 사용 |
| `Child(int index)` | 현재 GO의 인덱스로 직계 자식으로 이동 |
| `Child(string name)` | 현재 GO의 이름으로 직계 자식으로 이동 |

> `.asset` 파일에서 `GO()` 계열 메서드를 호출하면 LogWarning 후 무시됩니다.

**GO() 탐색 방식**
- 씬: 씬 루트 기준 경로. `"Canvas/Panel/Title"` ≡ `"Canvas.Panel.Title"`
- 프리팹: 프리팹 루트의 **자식** 기준 경로
- 탐색 실패 시 LogWarning 출력, 이후 체이닝은 무시됨

### GO 생성 / 삭제

| 메서드 | 설명 |
|--------|------|
| `CreateGO(string name)` | 현재 GO의 자식으로 새 GO 생성. `_currentGO` null이면 씬/프리팹 루트에 생성. 생성 후 컨텍스트가 새 GO로 이동 |
| `DeleteGO()` | 현재 GO 삭제. 부모로 컨텍스트 이동 (부모 없으면 null) |

### 컴포넌트

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `GetComp<T>()` | `T?` | 현재 GO에서 컴포넌트 가져오기. 없으면 null |
| `AddComp<T>()` | `AssetEditSession` | 없을 때만 추가. Undo 등록 |
| `RemoveComp<T>()` | `AssetEditSession` | 현재 GO에서 컴포넌트 제거 |
| `EditComp<T>(Action<ComponentEditScope>)` | `AssetEditSession` | 콜백에서 특정 컴포넌트 직접 편집. 콜백 후 자동 Apply |

### SO 세터 (Write)

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
| `Set(string prop, object value)` | 타입 자동 판별 세터. HTTP IPC `/edit` 엔드포인트용 |

SO 세터를 호출하면 현재 GO의 **모든 컴포넌트를 순회**하며 해당 프로퍼티를 가진 첫 번째 컴포넌트를 찾습니다. `.asset` 파일에서는 SO 에셋에서 직접 탐색합니다.

### 값 읽기 (Read)

| 메서드 | 반환 | 기본값 |
|--------|------|--------|
| `GetFloat(string prop)` | `float` | `0f` |
| `GetString(string prop)` | `string` | `""` |
| `GetInt(string prop)` | `int` | `0` |
| `GetBool(string prop)` | `bool` | `false` |
| `GetColor(string prop)` | `Color` | `Color.clear` |

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

## 4. ComponentEditScope API

`EditComp<T>(action)` 콜백에서 사용하는 컴포넌트 편집 컨텍스트입니다. 콜백 종료 후 `ApplyModifiedPropertiesWithoutUndo()`가 자동 호출됩니다.

| 메서드 | 설명 |
|--------|------|
| `Set(string prop, object value)` | 타입 자동 판별 세터. string / bool / int / float / Color / Vector3 / Vector2 지원 |
| `SetRef(string prop, Object value)` | `objectReferenceValue` 세터 |

```csharp
s.GOFind("Icon").EditComp<Image>(img =>
{
    img.Set("m_Color", new Color(1f, 0.8f, 0.2f, 1f));
    img.SetRef("m_Sprite", mySprite);
});
```

---

## 5. 개별 클래스 레퍼런스

SindyEdit이 내부적으로 위임하는 클래스들입니다. 복잡한 시나리오(어셈블리 경계 우회, `AddComp(string)` 등)에서 직접 사용합니다.

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
       .SOStr("m_text", "Hello")
       .Apply();                 // ← 반드시 호출

    ctx.MarkDirty();             // ← 저장 예약
}
```

### GOEditor

특정 GO에 컴포넌트를 추가하거나 SerializedProperty 값을 변경하는 체인 빌더. SceneEditor·PrefabEditor에서 반환받아 사용합니다.

`sealed class GOEditor : IDisposable`

| 메서드 | 설명 |
|--------|------|
| `AddComp<T>()` | 없으면 추가, 있으면 재사용. SO* 대상 설정. Undo 등록 |
| `EditComp<T>()` | 기존 컴포넌트를 SO* 대상으로 전환. 없으면 LogWarning |
| `AddComp(string typeFullName)` | 타입 FullName으로 추가 (어셈블리 경계 우회용) |
| `Child(string path)` | 현재 GO 기준 상대 경로 자식 탐색/생성 |
| `ChildFind(string path)` | 현재 GO 기준 탐색만. 없으면 `null` |
| `SetRef / SOStr / SetBool / SetInt / SetFloat / SODouble / SOLong / SOEnum / SetColor / SetVector2/3/4 / SOQuaternion` | SerializedProperty 세터 |
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
| SO* 세터 | GOEditor와 동일 세트 |
| `Apply()` | `ApplyModifiedProperties()` + `SetDirty()` |
| `Dispose()` | `Apply()` 호출 시 `AssetDatabase.SaveAssets()` |

> SindyEdit의 `Create<T>()`, `Open()` 사용 시 Dispose에서 자동 저장. SOEditor 직접 사용 시 `Apply()` 필수.

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
- 동일한 이름의 프로퍼티가 여러 컴포넌트에 있으면 첫 번째 컴포넌트만 수정됩니다. 특정 컴포넌트를 명시하려면 `EditComp<T>()` 사용.
- `SceneEditor`를 직접 사용할 때 `MarkDirty()` 없이 Dispose하면 저장 안 됩니다.
- `GOEditor`를 직접 사용할 때 `Apply()` 없이 Dispose하면 변경사항이 사라집니다.

### 에러 대처

| 증상 | 원인 | 대처 |
|------|------|------|
| HTTP 연결 거부 | 에디터 닫힘 / 컴파일 중 | Unity 열려있는지 확인, Console에서 `[SindyCmd]` 로그 확인 |
| `GO를 찾을 수 없습니다` LogWarning | GO 경로 오타 또는 존재하지 않는 경로 | 경로 재확인, `GOFind()` 사용 |
| Property 찾을 수 없음 | 필드명 오타 | FieldPeeker로 정확한 직렬화 경로 확인 |
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
