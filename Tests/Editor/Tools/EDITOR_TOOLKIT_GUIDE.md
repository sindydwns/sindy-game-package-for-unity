# Editor Toolkit Guide

Unity 에디터 스크립트를 **메서드 체이닝 + IDisposable 컨텍스트** 패턴으로 작성하고, HTTP/AI에서 원격으로 실행하기 위한 통합 가이드.

> 모든 경로는 Unity 프로젝트 루트(`SindyGamePackage/`)를 기준으로 표기합니다.

---

## UPM 설치 방법

### 1. Embedded (현재 방식)

패키지 폴더를 프로젝트 `Assets/` 안에 직접 복사합니다.

```
YourProject/Assets/sindy-game-package-for-unity/
```

### 2. 로컬 참조 (Local file reference)

`Packages/manifest.json`의 `dependencies`에 추가합니다:

```json
{
  "dependencies": {
    "com.sindy": "file:../../path/to/SindyGamePackage/Assets/sindy-game-package-for-unity"
  }
}
```

> 경로는 `Packages/` 폴더 기준 상대 경로입니다.

설치 후 패키지 위치: `Packages/com.sindy/`

### 3. Git URL / Registry

Package Manager → **Add package from git URL**:
```
https://github.com/your-repo/SindyGamePackage.git?path=Assets/sindy-game-package-for-unity
```

설치 후 패키지 위치: `Library/PackageCache/com.sindy@1.0.0-alpha.18/`

---

## 빠른 시작

> 지금 당장 실행해서 동작을 확인하고 싶을 때.

### 1. HTTP 방식 (메인 — 에디터가 열려 있을 때)

1. **Unity 에디터에서 프로젝트 열기**
   `SindyGamePackage/` 폴더를 Unity Hub에서 열거나 더블클릭으로 실행

2. **컴파일 완료 확인**
   Unity Console에 다음 로그가 보이면 준비 완료:
   ```
   [SindyCmd] HTTP 서버 시작됨 → http://localhost:6060
   ```
   > 컴파일 중에는 아무 응답이 없습니다. 하단 상태바의 스피너가 멈출 때까지 대기.

3. **Ping 테스트로 동작 확인**
   ```bash
   curl -s http://localhost:6060/ping
   ```
   `{"id":"","success":true,"message":"pong",...}` 출력 확인.

4. **메서드 실행 예시**
   ```bash
   # 동작 확인
   curl -s http://localhost:6060/execute \
     -H "Content-Type: application/json" \
     -d '{"method":"Sindy.Editor.Examples.BatchTest.Ping"}'

   # 씬 하이라키 읽기 → Temp/sindy_hierarchy.json 생성
   curl -s http://localhost:6060/execute \
     -H "Content-Type: application/json" \
     -d '{"method":"Sindy.Editor.Examples.ReadSceneHierarchy.Execute"}'

   # 쇼케이스 씬 세팅
   curl -s http://localhost:6060/execute \
     -H "Content-Type: application/json" \
     -d '{"method":"Sindy.Editor.Examples.SetupShowcaseTask.Run"}'

   # ── /edit 엔드포인트 (SindyEdit 파사드 직접 사용) ──

   # 프리팹 색상 변경 (이름으로 탐색)
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"GaugeBar","go":"Fill/Image","prop":"m_Color","value":[0.2,0.8,0.4,1.0]}'

   # 씬 텍스트 변경 (전체 경로 지정)
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"Assets/Scenes/Main.unity","go":"Canvas/Header/Title","prop":"m_text","value":"Hello"}'

   # ScriptableObject 정수 필드 변경
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"Assets/Config/Game.asset","prop":"maxHealth","value":200}'

   # float 필드 변경
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"Assets/Config/Game.asset","prop":"gravity","value":9.81}'

   # bool 필드 변경
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"Assets/Config/Game.asset","prop":"debugMode","value":true}'

   # Vector3 변경 (3개 float 배열)
   curl -s http://localhost:6060/edit \
     -H "Content-Type: application/json" \
     -d '{"asset":"Assets/Prefabs/Player.prefab","go":"Body","prop":"m_LocalPosition","value":[0,1,0]}'
   ```

   **응답 형식** (`/execute`와 동일):
   ```json
   {"id":"","success":true,"message":"OK — GaugeBar.GO(Fill/Image).m_Color","timestamp":"..."}
   ```

   **`/edit` 요청 필드 설명:**

   | 필드 | 타입 | 필수 | 설명 |
   |------|------|------|------|
   | `asset` | string | ✅ | 에셋 이름(자동 탐색) 또는 전체 경로(`Assets/...`) |
   | `go` | string | — | 씬/프리팹의 GO 계층 경로. `.asset` 편집 시 생략 |
   | `prop` | string | ✅ | 편집할 SerializedProperty 경로 |
   | `value` | any | ✅ | 값. string / number / bool / float 배열(2=Vector2, 3=Vector3, 4=Color) |

