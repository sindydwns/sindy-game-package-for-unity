# Editor Toolkit Guide

Unity 에디터 스크립트를 **메서드 체이닝 + IDisposable 컨텍스트** 패턴으로 작성하고, 셸/AI에서 원격으로 실행하기 위한 통합 가이드.

---

## 빠른 시작

> 지금 당장 실행해서 동작을 확인하고 싶을 때.

### 에디터가 열려 있을 때 — IPC 모드

```bash
# 동작 확인 Ping
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.BatchTest.Ping"

# 씬 하이라키 읽기 → Temp/sindy_hierarchy.json 생성
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.ReadSceneHierarchy.Execute"

# 쇼케이스 씬 세팅
bash Tools/sindy_cmd.sh "Sindy.Editor.Examples.SetupShowcaseTask.Run"
```

### 에디터가 닫혀 있을 때 — 배치 모드

```bash
# 기본 실행
./Tools/batch_run.sh "MyTask.Run"

# 타임아웃 지정 (초, 기본 120)
./Tools/batch_run.sh "MyTask.Run" 180
```

---

## 실행 모드 선택

> 어떤 스크립트로 실행할지 판단할 때.

| 상황 | 명령 | 특징 |
|------|------|------|
| 에디터 열려 있음 | `bash Tools/sindy_cmd.sh "Namespace.Class.Method"` | 에디터 상태 유지, 씬 저장·Undo·UI 반영 가능 |
| 에디터 닫혀 있음 | `bash Tools/batch_run.sh "Class.Method"` | headless 실행, CI/CD에 적합 |

**IPC 모드 제약사항**
- 메서드는 `static`, 인수 없음 형식이어야 함
- `Namespace.TypeName.MethodName` 형식 (마지막 `.` 기준으로 타입/메서드 분리)
- Unity 에디터가 열려 있고 컴파일 완료 상태여야 함

---

## 클래스 레퍼런스

### SceneEditor

> 씬을 열고, GameObject를 탐색/생성하고, 저장할 때.

`namespace Sindy.Editor.EditorTools` · `sealed class SceneEditor : IDisposable`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string scenePath` | `SceneEditor?` | 씬을 열거나 이미 열린 씬을 재사용. 실패 시 `null` |
| `GO` | `string hierarchyPath` | `GOEditor` | 점(`.`) 구분 계층 경로로 GO 탐색/생성 (없으면 자동 생성) |
| `GOFind` | `string hierarchyPath` | `GOEditor?` | 점(`.`) 구분 계층 경로로 GO 탐색만. 없으면 `null` + LogWarning |
| `MarkDirty` | — | `void` | Dispose 시 `EditorSceneManager.SaveScene` 호출 예약 |
| `Dispose` | — | `void` | `MarkDirty()` 호출된 경우 씬 자동 저장 |

---

### GOEditor

> GameObject에 컴포넌트를 추가하고 SerializedProperty를 수정할 때.

`namespace Sindy.Editor.EditorTools` · `sealed class GOEditor : IDisposable`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `AddComp<T>` | — | `GOEditor` | 컴포넌트 없으면 추가, 있으면 재사용. SO* 대상으로 설정. Undo 등록 |
| `WithComp<T>` | — | `GOEditor` | 기존 컴포넌트를 SO* 대상으로 전환. 없으면 LogWarning |
| `AddComp` | `string typeFullName` | `GOEditor` | 타입 FullName으로 컴포넌트 추가/재사용 (어셈블리 경계 우회용) |
| `WithComp` | `string typeFullName` | `GOEditor` | 타입 FullName으로 기존 컴포넌트를 SO* 대상으로 전환 |
| `Child` | `string relativePath` | `GOEditor` | 현재 GO 기준 상대 경로로 자식 GO 탐색/생성 |
| `ChildFind` | `string relativePath` | `GOEditor?` | 현재 GO 기준 상대 경로로 자식 GO 탐색만. 없으면 `null` |
| `SORef` | `string path, Object value, bool ignoreNullWarning = false` | `GOEditor` | `objectReferenceValue` 세터 |
| `SOStr` | `string path, string value` | `GOEditor` | `stringValue` 세터 |
| `SOBool` | `string path, bool value` | `GOEditor` | `boolValue` 세터 |
| `SOInt` | `string path, int value` | `GOEditor` | `intValue` 세터 |
| `SOFloat` | `string path, float value` | `GOEditor` | `floatValue` 세터 |
| `SODouble` | `string path, double value` | `GOEditor` | `doubleValue` 세터 |
| `SOLong` | `string path, long value` | `GOEditor` | `longValue` 세터 |
| `SOEnum` | `string path, int value` | `GOEditor` | `enumValueIndex` 세터 |
| `SOColor` | `string path, Color value` | `GOEditor` | `colorValue` 세터 |
| `SOVector2` | `string path, Vector2 value` | `GOEditor` | `vector2Value` 세터 |
| `SOVector3` | `string path, Vector3 value` | `GOEditor` | `vector3Value` 세터 |
| `SOVector4` | `string path, Vector4 value` | `GOEditor` | `vector4Value` 세터 |
| `SOQuaternion` | `string path, Quaternion value` | `GOEditor` | `quaternionValue` 세터 |
| `Apply` | — | `void` | `ApplyModifiedProperties()` + `SetDirty()`. **체인 마지막에 반드시 호출** |
| `Dispose` | — | `void` | `Apply()` 없이 미저장 변경사항 있으면 LogWarning |

> `AddComp<T>()` 또는 `WithComp<T>()` 호출 전에 SO* 메서드를 호출하면 `InvalidOperationException` 발생.

---

### PrefabEditor

> 프리팹 파일을 열고, 수정하고, 저장할 때.

`namespace Sindy.Editor.EditorTools` · `sealed class PrefabEditor : IDisposable`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string assetPath` | `PrefabEditor?` | `LoadPrefabContents`로 프리팹 로드. 실패 시 `null` |
| `GO` | `string hierarchyPath` | `GOEditor` | 루트 기준 상대 경로로 자식 GO 탐색/생성 |
| `GOFind` | `string hierarchyPath` | `GOEditor?` | 루트 기준 상대 경로로 자식 GO 탐색만. 없으면 `null` |
| `Root` | — | `GOEditor` | 프리팹 루트 GO에 대한 GOEditor 반환 |
| `Dispose` | — | `void` | `SaveAsPrefabAsset` + `UnloadPrefabContents` 자동 호출 |

