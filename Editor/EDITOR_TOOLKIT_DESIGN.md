# Editor Toolkit 설계 문서

> 상태: **구현 완료**
> 마지막 업데이트: 2026-04-14

---

## 목차

1. [클래스 구조 및 역할](#1-클래스-구조-및-역할)
2. [공개 API 레퍼런스](#2-공개-api-레퍼런스)
3. [IPC 시스템 (sindy_cmd.sh + EditorCommandWatcher)](#3-ipc-시스템)
4. [배치 모드 시스템 (batch_run.sh + BatchEntryPoint)](#4-배치-모드-시스템)
5. [AI 워크플로우 요약](#5-ai-워크플로우-요약)
6. [직렬화 필드명 레퍼런스](#6-직렬화-필드명-레퍼런스)
7. [확인된 동작 / 미확인 항목](#7-확인된-동작--미확인-항목)
8. [파일 구조](#8-파일-구조)

---

## 1. 클래스 구조 및 역할

| 클래스 | 파일 | 역할 |
|--------|------|------|
| `SceneEditor` | `SceneEditor.cs` | 씬 열기/저장 컨텍스트 래퍼 (IDisposable) |
| `GOEditor` | `GOEditor.cs` | GameObject 계층 탐색 + 컴포넌트 필드 편집 빌더 |
| `SOPropertyHelper` | `GOEditor.cs` (내부) | SerializedProperty 경로 탐색 공유 유틸 |
| `PrefabEditor` | `PrefabEditor.cs` | 프리팹 로드/편집/저장 컨텍스트 래퍼 (IDisposable) |
| `SOEditor<T>` | `SOEditor.cs` | ScriptableObject 편집 컨텍스트 래퍼 (IDisposable) |
| `AssetFinder` | `AssetFinder.cs` | AssetDatabase 기반 프리팹·SO 탐색 유틸 (캐시 포함) |
| `FieldPeeker` | `FieldPeeker.cs` | SerializedProperty 경로 목록 출력 진단 유틸 |
| `FieldPeekerWindow` | `FieldPeeker.cs` | FieldPeeker GUI 버전 EditorWindow |
| `EditorCommandWatcher` | `EditorCommandWatcher.cs` | 파일 기반 IPC 폴링 시스템 `[InitializeOnLoad]` |
| `BatchEntryPoint` | `BatchEntryPoint.cs` | 배치 태스크 베이스 클래스 (예외 처리·종료 자동화) |
| `BatchRunner` | `BatchRunner.cs` | 에디터에서 Unity 배치 서브프로세스 실행 헬퍼 |

### 계층 관계

```
SceneEditor ──┬─→ GOEditor (GetOrCreate / FindOnly)
              └─→ GOEditor (GOFind → null-safe)

PrefabEditor──┬─→ GOEditor (GO / GOFind / Root)
              └─→ GOEditor.Child / ChildFind

SOEditor<T> ──→  SO* 메서드 + Apply() + Dispose(SaveAssets)

AssetFinder ──→  SceneEditor / PrefabEditor 에서 경로 또는 참조를 얻을 때 사용

BatchEntryPoint ←── SetupShowcaseTask (예시 상속)
BatchRunner     ──→  Unity 배치 서브프로세스 실행

EditorCommandWatcher ──→  IPC: Temp/sindy_cmd.json 폴링 → 리플렉션 실행
```

---

## 2. 공개 API 레퍼런스

### 2-1. `SceneEditor`

```csharp
namespace Sindy.Editor.EditorTools

public sealed class SceneEditor : IDisposable
{
    // ── 프로퍼티
    public Scene Scene { get; }

    // ── 팩토리
    /// 씬 열기. 이미 열렸으면 재사용. 사용자 취소 또는 실패 시 null.
    public static SceneEditor Open(string scenePath);

    // ── 계층 탐색
    /// 경로로 GO 탐색/생성 (없으면 자동 생성). "Canvas.Panel.Button" 형식.
    public GOEditor GO(string hierarchyPath);

    /// 경로로 GO 탐색만 (없으면 null + LogWarning).
    public GOEditor GOFind(string hierarchyPath);

    // ── 저장
    /// Dispose 시 SaveScene을 호출하도록 표시.
    public void MarkDirty();

    // ── IDisposable
    /// MarkDirty() 호출된 경우 EditorSceneManager.SaveScene 실행.
    public void Dispose();
}
```

**사용 패턴:**
```csharp
using (var ctx = SceneEditor.Open("Assets/.../MyScene.unity"))
{
    if (ctx == null) return;

    ctx.GO("ShowcaseRunner")
        .AddComp<MyComp>()
        .SOFloat("speed", 5f)
        .Apply();

    ctx.MarkDirty();
}
// Dispose → 자동 씬 저장
```

---

### 2-2. `GOEditor`

```csharp
public sealed class GOEditor : IDisposable
{
    // ── 프로퍼티
    public GameObject GameObject { get; }

    // ── 계층 이동
    /// 현재 GO 기준 상대 경로로 자식 GO 탐색/생성.
    public GOEditor Child(string relativePath);

    /// 현재 GO 기준 상대 경로로 자식 GO 탐색만 (없으면 null).
    public GOEditor ChildFind(string relativePath);

    // ── 컴포넌트 타게팅 (제네릭)
    /// 컴포넌트 추가 또는 재사용. 이후 SO* 대상으로 설정. Undo 등록.
    public GOEditor AddComp<T>() where T : Component;

    /// 기존 컴포넌트를 SO* 대상으로 전환. 없으면 LogWarning + 이전 대상 유지.
    public GOEditor WithComp<T>() where T : Component;

    // ── 컴포넌트 타게팅 (문자열 — 어셈블리 경계 우회용)
    /// 타입 FullName으로 컴포넌트 추가 또는 재사용.
    public GOEditor AddComp(string typeFullName);

    /// 타입 FullName으로 기존 컴포넌트를 SO* 대상으로 전환.
    public GOEditor WithComp(string typeFullName);

    // ── 필드 세터 (모두 GOEditor 반환 — 체이닝 가능)
    public GOEditor SORef     (string path, UnityEngine.Object value, bool ignoreNullWarning = false);
    public GOEditor SOStr     (string path, string value);
    public GOEditor SOBool    (string path, bool value);
    public GOEditor SOInt     (string path, int value);
    public GOEditor SOLong    (string path, long value);
    public GOEditor SOFloat   (string path, float value);
    public GOEditor SODouble  (string path, double value);
    public GOEditor SOEnum    (string path, int value);
    public GOEditor SOColor   (string path, Color value);
    public GOEditor SOVector2 (string path, Vector2 value);
    public GOEditor SOVector3 (string path, Vector3 value);
    public GOEditor SOVector4 (string path, Vector4 value);
    public GOEditor SOQuaternion(string path, Quaternion value);

    // ── 커밋
    /// ApplyModifiedProperties() + SetDirty(). 체인 마지막에 반드시 호출.
    public void Apply();

    // ── IDisposable
    /// Apply() 없이 미저장 변경사항이 있으면 LogWarning 출력.
    public void Dispose();
}
```

**주의사항:**
- `AddComp<T>()` 또는 `WithComp<T>()` 전에 SO* 호출 시 `InvalidOperationException`.
- SO* 경로 실패 시 `Debug.LogError` 후 예외 rethrow.
- `Apply()` 없이 버리면 변경사항 미저장 (IDisposable Dispose 시 LogWarning).

---

### 2-3. `PrefabEditor`

```csharp
public sealed class PrefabEditor : IDisposable
{
    // ── 프로퍼티
    public GameObject RootObject { get; }

    // ── 팩토리
    /// 프리팹 로드 (LoadPrefabContents). 실패 시 null.
    public static PrefabEditor Open(string assetPath);

    // ── 계층 탐색
    /// 루트 기준 상대 경로로 자식 GO 탐색/생성.
    public GOEditor GO(string hierarchyPath);

    /// 루트 기준 상대 경로로 자식 GO 탐색만 (없으면 null).
    public GOEditor GOFind(string hierarchyPath);

    /// 프리팹 루트 GO에 대한 GOEditor 반환.
    public GOEditor Root();

    // ── IDisposable
    /// SaveAsPrefabAsset + UnloadPrefabContents 자동 호출.
    public void Dispose();
}
```

---

### 2-4. `SOEditor<T>`

```csharp
public sealed class SOEditor<T> : IDisposable where T : ScriptableObject
{
    // ── 프로퍼티
    public T Asset { get; }

    // ── 팩토리
    /// 기존 에셋 로드. 실패 시 null.
    public static SOEditor<T> Open(string assetPath);

    /// 새 에셋 생성 (이미 있으면 덮어씀).
    public static SOEditor<T> Create(string assetPath);

    // ── 필드 세터 (모두 SOEditor<T> 반환 — 체이닝 가능)
    public SOEditor<T> SORef     (string path, UnityEngine.Object value, bool ignoreNullWarning = false);
    public SOEditor<T> SOStr     (string path, string value);
    public SOEditor<T> SOBool    (string path, bool value);
    public SOEditor<T> SOInt     (string path, int value);
    public SOEditor<T> SOLong    (string path, long value);
    public SOEditor<T> SOFloat   (string path, float value);
    public SOEditor<T> SODouble  (string path, double value);
    public SOEditor<T> SOEnum    (string path, int value);
    public SOEditor<T> SOColor   (string path, Color value);
    public SOEditor<T> SOVector2 (string path, Vector2 value);
    public SOEditor<T> SOVector3 (string path, Vector3 value);
    public SOEditor<T> SOVector4 (string path, Vector4 value);
    public SOEditor<T> SOQuaternion(string path, Quaternion value);

    // ── 커밋
    /// ApplyModifiedProperties() + SetDirty(). Dispose 시 SaveAssets() 자동 호출.
    public void Apply();

    // ── IDisposable
    /// Apply() 호출 시 AssetDatabase.SaveAssets(). 미적용 변경사항 있으면 LogWarning.
    public void Dispose();
}
```

---

### 2-5. `AssetFinder`



```csharp
public static class AssetFinder
{
    // ── 캐시
    public static void ClearCache();

    // ── 프리팹 탐색
    /// T 컴포넌트를 가진 프리팹의 첫 번째 컴포넌트 반환.
    public static T Prefab<T>(string inFolder = null) where T : Component;

    /// T 컴포넌트를 가진 프리팹 GameObject 전체 반환.
    public static List<GameObject> AllPrefabs<T>(string inFolder = null) where T : Component;

    /// 타입 FullName으로 프리팹 탐색. hint 이름 포함 프리팹을 우선 반환.
    public static Component Prefab(string componentTypeFullName, string hint = null, string inFolder = null);

    /// 이름 패턴으로 프리팹 탐색 (점수 기반 정렬).
    public static GameObject PrefabByName(params string[] patterns);
    public static GameObject PrefabByName(string inFolder, params string[] patterns);

    // ── ScriptableObject 탐색
    /// T 타입 SO 에셋 중 첫 번째 반환.
    public static T Asset<T>(string inFolder = null) where T : ScriptableObject;

    /// T 타입 SO 에셋 전체 반환.
    public static List<T> AllAssets<T>(string inFolder = null) where T : ScriptableObject;
}
```

---

### 2-6. `FieldPeeker`

```csharp
public static class FieldPeeker
{
    // ── MenuItem
    [MenuItem("Sindy/Tools/Print Field Names (Selected)")]
    public static void PrintSelectedComponents();  // 선택된 GO의 모든 컴포넌트 출력

    // ── 코드 API
    public static void Print<T>(GameObject go) where T : Component;
    public static void Print(Component comp);
}

public class FieldPeekerWindow : EditorWindow
{
    [MenuItem("Sindy/Tools/Field Peeker Window")]
    public static void Open();
    // 기능: 컴포넌트 선택 → SO* 경로 목록 표시 → [복사] 버튼 → 필터
}
```

---

### 2-7. `BatchEntryPoint`

```csharp
public abstract class BatchEntryPoint
{
    // ── 진입점 (서브클래스 정적 메서드에서 호출)
    protected static void RunTask<T>() where T : BatchEntryPoint, new();
    // → AssetDatabase.Refresh() → Execute() → Success() or Fail()

    // ── 구현 대상
    protected abstract void Execute();

    // ── 로그 헬퍼
    protected static void Log(string msg);       // Debug.Log + batch_result.txt
    protected static void LogError(string msg);  // Debug.LogError + batch_result.txt

    // ── 종료 (배치 모드에서만 Exit 호출, 에디터 모드에서는 로그만)
    protected static void Success(string msg = null);  // Exit(0)
    protected static void Fail(string msg);            // Exit(1)
}
```

---

### 2-8. `BatchRunner`

```csharp
public static class BatchRunner
{
    /// 현재 버전 Unity 실행 파일 경로 반환 (없으면 Hub 최신 버전).
    public static string FindUnityPath();

    /// 쉘에서 사용할 배치 명령어 문자열 생성.
    public static string BuildCommand(string methodName, string logFilePath = null);

    /// 타임스탬프 기반 로그 파일 경로 반환.
    public static string GetLogFilePath(string methodName = null);

    /// 에디터에서 Unity 배치 서브프로세스 실행 (블로킹). exit code 반환.
    public static int Run(string methodName, int timeoutSeconds = 120);

    // ── MenuItem
    [MenuItem("Sindy/Batch/▶ Show Unity Path")]
    public static void ShowUnityPath();  // 경로 클립보드 복사

    [MenuItem("Sindy/Batch/▶ Copy batch_run.sh Command (BatchTest.Ping)")]
    public static void CopyPingCommand();
}
```

---

## 3. IPC 시스템

Unity 에디터가 열려 있는 상태에서 AI(쉘)가 에디터 메서드를 원격 실행하는 파일 기반 IPC.

### 작동 원리

```
AI/Shell                         Unity Editor (EditorCommandWatcher)
──────────────────────────────   ──────────────────────────────────────
sindy_cmd.sh "Namespace.Type.Method"
  │
  ├─ Temp/sindy_cmd.json 작성
  │    { "method": "...", "id": "abc123" }
  │
  └─ Temp/sindy_result.json 폴링 (1초 간격, 최대 30초)
                                   │
                                   ├─ EditorApplication.update (100ms 폴링)
                                   ├─ sindy_cmd.json 발견 → 파일 즉시 삭제
                                   ├─ 리플렉션: Type.GetMethod → method.Invoke(null, null)
                                   └─ sindy_result.json 작성
                                        { "id": "...", "success": true/false,
                                          "message": "...", "timestamp": "..." }
```

### 사용법

```bash
# 프로젝트 루트에서 실행
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.BatchTest.Ping"
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.SetupShowcaseTask.Run"
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.ReadSceneHierarchy.Execute"
```

### 제약사항

- 메서드는 반드시 **static, 인수 없음** 형식이어야 함
- `Namespace.TypeName.MethodName` 형식 (마지막 `.` 기준으로 타입/메서드 분리)
- Unity 에디터가 열려있고 컴파일 완료 상태여야 함
- 모달 다이얼로그(DisplayDialog)가 열린 상태면 폴링 중단

### 파일 위치

| 파일 | 역할 |
|------|------|
| `Temp/sindy_cmd.json` | AI → Unity 커맨드 (실행 후 자동 삭제) |
| `Temp/sindy_result.json` | Unity → AI 결과 |
| `Temp/sindy_hierarchy.json` | ReadSceneHierarchy 출력 JSON |

---

## 4. 배치 모드 시스템

Unity 에디터가 닫혀있을 때 별도 Unity 프로세스로 작업을 실행하는 headless 모드.

### 사용법

```bash
# 기본
./Tools/batch_run.sh "BatchTest.Ping"

# 타임아웃 지정 (초, 기본 120)
./Tools/batch_run.sh "SetupShowcaseTask.Run" 180
```

### BatchEntryPoint 상속 패턴

```csharp
public class MyTask : BatchEntryPoint
{
    // -executeMethod MyTask.Run 으로 호출됨
    public static void Run() => RunTask<MyTask>();

    protected override void Execute()
    {
        Log("작업 시작");

        using var ctx = SceneEditor.Open("Assets/.../MyScene.unity");
        if (ctx == null) throw new Exception("씬 열기 실패");

        ctx.GO("SomeGO").AddComp<SomeComp>().SOFloat("speed", 5f).Apply();
        ctx.MarkDirty();

        Log("완료");
        // RunTask가 자동으로 Success() → Exit(0)
    }
}
```

### 단독 static 메서드 패턴

```csharp
public static class QuickFix
{
    public static void Run()
    {
        AssetDatabase.Refresh();
        // 작업 수행
        Debug.Log("[QuickFix] 완료");
        EditorApplication.Exit(0); // 반드시 명시적 종료
    }
}
```

### 배치 결과 파일

| 파일 | 내용 |
|------|------|
| `Logs/batch_*.log` | Unity 전체 로그 |
| `Logs/batch_result.txt` | 태스크 요약 (`[LOG]`, `[ERROR]`, `[SUCCESS]`, `[FAIL]` 접두사) |

---

## 5. AI 워크플로우 요약

### 실행 모드 선택

| 상황 | 명령 |
|------|------|
| Unity 에디터 열려 있음 | `bash Tools/sindy_cmd.sh "Namespace.Class.Method"` |
| Unity 에디터 닫혀 있음 | `bash Tools/batch_run.sh "Class.Method"` |

### 작업 종류별 툴 선택

| 작업 | 사용 클래스 |
|------|------------|
| 씬 GO 추가 / 컴포넌트 설정 | `SceneEditor` + `GOEditor` |
| 프리팹 편집 | `PrefabEditor` + `GOEditor` |
| ScriptableObject 값 변경 | `SOEditor<T>` |
| 에셋 경로·참조 탐색 | `AssetFinder` |
| 씬 하이라키 읽기 | `ReadSceneHierarchy.Execute` (IPC) |
| 직렬화 필드명 확인 | `FieldPeeker` 또는 `Sindy/Tools/Field Peeker Window` |

### IPC 실행 시 메서드 등록 요령

AI가 새 작업을 추가할 때:

```csharp
// Editor/Examples/ 에 파일 생성
namespace Sindy.Editor.Examples
{
    public static class MyNewTask
    {
        [MenuItem("Sindy/Batch/▶ My New Task")]
        public static void Execute()
        {
            // 작업 구현
            // ...
            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }
    }
}
```

실행:
```bash
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.MyNewTask.Execute"
```

### 기존 IPC 실행 가능 메서드 목록

| 메서드 | 설명 |
|--------|------|
| `Sindy.Editor.Examples.BatchTest.Ping` | 동작 확인용 Ping |
| `Sindy.Editor.Examples.SetupShowcaseTask.Run` | 쇼케이스 씬 세팅 |
| `Sindy.Editor.Examples.ReadSceneHierarchy.Execute` | 현재 씬 하이라키 JSON 출력 |
| `Sindy.Editor.Examples.Example_SceneEdit.Run` | 씬 편집 예제 A |
| `Sindy.Editor.Examples.Example_PrefabEdit.RunWithAssetFinder` | 프리팹 편집 예제 B (AssetFinder) |
| `Sindy.Editor.Examples.Example_PrefabEdit.RunWithDirectPath` | 프리팹 편집 예제 B (직접 경로) |
| `Sindy.Editor.Examples.Example_PrefabEdit.RunBatchEdit` | 프리팹 일괄 편집 예제 |
| `Sindy.Editor.Examples.Example_SOEdit.CreateAndEdit` | SO 생성 예제 C |
| `Sindy.Editor.Examples.Example_SOEdit.LoadAndEdit` | SO 로드 편집 예제 C |
| `Sindy.Editor.Examples.Example_SOEdit.BatchEditViaAssetFinder` | SO 일괄 편집 예제 C |

---

## 6. 직렬화 필드명 레퍼런스

Unity 빌트인 컴포넌트는 내부 직렬화 필드명이 프로퍼티명과 다르다.

| 컴포넌트 | 프로퍼티 | SO* 경로 |
|----------|---------|---------|
| `TextMeshProUGUI` | `text` | `"m_text"` |
| `TextMeshProUGUI` | `fontSize` | `"m_fontSize"` |
| `TextMeshProUGUI` | `color` | `"m_fontColor"` |
| `Image` | `color` | `"m_Color"` |

**정확한 경로 확인 방법:**
1. `Sindy/Tools/Field Peeker Window` → 컴포넌트 드래그 → 경로 목록 확인
2. 코드: `FieldPeeker.Print<TextMeshProUGUI>(gameObject);`
3. `Sindy/Tools/Print Field Names (Selected)` 메뉴 (Hierarchy에서 GO 선택 후)

---

## 7. 확인된 동작 / 미확인 항목

### 확인됨 (코드 검증)

- [x] `SceneEditor.Open()` — 씬 재사용 / 신규 오픈 / null 반환(실패) 로직
- [x] `SceneEditor.GO()` — `GOEditor.GetOrCreate` 호출, 경로 탐색 + 자동 생성
- [x] `SceneEditor.GOFind()` — `GOEditor.FindOnly` 호출, 없으면 null + LogWarning
- [x] `SceneEditor.MarkDirty()` + `Dispose()` — SaveScene 자동 호출
- [x] `GOEditor.AddComp<T>()` — GetComponent 우선, 없으면 Undo.AddComponent
- [x] `GOEditor.AddComp(string)` — 어셈블리 경계 우회, ObjectFactory.AddComponent
- [x] `GOEditor.Child()` / `ChildFind()` — 현재 GO 기준 상대 경로 탐색
- [x] `GOEditor` SO* 전체 메서드 (13종) 구현됨
- [x] `GOEditor.Dispose()` — Apply() 없이 미저장 시 LogWarning
- [x] `PrefabEditor.Open()` — LoadPrefabContents, 실패 시 null
- [x] `PrefabEditor.Dispose()` — SaveAsPrefabAsset + UnloadPrefabContents 자동
- [x] `PrefabEditor.Root()` — RootObject에 대한 GOEditor 반환
- [x] `SOEditor<T>.Create()` — CreateInstance + CreateAsset
- [x] `SOEditor<T>.Open()` — LoadAssetAtPath, 실패 시 null
- [x] `SOEditor<T>.Dispose()` — Apply() 후 SaveAssets 자동, 미적용 시 LogWarning
- [x] `AssetFinder.AllPrefabs<T>()` — 에디터 세션 캐시 포함

- [x] `AssetFinder.Prefab(string, hint)` — FullName + 이름 힌트 탐색
- [x] `AssetFinder.AllAssets<T>()` — SO 탐색 + 캐시
- [x] `FieldPeeker.Print<T>()` / `FieldPeekerWindow` 구현됨
- [x] `EditorCommandWatcher` — `[InitializeOnLoad]`, 100ms 폴링, 리플렉션 실행, 결과 파일 기록
- [x] `BatchEntryPoint.RunTask<T>()` — AssetDatabase.Refresh + Execute + 예외 처리 + Exit
- [x] `BatchRunner.FindUnityPath()` — 현재 버전 우선, Hub 최신 버전 폴백
- [x] `sindy_cmd.sh` — ID 생성, cmd 파일 작성, 결과 30초 폴링, exit code 반환

### Unity 에디터에서 직접 확인 필요

- [ ] 컴파일 에러 없음 확인 (Unity 에디터 열기)
- [ ] `Sindy/` 메뉴 표시 확인
- [ ] `Sindy/Examples/A - Scene Edit` 실행 결과
- [ ] `Sindy/Examples/B - Prefab Edit` 실행 결과
- [ ] `Sindy/Examples/C - SO Create & Edit` / `C - SO Load & Edit` 실행 결과
- [ ] `Sindy/Tools/Field Peeker Window` 동작 확인
- [ ] `sindy_cmd.sh "Sindy.Editor.Examples.BatchTest.Ping"` IPC 왕복 확인
- [ ] `GOEditor` Apply() 누락 경고 로그 출력 확인
- [ ] `SOEditor` Apply() 누락 경고 로그 출력 확인
- [ ] `GOFind()` 실패 경고 메시지 형식 확인

---

## 8. 파일 구조

```
SindyGamePackage/
├── Tools/
│   ├── sindy_cmd.sh              ← IPC 모드: 에디터 열림 상태에서 실행
│   ├── batch_run.sh              ← 배치 모드: 에디터 닫힘 상태에서 실행
│   └── README_AI_WORKFLOW.md     ← AI 워크플로우 가이드
│
├── Temp/                         ← Unity 자동 관리 (.gitignore)
│   ├── sindy_cmd.json            ← IPC 커맨드 (실행 후 자동 삭제)
│   ├── sindy_result.json         ← IPC 결과
│   └── sindy_hierarchy.json      ← ReadSceneHierarchy 출력
│
├── Logs/
│   ├── batch_*.log               ← Unity 전체 배치 로그
│   └── batch_result.txt          ← 배치 결과 요약
│
└── Assets/sindy-game-package-for-unity/Editor/
    ├── EDITOR_TOOLKIT_DESIGN.md  ← 이 파일
    ├── EditorTools/
    │   ├── SceneEditor.cs         — 씬 열기/저장 컨텍스트
    │   ├── GOEditor.cs            — GO 체인 빌더 + SOPropertyHelper
    │   ├── PrefabEditor.cs        — 프리팹 편집 컨텍스트
    │   ├── SOEditor.cs            — ScriptableObject 편집 컨텍스트
    │   ├── AssetFinder.cs         — 프리팹/SO 탐색 유틸 (캐시)
    │   ├── FieldPeeker.cs         — 직렬화 경로 조회 도구 + EditorWindow
    │   ├── EditorCommandWatcher.cs — IPC 폴링 시스템 [InitializeOnLoad]
    │   ├── BatchEntryPoint.cs     — 배치 태스크 베이스 클래스
    │   └── BatchRunner.cs         — Unity 배치 서브프로세스 실행 헬퍼
    ├── Examples/
    │   ├── Example_SceneEdit.cs   — 예제 A: SceneEditor + GOEditor + AssetFinder
    │   ├── Example_PrefabEdit.cs  — 예제 B: PrefabEditor + GOEditor + AssetFinder
    │   ├── Example_SOEdit.cs      — 예제 C: SOEditor + AssetFinder
    │   └── Example_BatchTask.cs   — 예제 D: BatchTest.Ping, SetupShowcaseTask,
    │                                         ReadSceneHierarchy
    └── Sindy.Editor.asmdef
```