포트 변경: Unity 메뉴 → **Edit > Preferences > Sindy**

### 2. 배치 모드 (에디터가 닫혀 있을 때)

Unity를 직접 배치 모드로 실행합니다. Unity 실행 파일 경로는 에디터 메뉴 `Sindy/Batch/▶ Show Unity Path`로 확인하거나, `BatchRunner.BuildCommand()`로 명령어 문자열을 생성할 수 있습니다.

```bash
# 직접 실행 예시 (Unity 경로는 환경에 따라 다름)
"/Applications/Unity/Hub/Editor/6000.0.x/Unity.app/Contents/MacOS/Unity" \
  -batchmode \
  -projectPath "$(pwd)" \
  -executeMethod MyTask.Run \
  -quit \
  -logFile "Logs/batch_MyTask.log"
```

---

## 실행 모드 선택

> 어떤 방식으로 실행할지 판단할 때.

| 상황 | 방식 | 특징 |
|------|------|------|
| 에디터 열려 있음 | `curl http://localhost:6060/execute` | 에디터 상태 유지, 씬 저장·Undo·UI 반영 가능 |
| 에디터 닫혀 있음 | Unity `-batchmode -executeMethod` | headless 실행, CI/CD에 적합 |

**HTTP 방식 제약사항**
- 메서드는 `static`, 인수 없음 형식이어야 함
- `Namespace.TypeName.MethodName` 형식 (마지막 `.` 기준으로 타입/메서드 분리)
- Unity 에디터가 열려 있고 컴파일 완료 상태여야 함

---

## 클래스 레퍼런스

### SceneEditor

> **이럴 때 쓴다**: 씬 파일을 열어서 GameObject 추가·수정·저장해야 할 때. 에디터 열린 상태에서 씬을 직접 수정하므로 Undo·저장 상태가 에디터에 즉시 반영됨.
> - 새 UI 요소를 씬에 자동으로 배치할 때
> - CI/배치 스크립트로 씬 초기 설정을 자동화할 때
> - 여러 씬을 순회하며 특정 GO 구조를 일괄 수정할 때