---

### SOEditor\<T\>

> ScriptableObject 에셋을 생성하거나 값을 수정할 때.

`namespace Sindy.Editor.EditorTools` · `sealed class SOEditor<T> : IDisposable where T : ScriptableObject`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string assetPath` | `SOEditor<T>?` | 기존 에셋 로드. 실패 시 `null` |
| `Create` (static) | `string assetPath` | `SOEditor<T>` | 새 에셋 생성. 이미 있으면 덮어씀 |
| SO* 세터 | (GOEditor와 동일) | `SOEditor<T>` | `SORef`, `SOStr`, `SOBool`, `SOInt`, `SOFloat`, `SODouble`, `SOLong`, `SOEnum`, `SOColor`, `SOVector2/3/4`, `SOQuaternion` |
| `Apply` | — | `void` | `ApplyModifiedProperties()` + `SetDirty()` |
| `Dispose` | — | `void` | `Apply()` 호출 시 `AssetDatabase.SaveAssets()`. 미적용 변경사항 있으면 LogWarning |

---

### AssetFinder

> 에셋/프리팹 경로를 모를 때 타입이나 이름으로 탐색할 때.

`namespace Sindy.Editor.EditorTools` · `static class AssetFinder`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Prefab<T>` | `string inFolder = null` | `T?` | T 컴포넌트를 가진 프리팹의 첫 번째 컴포넌트 반환 |
| `AllPrefabs<T>` | `string inFolder = null` | `List<GameObject>` | T 컴포넌트를 가진 프리팹 전체 반환 |
| `Prefab` | `string componentTypeFullName, string hint = null, string inFolder = null` | `Component?` | 타입 FullName으로 탐색. `hint` 이름 포함 프리팹 우선 반환 |
| `PrefabByName` | `params string[] patterns` | `GameObject?` | 이름 패턴으로 프리팹 탐색 (점수 기반 정렬) |
| `PrefabByName` | `string inFolder, params string[] patterns` | `GameObject?` | 지정 폴더 내에서 이름 패턴으로 탐색 |
| `Asset<T>` | `string inFolder = null` | `T?` | T 타입 SO 에셋 중 첫 번째 반환 |
| `AllAssets<T>` | `string inFolder = null` | `List<T>` | T 타입 SO 에셋 전체 반환 |
| `ClearCache` | — | `void` | 에디터 세션 캐시 전체 삭제 |

---

### FieldPeeker

> 컴포넌트의 직렬화 필드명(SerializedProperty path)을 확인할 때.

- **메뉴**: `Sindy/Tools/Field Peeker Window` — 컴포넌트 드래그 → 경로 목록 표시 → [복사] 버튼
- **코드에서**: `FieldPeeker.Print<T>(go)` 또는 `FieldPeeker.Print(component)` → Console 출력

Unity 빌트인 컴포넌트 주요 필드명:

| 컴포넌트 | 프로퍼티 | 직렬화 필드명 |
|----------|---------|--------------|
| TextMeshProUGUI | text | `m_text` |
| TextMeshProUGUI | fontSize | `m_fontSize` |
| TextMeshProUGUI | color | `m_fontColor` |
| Image | color | `m_Color` |

---

## 코드 예제

### 씬 편집

```csharp
using (var ctx = SceneEditor.Open("Assets/Scenes/MyScene.unity"))
{
    if (ctx == null) return;
    ctx.GO("Canvas.HUD.Title")
       .AddComp<TextMeshProUGUI>()
       .SOStr("m_text", "Hello World")
       .SOFloat("m_fontSize", 32f)
       .Apply();
    ctx.MarkDirty();
}
// Dispose → 씬 자동 저장
```

### 프리팹 편집

```csharp
using (var p = PrefabEditor.Open("Assets/Prefabs/MyButton.prefab"))
{
    if (p == null) return;
    p.Root().WithComp<Image>().SOColor("m_Color", Color.cyan).Apply();
    p.GO("Label").AddComp<TextMeshProUGUI>().SOStr("m_text", "Click").Apply();
}
// Dispose → SaveAsPrefabAsset + UnloadPrefabContents 자동 호출
```

### SO 편집

```csharp
// 기존 에셋 수정
using (var so = SOEditor<FloatVariable>.Open("Assets/Data/Speed.asset"))
{
    if (so == null) return;
    so.SOFloat("Value", 9.8f).Apply();
}

// 새 에셋 생성
using (var so = SOEditor<FloatVariable>.Create("Assets/Data/NewSpeed.asset"))
{
    so.SOFloat("Value", 5f).SOStr("description", "초기 속도").Apply();
}
```

### 배치 태스크 작성 — BatchEntryPoint 상속 (권장)

> 예외 처리·AssetDatabase.Refresh·종료 코드를 자동으로 처리해야 할 때.

```csharp
// Editor/Examples/ 또는 Editor/SceneEditor/ 에 파일 생성
public class MyTask : BatchEntryPoint
{
    // Unity -executeMethod MyTask.Run 으로 호출됨
    public static void Run() => RunTask<MyTask>();

    protected override void Execute()
    {
        Log("작업 시작");

        using var ctx = SceneEditor.Open("Assets/.../MyScene.unity");
        if (ctx == null) throw new Exception("씬을 열 수 없음");

        ctx.GO("SomeObject")
           .AddComp<SomeComponent>()
           .SOFloat("speed", 5f)
           .Apply();

        ctx.MarkDirty();
        Log("완료");
        // RunTask가 자동으로 AssetDatabase.Refresh + Exit(0) 처리
    }
}
```

### 배치 태스크 작성 — 단독 static 메서드 (간단한 작업)

```csharp
public static class QuickFix
{
    [MenuItem("Sindy/Batch/▶ Quick Fix")]
    public static void Run()
    {
        AssetDatabase.Refresh(); // 단독 패턴은 직접 호출 필요

        // 작업 수행
        Debug.Log("[QuickFix] 완료");

        if (Application.isBatchMode) EditorApplication.Exit(0); // 배치 모드에서만 종료
    }
}
```

---

## IPC 실행 가능 메서드 목록

> `sindy_cmd.sh`로 바로 호출할 수 있는 등록된 메서드 목록.

| 클래스 | 전체 경로 | 설명 |
|--------|-----------|------|
| `BatchTest` | `Sindy.Editor.Examples.BatchTest.Ping` | 동작 확인용 Ping. Unity 버전·플랫폼 정보 출력 |
| `SetupShowcaseTask` | `Sindy.Editor.Examples.SetupShowcaseTask.Run` | 쇼케이스 씬 자동 세팅 |
| `ReadSceneHierarchy` | `Sindy.Editor.Examples.ReadSceneHierarchy.Execute` | 현재 씬 하이라키를 `Temp/sindy_hierarchy.json`에 저장 |
| `Example_SceneEdit` | `Sindy.Editor.Examples.Example_SceneEdit.Run` | SceneEditor + GOEditor + AssetFinder 예제 |
| `Example_PrefabEdit` | `Sindy.Editor.Examples.Example_PrefabEdit.RunWithAssetFinder` | PrefabEditor + AssetFinder 조합 예제 |
| `Example_PrefabEdit` | `Sindy.Editor.Examples.Example_PrefabEdit.RunWithDirectPath` | PrefabEditor 직접 경로 지정 예제 |
| `Example_PrefabEdit` | `Sindy.Editor.Examples.Example_PrefabEdit.RunBatchEdit` | 프리팹 일괄 편집 예제 |
| `Example_SOEdit` | `Sindy.Editor.Examples.Example_SOEdit.CreateAndEdit` | SOEditor 생성 예제 |
| `Example_SOEdit` | `Sindy.Editor.Examples.Example_SOEdit.LoadAndEdit` | SOEditor 로드 편집 예제 |
| `Example_SOEdit` | `Sindy.Editor.Examples.Example_SOEdit.BatchEditViaAssetFinder` | SOEditor + AssetFinder 일괄 편집 예제 |