`namespace Sindy.Editor.EditorTools` · `sealed class SceneEditor : IDisposable`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string scenePath` | `SceneEditor?` | 씬을 열거나 이미 열린 씬을 재사용. 실패 시 `null` |
| `GO` | `string hierarchyPath` | `GOEditor` | 점(`.`) 구분 계층 경로로 GO 탐색/생성 (없으면 자동 생성) |
| `GOFind` | `string hierarchyPath` | `GOEditor?` | 점(`.`) 구분 계층 경로로 GO 탐색만. 없으면 `null` + LogWarning |
| `MarkDirty` | — | `void` | Dispose 시 `EditorSceneManager.SaveScene` 호출 예약 |
| `Dispose` | — | `void` | `MarkDirty()` 호출된 경우 씬 자동 저장 |

**흔한 실수**
- `MarkDirty()` 를 빠뜨리면 Dispose 시 저장이 일어나지 않음. 변경 후 반드시 호출.
- `GO("Canvas.Panel")` 은 없으면 자동 생성하므로, 탐색만 원하면 `GOFind` 사용.

**메서드 체이닝 예제**

```csharp
using (var ctx = SceneEditor.Open("Assets/Scenes/MyScene.unity"))
{
    if (ctx == null) return;                        // 사용자 취소 또는 경로 오류

    // 계층 경로로 GO 접근 — 없으면 자동 생성
    ctx.GO("Canvas.HUD.Title")
       .AddComp<TextMeshProUGUI>()
       .SOStr("m_text", "Hello World")
       .SOFloat("m_fontSize", 32f)
       .SOColor("m_fontColor", Color.white)
       .Apply();                                    // ← 반드시 호출

    // 자식 GO 이동 후 별도 컴포넌트 설정
    ctx.GO("Canvas.HUD.Background")
       .AddComp<Image>()
       .SOColor("m_Color", new Color(0f, 0f, 0f, 0.6f))
       .Apply();

    ctx.MarkDirty();                                // ← 저장 예약
}
// Dispose → 씬 자동 저장
```

---

### GOEditor

> **이럴 때 쓴다**: 특정 GameObject에 컴포넌트를 추가하거나 SerializedProperty 값을 변경할 때. SceneEditor·PrefabEditor에서 반환받아 사용.
> - 컴포넌트 필드 값을 코드로 한 번에 여러 개 초기화할 때
> - 자식 계층 구조를 탐색하며 각각 다른 설정을 적용할 때
> - 타입 어셈블리가 달라 제네릭 사용이 불가할 때(`AddComp(string)` 사용)

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

**흔한 실수**
- `AddComp<T>()` / `WithComp<T>()` **이전**에 SO* 메서드를 호출하면 `InvalidOperationException` 발생. 순서: 컴포넌트 타게팅 → SO* → Apply.
- `Apply()` 를 빠뜨리면 변경이 적용되지 않음. `Dispose()` 시 LogWarning이 뜨지만 데이터는 저장 안 됨.
- 어셈블리가 다른 타입은 제네릭 사용 불가 → `AddComp("Sindy.MyNS.MyComp")` 문자열 오버로드 사용.

**메서드 체이닝 예제**

```csharp
// 여러 컴포넌트를 같은 GO에 순서대로 설정
ctx.GO("PlayerRoot")
   .AddComp<Rigidbody2D>()
   .SOFloat("m_GravityScale", 2f)
   .SOBool("m_IsKinematic", false)
   .Apply()                         // Rigidbody2D 적용

// Child로 자식 GO 이동
var playerGO = ctx.GO("PlayerRoot");
playerGO.Child("Sprite")
        .AddComp<SpriteRenderer>()
        .SOColor("m_Color", Color.white)
        .Apply();

// 어셈블리 경계 우회 (다른 asmdef의 타입)
ctx.GO("UIManager")
   .AddComp("MyGame.UI.UIManagerComponent")
   .SORef("mainCanvas", canvas)
   .SOBool("autoInit", true)
   .Apply();
```

---

### PrefabEditor

> **이럴 때 쓴다**: 프리팹 파일을 직접 열어서 수정하고 저장할 때. 씬과 무관하게 프리팹 에셋 자체를 변경.
> - 공통 UI 프리팹의 색상·텍스트를 일괄 초기화할 때
> - 프리팹 루트에 새 컴포넌트를 추가하거나 기본값을 세팅할 때
> - 수십 개 프리팹에 같은 변경을 반복 적용할 때(`AssetFinder.AllPrefabs<T>`와 조합)

`namespace Sindy.Editor.EditorTools` · `sealed class PrefabEditor : IDisposable`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string assetPath` | `PrefabEditor?` | `LoadPrefabContents`로 프리팹 로드. 실패 시 `null` |
| `GO` | `string hierarchyPath` | `GOEditor` | 루트 기준 상대 경로로 자식 GO 탐색/생성 |
| `GOFind` | `string hierarchyPath` | `GOEditor?` | 루트 기준 상대 경로로 자식 GO 탐색만. 없으면 `null` |
| `Root` | — | `GOEditor` | 프리팹 루트 GO에 대한 GOEditor 반환 |
| `Dispose` | — | `void` | `SaveAsPrefabAsset` + `UnloadPrefabContents` 자동 호출 |

**흔한 실수**
- Dispose 시 자동 저장되므로 별도 `MarkDirty()` 불필요 (SceneEditor와 다른 점).
- 경로가 잘못되면 `Open()`이 `null` 반환. 경로 확인 후 null 체크 필수.

**메서드 체이닝 예제**