---

## 에디터 메뉴

> 에디터가 열려 있을 때 메뉴에서도 실행 가능.

- `Sindy/Batch/▶ Ping (BatchTest)` — 배치 시스템 동작 확인
- `Sindy/Batch/▶ Setup Showcase Scene` — 쇼케이스 씬 세팅
- `Sindy/Batch/▶ Show Unity Path` — Unity 실행 파일 경로 확인 (클립보드 복사)
- `Sindy/Tools/Field Peeker Window` — 직렬화 필드명 확인 창

---

## 주의사항

### 배치 모드 필수 규칙

```csharp
// ✅ 변경 후 씬 저장 예약
ctx.MarkDirty();      // SceneEditor
// PrefabEditor, SOEditor는 Dispose 시 자동 저장

// ✅ 단독 static 메서드 패턴에서 명시적 종료 필요
EditorApplication.Exit(0); // 성공
EditorApplication.Exit(1); // 실패
// BatchEntryPoint.RunTask 사용 시 자동 처리됨
// ⚠️ 이 호출이 없으면 Unity가 종료되지 않아 타임아웃 발생

// ⚠️ 배치 모드에서 Dialog 사용 금지
// EditorUtility.DisplayDialog(...)  ← 배치 모드에서 자동 무시됨
```

### 실패 원인별 대처

| 원인 | 대처 |
|------|------|
| 씬/에셋 경로 오류 | `AssetFinder`로 경로 재확인 |
| 타입/필드명 오류 | `Field Peeker Window`로 직렬화 필드명 확인 |
| 컴파일 에러 | 로그에서 `error CS` 검색 후 수정 |
| 타임아웃 | 두 번째 인자로 타임아웃 값 늘리기 |

---

## 내부 구조

### IPC 동작 흐름

```
셸 (sindy_cmd.sh)                   Unity Editor (EditorCommandWatcher)
─────────────────────────────────   ──────────────────────────────────────
1. Temp/sindy_cmd.json 작성
   {"method":"...","id":"abc123"}
2. Temp/sindy_result.json 폴링
   (1초 간격, 최대 30초)              3. EditorApplication.update (100ms 폴링)
                                      4. sindy_cmd.json 발견 → 즉시 삭제
                                      5. 리플렉션으로 메서드 실행
                                      6. sindy_result.json 작성
                                         {"id":"abc123","success":true,...}
7. 결과 출력 후 exit 0/1
```

### 배치 모드 로그 확인

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  배치 결과 요약
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
12:34:56  [LOG] 작업 시작
12:34:57  [LOG] 완료
12:34:57  [SUCCESS] [MyTask] 태스크 완료

  ✅ 성공  (exit code: 0)
```

```bash
# 실패 시 전체 로그 검색
cat Logs/batch_MyTask_Run_20250101_123456.log | grep -E "Error|Exception|FAIL"
```

### 파일 구조

```
SindyGamePackage/
├── Tools/
│   ├── sindy_cmd.sh              ← IPC 모드 실행 스크립트
│   └── batch_run.sh              ← 배치 모드 실행 스크립트
├── Temp/                         ← Unity 자동 관리 (.gitignore 포함)
│   ├── sindy_cmd.json            ← IPC 커맨드 파일 (실행 후 삭제됨)
│   └── sindy_result.json         ← IPC 결과 파일
├── Logs/
│   ├── batch_*.log               ← Unity 전체 로그 (자동 생성)
│   └── batch_result.txt          ← 배치 결과 요약 (자동 생성)
└── Assets/sindy-game-package-for-unity/Editor/
    ├── SceneEditor/
    │   ├── EditorCommandWatcher.cs ← IPC 폴링 시스템 [InitializeOnLoad]
    │   ├── BatchEntryPoint.cs    ← 배치 태스크 베이스 클래스
    │   ├── SceneEditor.cs
    │   ├── GOEditor.cs
    │   ├── PrefabEdit.cs
    │   ├── SOEdit.cs
    │   └── AssetFinder.cs
    └── Examples/
        ├── Example_BatchTask.cs
        ├── Example_SceneEdit.cs
        ├── Example_PrefabEdit.cs
        └── Example_SOEdit.cs
```