```csharp
using (var p = PrefabEditor.Open("Assets/Prefabs/MyButton.prefab"))
{
    if (p == null) return;

    // 루트에 컴포넌트 설정
    p.Root().WithComp<Image>()
            .SOColor("m_Color", Color.cyan)
            .Apply();

    // 자식 GO 접근
    p.GO("Label").AddComp<TextMeshProUGUI>()
                 .SOStr("m_text", "Click Me")
                 .SOFloat("m_fontSize", 18f)
                 .Apply();
}
// Dispose → SaveAsPrefabAsset + UnloadPrefabContents 자동
```

---

### SOEditor\<T\>

> **이럴 때 쓴다**: ScriptableObject 에셋의 값을 코드로 읽거나 수정할 때.
> - 게임 밸런스 수치를 스크립트로 일괄 업데이트할 때
> - 설정 에셋을 새로 생성하고 초기값을 세팅할 때
> - `AssetFinder.AllAssets<T>()`와 조합해 모든 SO 에셋을 순회 수정할 때

`namespace Sindy.Editor.EditorTools` · `sealed class SOEditor<T> : IDisposable where T : ScriptableObject`

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string assetPath` | `SOEditor<T>?` | 기존 에셋 로드. 실패 시 `null` |
| `Create` (static) | `string assetPath` | `SOEditor<T>` | 새 에셋 생성. 이미 있으면 덮어씀 |
| SO* 세터 | (GOEditor와 동일) | `SOEditor<T>` | `SORef`, `SOStr`, `SOBool`, `SOInt`, `SOFloat`, `SODouble`, `SOLong`, `SOEnum`, `SOColor`, `SOVector2/3/4`, `SOQuaternion` |
| `Apply` | — | `void` | `ApplyModifiedProperties()` + `SetDirty()` |
| `Dispose` | — | `void` | `Apply()` 호출 시 `AssetDatabase.SaveAssets()`. 미적용 변경사항 있으면 LogWarning |

**흔한 실수**
- `Apply()` 없이 Dispose하면 변경사항이 디스크에 저장되지 않음.
- `Create()`는 기존 에셋을 덮어쓰므로 경로 확인 후 사용.

**메서드 체이닝 예제**

```csharp
// 기존 에셋 수정
using (var so = SOEditor<FloatVariable>.Open("Assets/Data/Speed.asset"))
{
    if (so == null) return;
    so.SOFloat("Value", 9.8f)
      .SOStr("description", "중력 가속도")
      .Apply();
}

// 새 에셋 생성
using (var so = SOEditor<FloatVariable>.Create("Assets/Data/NewSpeed.asset"))
{
    so.SOFloat("Value", 5f)
      .SOStr("description", "초기 속도")
      .Apply();
}

// AllAssets로 일괄 수정
foreach (var asset in AssetFinder.AllAssets<FloatVariable>("Assets/Data/"))
{
    string path = AssetDatabase.GetAssetPath(asset);
    using var so = SOEditor<FloatVariable>.Open(path);
    so?.SOFloat("Value", asset.Value * 1.1f).Apply(); // 모두 10% 증가
}
```

---

### SindyEdit (통합 파사드)

> **이럴 때 쓴다**: 씬·프리팹·ScriptableObject 를 타입에 무관한 동일한 코드 패턴으로 편집하고 싶을 때. 에셋 타입을 신경 쓰지 않아도 되는 편의 레이어.
> - HTTP `/edit` 엔드포인트를 통해 에셋 이름만으로 원격 편집할 때
> - 편집 대상이 씬인지 프리팹인지 모르거나 혼재할 때
> - 스크립트에서 타입 분기 없이 통일된 패턴을 유지하고 싶을 때

`namespace Sindy.Editor.EditorTools`

#### SindyEdit (정적 파사드)

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `Open` (static) | `string assetPath` | `AssetEditSession?` | 확장자로 타입 자동 감지(`.unity`→씬, `.prefab`→프리팹, 나머지→SO). 실패 시 `null` |
| `Find` (static) | `string nameOrPath` | `AssetEditSession?` | 이름으로 에셋 탐색. 프리팹→씬→SO 순서. 전체 경로 시 `Open`과 동일 |

#### AssetEditSession (IDisposable)

| 메서드 | 파라미터 | 반환값 | 설명 |
|--------|----------|--------|------|
| `GO` | `string goPath` | `AssetEditSession` | `'/'` 또는 `'.'` 구분 계층 경로로 GO 탐색. 탐색 실패 시 LogWarning |
| `SOString` | `string prop, string value` | `AssetEditSession` | `stringValue` 세터 |
| `SOInt` | `string prop, int value` | `AssetEditSession` | `intValue` 세터 |
| `SOFloat` | `string prop, float value` | `AssetEditSession` | `floatValue` 세터 |
| `SOBool` | `string prop, bool value` | `AssetEditSession` | `boolValue` 세터 |
| `SOColor` | `string prop, Color value` | `AssetEditSession` | `colorValue` 세터 |
| `SOVector3` | `string prop, Vector3 value` | `AssetEditSession` | `vector3Value` 세터 |
| `SOVector2` | `string prop, Vector2 value` | `AssetEditSession` | `vector2Value` 세터 |
| `Set` | `string prop, object value` | `AssetEditSession` | 타입 자동 판별. HTTP IPC용. |
| `Save` | — | `void` | 명시적 저장. Dispose에서도 자동 저장되므로 생략 가능 |
| `Dispose` | — | `void` | 변경사항 자동 저장 + 내부 리소스 정리 |

**내부 동작 원리**

| 에셋 타입 | 내부 구현 | 저장 시점 |
|----------|----------|----------|
| `.unity` | `SceneEditor` 위임 | Dispose 시 `EditorSceneManager.SaveScene` |
| `.prefab` | `PrefabEditor` 위임 | Dispose 시 `SaveAsPrefabAsset + UnloadPrefabContents` |
| `.asset` / 기타 | `SerializedObject` 직접 | Dispose 시 `AssetDatabase.SaveAssets` |

**GO() 경로 탐색 방식**

- 씬 모드: 씬 루트 기준 경로 (예: `"Canvas/Panel/Title"`)
- 프리팹 모드: 프리팹 루트의 **자식** 기준 경로 (예: `"Fill/Image"`)
- `'/'`와 `'.'` 구분자 모두 허용 (`"Canvas/Panel"` ≡ `"Canvas.Panel"`)
- GO를 찾지 못하면 LogWarning을 출력하고 이후 SO* 세터는 무시됨

**GO() 이후 컴포넌트 자동 탐색**

`GO()` 후 SO* 메서드를 호출하면, 해당 GO의 **모든 컴포넌트**를 순회하며 프로퍼티를 가진 첫 번째 컴포넌트를 찾습니다. 컴포넌트를 명시할 필요가 없습니다.

```csharp
// Image.m_Color 를 자동으로 찾아 설정
s.GO("Fill/Image").SOColor("m_Color", Color.green);

// TextMeshProUGUI.m_text 를 자동으로 찾아 설정
s.GO("Header/Title").SOString("m_text", "Hello");
```

**흔한 실수**

- `.asset` 파일에 `GO()`를 호출하면 LogWarning 후 무시됨 (SO 편집은 `GO()` 없이 직접 `SOInt()` 등 호출)
- 동일한 이름의 프로퍼티가 여러 컴포넌트에 있으면 첫 번째 컴포넌트만 수정됨

**코드 예제**

```csharp
// ─── 씬 편집 ───────────────────────────────────────────────────────────────
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
s.GO("Canvas/Panel/Title")
 .SOString("m_text", "Hello World")
 .SOColor("m_Color", Color.white);
// Dispose → 자동 저장

// ─── 프리팹 편집 (코드 패턴 완전히 동일) ──────────────────────────────────
using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
s.GO("Fill/Image").SOColor("m_Color", Color.green);

// ─── ScriptableObject 편집 ─────────────────────────────────────────────────
using var s = SindyEdit.Open("Assets/Config/Game.asset");
s.SOInt("maxHealth", 200).SOFloat("gravity", 9.81f);

// ─── 이름으로 자동 탐색 ───────────────────────────────────────────────────
using var s = SindyEdit.Find("GaugeBar");      // 프리팹 이름
s.GO("Fill/Image").SOColor("m_Color", Color.green);

// ─── 명시적 Save (using 없이 사용 시) ─────────────────────────────────────
var session = SindyEdit.Open("Assets/Prefabs/Button.prefab");
session.GO("Label").SOString("m_text", "Click");
session.Save();
session.Dispose();
```

**기존 개별 클래스와의 비교**

| | SindyEdit (통합 파사드) | 기존 개별 클래스 |
|-|----------------------|----------------|
| 타입 감지 | 자동 (확장자 기반) | 수동 (`SceneEditor.Open` / `PrefabEditor.Open` / `SOEditor<T>.Open`) |
| 컴포넌트 지정 | 자동 (프로퍼티명으로 탐색) | 수동 (`AddComp<T>()` / `WithComp<T>()` 후 SO*) |
| Apply 호출 | 불필요 (Dispose 시 자동) | **필수** (`Apply()` 없으면 변경 안 됨) |
| HTTP IPC | `/edit` 엔드포인트로 지원 | 지원 없음 |
| 적합한 용도 | 빠른 단일 프로퍼티 편집, HTTP 원격 편집 | 복잡한 컴포넌트 조작, AddComp, 세밀한 제어 |

---

### AssetFinder

> **이럴 때 쓴다**: 에셋 경로를 모르거나 동적으로 찾아야 할 때. 하드코딩 경로 대신 타입·이름으로 검색.
> - 특정 컴포넌트가 붙은 프리팹을 찾을 때
> - 프리팹 이름 일부만 알고 정확한 경로가 기억나지 않을 때
> - 특정 폴더 내 모든 SO 에셋을 처리할 때

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

**흔한 실수**
- 캐시가 있어서 새로 추가한 에셋이 탐색되지 않을 수 있음 → `AssetFinder.ClearCache()` 호출 후 재탐색.
- `Prefab<T>` 는 T 컴포넌트 자체를 반환함 (GameObject 아님). `AllPrefabs<T>` 는 GameObject 반환.

---

### FieldPeeker

> **이럴 때 쓴다**: SO* 메서드에 쓸 직렬화 필드명(SerializedProperty path)을 모를 때. 컴포넌트의 내부 직렬화 경로는 프로퍼티명과 다른 경우가 많음.

- **에디터 메뉴**: `Sindy/Tools/Field Peeker Window` — 컴포넌트 드래그 → 경로 목록 표시 → [복사] 버튼
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
// Assets/sindy-game-package-for-unity/Editor/Examples/ 에 파일 생성
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

## HTTP 실행 가능 메서드 목록

> `curl http://localhost:6060/execute`로 바로 호출할 수 있는 등록된 메서드 목록.

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

### 예제 코드의 경로 해결 방식

예제 코드(`Example_SceneEdit`, `Example_PrefabEdit`, `Example_SOEdit`)는 `PackagePathHelper`를 통해 설치 방식에 무관하게 경로를 해결합니다.

```
// PackagePathHelper.Resolve("Tests/Runtime/Scenes/MyScene.unity")
//   UPM(Git/Local): "Packages/com.sindy/Tests/Runtime/Scenes/MyScene.unity"
//   Embedded:       "Assets/sindy-game-package-for-unity/Tests/Runtime/Scenes/MyScene.unity"
```

| 설치 방식 | Packages/com.sindy/ 접근 | 에셋 쓰기 가능 |
|----------|--------------------------|--------------|
| Embedded | ✗ (Assets/ 폴백) | ✅ |
| 로컬 참조 | ✅ | ✅ |
| Git URL | ✅ (캐시 가상 경로) | ⚠️ 읽기 전용 |

> **Git URL 설치 시 주의**: 패키지 폴더는 읽기 전용이므로 `SOEditor.Create()`로 패키지 내부에 에셋을 생성하면 실패합니다. 생성 경로를 프로젝트의 `Assets/` 하위로 변경하세요.

---

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

### 에러 발생 시 대처

| 증상 | 원인 | 대처 |
|------|------|------|
| HTTP 요청 타임아웃 / 연결 거부 | 에디터 닫힘 / 컴파일 중 / 포트 충돌 | Unity 열려있는지 확인, Console에 `[SindyCmd] HTTP 서버 시작됨` 로그 확인, 포트 변경 시 Preferences > Sindy |
| `타입을 찾을 수 없습니다: Namespace.Type` | 네임스페이스·타입명 오타, 또는 컴파일 에러 | Unity Console에서 `error CS` 검색 후 수정, namespace 포함 전체 이름 재확인 |
| `static 메서드를 찾을 수 없습니다` | 메서드가 static이 아님 또는 이름 오타 | 메서드에 `static` 키워드 추가, 대소문자 포함 이름 재확인 |
| `NullReferenceException` 발생 | `Open()` 반환값 null 체크 누락, 또는 씬/에셋 경로 오류 | `if (ctx == null) return;` 또는 `throw` 패턴으로 null 처리, `AssetFinder`로 경로 재확인 |
| 씬/에셋 경로 오류 | 경로 오타 또는 파일 없음 | `AssetFinder`로 동적 탐색, `FieldPeeker`로 필드명 확인 |

---

## 내부 구조 / 배경 지식

> 이 섹션은 직접 사용할 필요 없는 내부 동작 설명입니다. HTTP 방식이 막힐 때(방화벽·포트 충돌) 폴백 수단으로만 참고하세요.

### HTTP IPC 동작 흐름

```
AI / Shell                           Unity Editor (EditorCommandWatcher)
──────────────────────────────────   ──────────────────────────────────────
curl POST /execute                   HTTP 리스너 스레드 (블로킹)
  {"method":"..."}          ──────→  요청 수신 → _requestQueue에 enqueue
                                     │
                                     └─ EditorApplication.update (100ms 폴링)
                                        큐에서 dequeue → 리플렉션으로 메서드 실행
                                        → HTTP 응답 반환
                                           {"success":true,"message":"OK"}
```

### 파일 기반 IPC 동작 흐름 (폴백)

> HTTP 포트가 방화벽에 막히거나 포트 충돌 시 사용할 수 있는 폴백 방식.
> `EditorCommandWatcher`는 두 방식을 동시에 폴링합니다.

```
외부 프로세스                        Unity Editor (EditorCommandWatcher)
──────────────────────────────────   ──────────────────────────────────────
1. Temp/sindy_cmd.json 작성
   {"method":"...","id":"abc123"}
2. Temp/sindy_result.json 폴링       3. EditorApplication.update (100ms 폴링)
   (1초 간격, 최대 30초)                4. sindy_cmd.json 발견 → 즉시 삭제
                                        5. 리플렉션으로 메서드 실행
                                        6. sindy_result.json 작성
                                           {"id":"abc123","success":true,...}
7. 결과 확인 후 처리
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

**패키지 내부 구조 (공통):**
```
<package-root>/
├── Editor/
│   ├── EditorTools/
│   │   ├── EditorCommandWatcher.cs  ← HTTP 서버 + 파일 기반 IPC 폴링 [InitializeOnLoad]
│   │   ├── BatchEntryPoint.cs       ← 배치 태스크 베이스 클래스
│   │   ├── BatchRunner.cs           ← Unity 배치 서브프로세스 실행 헬퍼
│   │   ├── SindyEdit.cs             ← 통합 파사드 (씬/프리팹/SO 동일 API)
│   │   ├── SceneEditor.cs
│   │   ├── GOEditor.cs
│   │   ├── PrefabEditor.cs
│   │   ├── SOEditor.cs
│   │   ├── AssetFinder.cs
│   │   └── FieldPeeker.cs
│   └── Examples/
│       ├── Example_BatchTask.cs
│       ├── Example_SceneEdit.cs
│       ├── Example_PrefabEdit.cs
│       └── Example_SOEdit.cs
└── Tests/Editor/Tools/
    └── EDITOR_TOOLKIT_GUIDE.md     ← 이 파일
```

**프로젝트 루트 구조:**
```
YourProject/                          ← 프로젝트 루트 (Assets/, ProjectSettings/ 위치)
├── Temp/                             ← IPC 통신 파일 (Unity 자동 관리)
│   ├── sindy_cmd.json                ← 파일 기반 IPC 커맨드 (폴백용, 실행 후 삭제됨)
│   └── sindy_result.json             ← 파일 기반 IPC 결과 (폴백용)
└── Logs/
    ├── batch_*.log                   ← Unity 전체 로그 (자동 생성)
    └── batch_result.txt              ← 배치 결과 요약 (자동 생성)
```
