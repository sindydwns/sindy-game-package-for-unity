# SindyEdit 튜토리얼

씬·프리팹·ScriptableObject를 하나의 API로 편집하는 `SindyEdit` 사용법을 단계별로 설명합니다.

---

## 목차

1. [기본 개념](#1-기본-개념)
2. [씬 생성 · 편집 · 삭제](#2-씬-생성--편집--삭제)
3. [씬 하이라키 조회 · 생성 · 삭제](#3-씬-하이라키-조회--생성--삭제)
4. [컴포넌트 추가 · 삭제 · 값 수정](#4-컴포넌트-추가--삭제--값-수정)
5. [프리팹 생성 · 삭제](#5-프리팹-생성--삭제)
6. [프리팹 구조 읽기](#6-프리팹-구조-읽기)
7. [프리팹에 자식 추가](#7-프리팹에-자식-추가)
8. [프리팹 참조 연결 (중첩 프리팹)](#8-프리팹-참조-연결-중첩-프리팹)
9. [ScriptableObject 생성 · 삭제](#9-scriptableobject-생성--삭제)
10. [ScriptableObject 값 변경](#10-scriptableobject-값-변경)
11. [복잡한 구조의 ScriptableObject 편집](#11-복잡한-구조의-scriptableobject-편집)

---

## 1. 기본 개념

### using 블록 패턴

모든 편집은 `using` 블록 안에서 이루어집니다. 블록이 끝나면(`Dispose`) 변경사항이 자동으로 디스크에 저장됩니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return; // 로드 실패 시 null

s.GO("Canvas/Title").SetString("m_text", "Hello");
// Dispose → EditorSceneManager.SaveScene 자동 호출
```

명시적으로 저장이 필요하면 `Save()`를 호출합니다. `using` 블록을 사용하는 경우 `Save()`는 생략할 수 있습니다.

```csharp
using var s = SindyEdit.Open("Assets/Config/Game.asset");
s.SetInt("maxHealth", 200);
s.Save(); // 생략 가능 — Dispose 시 자동 저장
```

### FP 설계 — 탐색은 새 세션을 반환

`GO()` / `Root()` / `FindGameObject()` / `Child()`는 `this`를 변경하지 않고 새로운 `AssetEditSession`을 반환합니다. **반환값을 변수로 받아야 합니다.**

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");

// 올바른 사용 — 반환값을 변수로 받음
var title  = s.GO("Canvas/Panel/Title");
var footer = s.GO("Canvas/Footer");
title.SetString("m_text", "Stage 1");
footer.SetString("m_text", "v1.0.0");

// 잘못된 사용 — 반환값을 버림
s.GO("Canvas/Panel/Title"); // 이 세션이 버려짐
s.SetString("m_text", "Stage 1"); // s는 GO가 선택되지 않은 루트 세션
```

### 오류 처리

프로퍼티를 찾지 못하거나 타입이 맞지 않으면 `InvalidOperationException`을 던집니다.

```csharp
// 프로퍼티 이름이 틀리면 throw
s.GO("Canvas/Title").SetString("m_Text", "Hi"); // "m_text"가 맞는 이름

// 타입 불일치 시 throw
s.GO("Canvas/Title").SetFloat("m_text", 1.0f); // string 프로퍼티에 float → throw
```

정확한 직렬화 경로는 `Sindy/Tools/Field Peeker Window`로 확인합니다.

---

## 2. 씬 생성 · 편집 · 삭제

### 씬 생성

`NewScene()`은 빈 씬을 생성합니다. **파일이 이미 존재하면 `InvalidOperationException`을 던집니다.**

```csharp
[MenuItem("MyGame/Setup/씬 생성")]
static void CreateScene()
{
    const string path = "Assets/Scenes/Stage01.unity";

    // 이미 있으면 먼저 삭제하거나 스킵
    if (SindyEdit.Exists(path))
    {
        Debug.Log($"이미 존재: {path}");
        return;
    }

    using var s = SindyEdit.NewScene(path);
    if (s == null) return;

    // 기본 오브젝트 구성
    s.CreateGameObject("Main Camera");
    s.CreateGameObject("Directional Light");
    s.CreateGameObject("Canvas");

    Debug.Log($"씬 생성됨: {path}");
}
```

여러 씬을 일괄 생성할 때는 `Exists` + `Delete`로 상태를 제어합니다.

```csharp
[MenuItem("MyGame/Setup/스테이지 씬 일괄 생성")]
static void CreateAllStageScenes()
{
    string[] stages = { "Stage01", "Stage02", "Stage03", "Boss" };

    foreach (var stage in stages)
    {
        string path = $"Assets/Scenes/Stages/{stage}.unity";

        // 이미 있으면 덮어쓰기 위해 먼저 삭제
        if (SindyEdit.Exists(path))
            SindyEdit.Delete(path);

        using var s = SindyEdit.NewScene(path);
        if (s == null) continue;

        s.CreateGameObject("Canvas");
        s.CreateGameObject("GameManager");
        Debug.Log($"생성됨: {path}");
    }
}
```

### 기존 씬 열기

```csharp
[MenuItem("MyGame/Setup/Main씬 편집")]
static void EditMainScene()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GO("Canvas/Panel/Title")
     .SetString("m_text", "Main Menu")
     .SetColor("m_fontColor", Color.white)
     .SetFloat("m_fontSize", 36f);
}
```

### 씬 삭제

`SindyEdit.Delete()`는 `.unity` / `.prefab` / `.asset` 세 가지 확장자를 지원합니다. 파일이 없으면 예외를 던집니다.

```csharp
[MenuItem("MyGame/Setup/임시 씬 삭제")]
static void DeleteTempScene()
{
    const string path = "Assets/Scenes/Temp.unity";

    if (!SindyEdit.Exists(path))
    {
        Debug.Log("삭제할 파일이 없습니다.");
        return;
    }

    SindyEdit.Delete(path);
    Debug.Log($"삭제됨: {path}");
}
```

---

## 3. 씬 하이라키 조회 · 생성 · 삭제

### GO 탐색 방법 4가지

| 방법 | 특징 | 사용 시점 |
|------|------|-----------|
| `GO("Canvas/HUD/Title")` | 경로로 정확히 탐색 (구분자 `/` 또는 `.`) | 계층 구조를 알 때 |
| `FindGameObject("Title")` | 이름으로 전체 계층 재귀 탐색 | 위치를 모를 때 |
| `Root()` | 씬 첫 번째 루트 GO | 최상위부터 내려갈 때 |
| `Child("HUD")` / `Child(0)` | 현재 GO의 직계 자식 | 단계별로 명시적으로 내려갈 때 |

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// 경로로 직접 접근 — 가장 빠름
var title = s.GO("Canvas/HUD/Title");
title.SetString("m_text", "Stage 1");

// 이름으로 재귀 탐색 — 계층 위치를 몰라도 됨
var hp = s.FindGameObject("HpBar");
hp.SetFloat("m_fillAmount", 0.75f);

// Root → Child 순서로 내려가기
var root      = s.Root();
var canvas    = root.Child("Canvas");
var hud       = canvas.Child("HUD");
var firstBtn  = hud.Child(0); // 인덱스로 접근

// 컴포넌트 유무 확인
if (s.GO("Canvas").HasComponent<UnityEngine.CanvasScaler>())
    Debug.Log("CanvasScaler 있음");
```

### GO 생성

`CreateGameObject()`는 현재 GO의 자식으로 새 GO를 만듭니다. 현재 GO가 없으면(루트 세션) 씬 루트 레벨에 만듭니다. 생성된 GO를 가리키는 새 세션을 반환합니다.

```csharp
[MenuItem("MyGame/Setup/HUD 계층 생성")]
static void CreateHUD()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    // ── 씬 루트에 Canvas 생성 (s에 GO가 선택되지 않은 상태 → 씬 루트)
    s.CreateGameObject("Canvas");

    // ── Canvas 아래에 HUD 생성
    s.GO("Canvas").CreateGameObject("HUD");

    // ── HUD 아래에 Title, Background, Footer 생성
    s.GO("Canvas/HUD").CreateGameObject("Title")
        .AddComponent<TMPro.TextMeshProUGUI>()
        .SetProperty("m_text", "Stage 1")
        .SetProperty("m_fontSize", 32f)
        .SetProperty("m_fontColor", new Color(1f, 0.9f, 0.4f));

    s.GO("Canvas/HUD").CreateGameObject("Background")
        .AddComponent<UnityEngine.UI.Image>()
        .SetProperty("m_Color", new Color(0f, 0f, 0f, 0.5f));

    s.GO("Canvas/HUD").CreateGameObject("Footer");
    s.GO("Canvas/HUD/Footer").CreateGameObject("VersionLabel")
        .AddComponent<TMPro.TextMeshProUGUI>()
        .SetProperty("m_text", "v1.0.0")
        .SetProperty("m_fontSize", 12f)
        .SetProperty("m_fontColor", new Color(0.6f, 0.6f, 0.6f));
}
```

`CreateGameObject()`는 `AssetEditSession`을 반환하므로, 생성 후 바로 다른 GO를 탐색할 수 있습니다.

```csharp
// 체이닝으로 계층 한 번에 구성
s.GO("Canvas").CreateGameObject("Overlay")
              .CreateGameObject("Panel")   // Overlay의 자식
              .CreateGameObject("Title");  // Panel의 자식
```

### GO 삭제

`DeleteGameObject()`는 현재 GO를 삭제하고 부모 GO를 가리키는 새 세션을 반환합니다.

```csharp
[MenuItem("MyGame/Setup/디버그 오브젝트 일괄 삭제")]
static void DeleteDebugObjects()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    // 단일 GO 삭제
    s.GO("Canvas/DebugPanel").DeleteGameObject();

    // 없을 수도 있는 GO는 HasComponent로 존재 확인 후 삭제
    var devTool = s.FindGameObject("DevToolOverlay");
    if (devTool.HasComponent<UnityEngine.Transform>())
        devTool.DeleteGameObject();
}
```

### GO 존재 여부 확인

`HasComponent<Transform>()`으로 GO가 유효한지 간접 확인합니다. 탐색에 실패한 세션도 null GO 세션으로 안전하게 유지되어 이후 체이닝이 무시됩니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

var debugPanel = s.GO("Canvas/DebugPanel");

// Transform이 있으면 유효한 GO
if (debugPanel.HasComponent<UnityEngine.Transform>())
{
    debugPanel.SetBool("m_IsActive", false);
    Debug.Log("DebugPanel 비활성화");
}
else
{
    Debug.Log("DebugPanel이 씬에 없습니다.");
}
```

---

## 4. 컴포넌트 추가 · 삭제 · 값 수정

### 컴포넌트 추가

`AddComponent<T>()`는 컴포넌트를 추가하고 `ComponentScope`를 반환합니다. `ComponentScope`에서 `SetProperty()` 체이닝으로 값을 바로 설정합니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// AddComponent → ComponentScope에서 SetProperty 체이닝
s.GO("Canvas").AddComponent<UnityEngine.CanvasGroup>()
    .SetProperty("m_Alpha", 1f)
    .SetProperty("m_Interactable", true)
    .SetProperty("m_BlocksRaycasts", true);

// 컴포넌트 추가 후 세션을 계속 사용할 때는 변수로 저장
var canvasSession = s.GO("Canvas");
canvasSession.AddComponent<UnityEngine.CanvasScaler>();
canvasSession.SetFloat("m_ReferenceResolution.x", 1920f);
canvasSession.SetFloat("m_ReferenceResolution.y", 1080f);
```

`GetOrAddComponent<T>()`는 이미 있으면 가져오고, 없으면 추가합니다. 멱등(idempotent) 초기화에 적합합니다.

```csharp
// 있으면 가져오고, 없으면 추가 — 콜백으로 바로 편집
s.GO("Canvas").GetOrAddComponent<UnityEngine.UI.GraphicRaycaster>(gr =>
{
    gr.SetProperty("m_BlockingMask", -1); // Everything
});
```

### 컴포넌트 삭제

`RemoveComponent<T>()`는 `AssetEditSession`을 반환하므로 세터 체이닝을 이어갈 수 있습니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// 단일 컴포넌트 제거
s.FindGameObject("Title").RemoveComponent<UnityEngine.UI.Shadow>();

// 같은 타입 컴포넌트가 여러 개일 때 인덱스 지정
// GO에 Collider2D가 3개 있으면: index 0, 1, 2
s.FindGameObject("Player").RemoveComponent<UnityEngine.BoxCollider2D>(index: 1);

// 있을 때만 제거
var label = s.FindGameObject("DebugLabel");
if (label.HasComponent<TMPro.TextMeshProUGUI>())
    label.RemoveComponent<TMPro.TextMeshProUGUI>();
```

### 컴포넌트 값 쓰기 — 세션 레벨 세터

GO가 선택된 세션에서 세터(`SetFloat`, `SetString` 등)를 호출하면 해당 GO의 **모든 컴포넌트를 순회**하여 해당 이름의 프로퍼티를 찾습니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// m_text는 TextMeshProUGUI 컴포넌트에서 자동으로 찾음
s.GO("Canvas/HUD/Title").SetString("m_text", "Round 2");

// m_Color는 Image 컴포넌트에서 자동으로 찾음
s.GO("Canvas/HUD/Background").SetColor("m_Color", new Color(0f, 0f, 0f, 0.7f));

// 여러 프로퍼티를 체이닝으로 한 번에 설정
s.GO("Canvas/HUD/Title")
 .SetString("m_text", "Round 2")
 .SetFloat("m_fontSize", 36f)
 .SetColor("m_fontColor", Color.yellow);
```

### 컴포넌트 값 쓰기 — GetComponent 콜백

특정 타입의 컴포넌트를 명시하려면 `GetComponent<T>(action)` 콜백을 사용합니다. 동일 이름의 프로퍼티가 여러 컴포넌트에 있을 때도 정확한 대상을 지정할 수 있습니다.

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// 콜백 패턴 — 여러 프로퍼티를 한 번에 설정
s.GO("Canvas/HUD/Title").GetComponent<TMPro.TextMeshProUGUI>(tmp =>
{
    tmp.SetProperty("m_text", "Round 2");
    tmp.SetProperty("m_fontSize", 36f);
    tmp.SetProperty("m_fontColor", Color.yellow);
    tmp.SetProperty("m_enableAutoSizing", false);
});

// 반환값(ComponentScope)을 변수로 받아 값 읽기+쓰기 혼용
var imgScope = s.GO("Canvas/HUD/Background").GetComponent<UnityEngine.UI.Image>();
if (imgScope != null)
{
    Color current = imgScope.GetColor("m_Color");
    Debug.Log($"현재 색상: {current}");
    imgScope.SetProperty("m_Color", new Color(current.r, current.g, current.b, 0.8f));
}
```

### 컴포넌트 값 읽기 — 세션 레벨 getter

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

var title = s.GO("Canvas/HUD/Title");
string text  = title.GetString("m_text");
float  size  = title.GetFloat("m_fontSize");
Color  color = title.GetColor("m_fontColor");
Debug.Log($"text={text}, size={size}, color={color}");

// 타입을 명시한 오버로드 — GO에 같은 프로퍼티를 가진 컴포넌트가 여러 개일 때 유용
float alpha = s.GO("Panel").GetFloat<UnityEngine.CanvasGroup>("m_Alpha");
```

### 컴포넌트 값 읽기 — ComponentScope getter

```csharp
// ComponentScope에서 직접 읽기
var scope = s.GO("Canvas/HUD/Title").GetComponent<TMPro.TextMeshProUGUI>();
if (scope != null)
{
    string text      = scope.GetString("m_text");
    float  fontSize  = scope.GetFloat("m_fontSize");
    Color  fontColor = scope.GetColor("m_fontColor");
    bool   autoSize  = scope.GetBool("m_enableAutoSizing");
    Debug.Log($"{text} / {fontSize}pt / autoSize={autoSize}");
}
```

### 같은 타입 컴포넌트가 여러 개일 때 — index

```csharp
using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
if (s == null) return;

// "Player" GO에 BoxCollider2D가 2개 붙어 있다고 가정
var player = s.FindGameObject("Player");

// index 0 — 첫 번째 Collider: 히트박스
player.GetComponent<UnityEngine.BoxCollider2D>(col =>
    col.SetProperty("m_Size", new UnityEngine.Vector2(1f, 2f)), index: 0);

// index 1 — 두 번째 Collider: 발 밑 지형 감지
player.GetComponent<UnityEngine.BoxCollider2D>(col =>
    col.SetProperty("m_Size", new UnityEngine.Vector2(0.8f, 0.2f)), index: 1);
```

---

## 5. 프리팹 생성 · 삭제

### 프리팹 생성

`NewPrefab()`은 빈 프리팹을 생성합니다. **파일이 이미 존재하면 `InvalidOperationException`을 던집니다.**

```csharp
[MenuItem("MyGame/Prefabs/UI 버튼 프리팹 생성")]
static void CreateButtonPrefab()
{
    const string path = "Assets/Prefabs/UI/Button.prefab";

    if (SindyEdit.Exists(path))
    {
        Debug.Log($"이미 존재: {path}");
        return;
    }

    // 두 번째 인수 = 프리팹 루트 GO 이름 (기본값: "Root")
    using var s = SindyEdit.NewPrefab(path, "Button");
    if (s == null) return;

    // 루트(Button)에 Image + Button 컴포넌트 추가
    s.Root().AddComponent<UnityEngine.UI.Image>()
        .SetProperty("m_Color", new Color(0.2f, 0.5f, 0.9f));

    s.Root().AddComponent<UnityEngine.UI.Button>();

    // 자식 GO "Label" 추가
    s.Root().CreateGameObject("Label")
        .AddComponent<TMPro.TextMeshProUGUI>()
        .SetProperty("m_text", "Button")
        .SetProperty("m_fontSize", 18f)
        .SetProperty("m_fontColor", Color.white)
        .SetProperty("m_alignment", 4102); // Center + Middle (TMPro TextAlignmentOptions enum)

    Debug.Log($"프리팹 생성됨: {path}");
}
```

여러 프리팹을 일괄 생성하는 패턴입니다.

```csharp
[MenuItem("MyGame/Prefabs/게이지 프리팹 세트 생성")]
static void CreateGaugePrefabSet()
{
    var configs = new (string path, string rootName, Color fillColor)[]
    {
        ("Assets/Prefabs/UI/HpGauge.prefab",   "HpGauge",   new Color(0.9f, 0.2f, 0.2f)),
        ("Assets/Prefabs/UI/MpGauge.prefab",   "MpGauge",   new Color(0.2f, 0.4f, 0.9f)),
        ("Assets/Prefabs/UI/ExpGauge.prefab",  "ExpGauge",  new Color(0.9f, 0.7f, 0.1f)),
    };

    foreach (var (path, rootName, fillColor) in configs)
    {
        if (SindyEdit.Exists(path))
            SindyEdit.Delete(path);

        using var s = SindyEdit.NewPrefab(path, rootName);
        if (s == null) continue;

        // 배경 이미지
        s.Root().CreateGameObject("Background")
            .AddComponent<UnityEngine.UI.Image>()
            .SetProperty("m_Color", new Color(0.1f, 0.1f, 0.1f, 0.8f));

        // 게이지 fill 이미지
        s.Root().CreateGameObject("Fill")
            .AddComponent<UnityEngine.UI.Image>()
            .SetProperty("m_Color", fillColor);

        Debug.Log($"생성됨: {path}");
    }
}
```

### 프리팹 삭제

```csharp
[MenuItem("MyGame/Prefabs/임시 프리팹 삭제")]
static void DeleteTempPrefabs()
{
    string[] targets =
    {
        "Assets/Prefabs/Temp/Draft.prefab",
        "Assets/Prefabs/Temp/Test.prefab",
    };

    foreach (var path in targets)
    {
        if (SindyEdit.Exists(path))
        {
            SindyEdit.Delete(path);
            Debug.Log($"삭제됨: {path}");
        }
    }
}
```

---

## 6. 프리팹 구조 읽기

`Root()` / `Child()` / `HasComponent()` / `GetComponent()` 로 프리팹의 계층과 컴포넌트 정보를 읽습니다.

```csharp
[MenuItem("MyGame/Prefabs/GaugeBar 구조 출력")]
static void PrintGaugeBarStructure()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    if (s == null) return;

    // ── 루트 GO 정보
    var root = s.Root();
    Debug.Log("=== GaugeBar 프리팹 구조 ===");
    Debug.Log($"루트 Image 있음: {root.HasComponent<UnityEngine.UI.Image>()}");
    Debug.Log($"루트 Button 있음: {root.HasComponent<UnityEngine.UI.Button>()}");

    // ── 루트의 컴포넌트 값 읽기 (ComponentScope)
    var rootImg = root.GetComponent<UnityEngine.UI.Image>();
    if (rootImg != null)
    {
        Color bg = rootImg.GetColor("m_Color");
        Debug.Log($"루트 배경색: {bg}");
    }

    // ── Child(string)으로 직계 자식 읽기
    var fill = root.Child("Fill");
    if (fill.HasComponent<UnityEngine.UI.Image>())
    {
        Color fillColor = fill.GetComponent<UnityEngine.UI.Image>()?.GetColor("m_Color")
                          ?? Color.clear;
        Debug.Log($"Fill 색상: {fillColor}");
    }

    // ── FindGameObject로 깊은 자식 읽기
    var label = s.FindGameObject("Label");
    if (label.HasComponent<TMPro.TextMeshProUGUI>())
    {
        string text     = label.GetString("m_text");
        float  fontSize = label.GetFloat("m_fontSize");
        Debug.Log($"Label: \"{text}\" ({fontSize}pt)");
    }

    // ── Child(int)으로 인덱스 순서 확인
    Debug.Log("── 직계 자식 목록 ──");
    for (int i = 0; i < 10; i++)
    {
        var child = root.Child(i);
        if (!child.HasComponent<UnityEngine.Transform>()) break;
        // Transform 유무로 유효한 자식인지 확인
        // (유효하지 않은 인덱스면 LogWarning과 함께 null GO 세션 반환)
        Debug.Log($"  [{i}] HasImage={child.HasComponent<UnityEngine.UI.Image>()}");
    }
}
```

여러 프리팹의 특정 값을 일괄 수집합니다.

```csharp
[MenuItem("MyGame/Prefabs/모든 GaugeBar Fill 색상 출력")]
static void PrintAllGaugeColors()
{
    var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });

    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        using var s = SindyEdit.Open(path);
        if (s == null) continue;

        var fill = s.FindGameObject("Fill");
        if (!fill.HasComponent<UnityEngine.UI.Image>()) continue;

        Color color = fill.GetComponent<UnityEngine.UI.Image>()?.GetColor("m_Color")
                      ?? Color.clear;
        Debug.Log($"{System.IO.Path.GetFileName(path)}: Fill={color}");
    }
}
```

---

## 7. 프리팹에 자식 추가

기존 프리팹에 `CreateGameObject()`로 자식을 추가합니다. `Root()`로 루트 GO를 먼저 잡은 뒤 자식을 붙입니다.

```csharp
[MenuItem("MyGame/Prefabs/GaugeBar에 라벨 추가")]
static void AddLabelToGaugeBar()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    if (s == null) return;

    // 이미 있으면 스킵
    var existing = s.FindGameObject("ValueLabel");
    if (existing.HasComponent<TMPro.TextMeshProUGUI>())
    {
        Debug.Log("ValueLabel이 이미 있습니다.");
        return;
    }

    // 루트 아래에 "ValueLabel" 추가
    s.Root().CreateGameObject("ValueLabel")
        .AddComponent<TMPro.TextMeshProUGUI>()
        .SetProperty("m_text", "100%")
        .SetProperty("m_fontSize", 14f)
        .SetProperty("m_fontColor", Color.white)
        .SetProperty("m_alignment", 4102);
}
```

여러 컴포넌트를 한 GO에 추가할 때는 세션 변수를 활용합니다.

```csharp
[MenuItem("MyGame/Prefabs/버튼에 애니메이션 설정 추가")]
static void AddAnimSetupToButton()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Button.prefab");
    if (s == null) return;

    // 기존 루트 세션 유지 (AddComponent 이후에도 root 세션으로 세터 체이닝 가능)
    var root = s.Root();

    // CanvasGroup 추가
    root.AddComponent<UnityEngine.CanvasGroup>()
        .SetProperty("m_Alpha", 1f)
        .SetProperty("m_Interactable", true)
        .SetProperty("m_BlocksRaycasts", true);

    // Animator 추가 (controller는 별도 SetRef로 연결)
    root.AddComponent<UnityEngine.Animator>();

    // AnimatorController 에셋 참조 연결
    var controller = AssetDatabase.LoadAssetAtPath<UnityEngine.RuntimeAnimatorController>(
        "Assets/Animations/UI/ButtonAnim.controller");

    if (controller != null)
        root.GetComponent<UnityEngine.Animator>(anim =>
            anim.SetRef("m_Controller", controller));

    // 자식 "Highlight" GO 추가 (마우스 오버 하이라이트)
    root.CreateGameObject("Highlight")
        .AddComponent<UnityEngine.UI.Image>()
        .SetProperty("m_Color", new Color(1f, 1f, 1f, 0.1f))
        .SetProperty("m_RaycastTarget", false);
}
```

---

## 8. 프리팹 참조 연결 (중첩 프리팹)

SindyEdit에서 "중첩 프리팹"이란, 컴포넌트의 직렬화 필드에 다른 프리팹 에셋을 참조로 연결하는 것을 의미합니다. 실제 프리팹 인스턴스를 자식으로 배치하는 진짜 Nested Prefab은 Unity Editor의 Prefab 모드에서만 가능합니다.

### 스포너 컴포넌트에 스폰 프리팹 연결

```csharp
// 커스텀 컴포넌트 예시:
// public class EnemySpawner : MonoBehaviour
// {
//     public GameObject enemyPrefab;       // 직렬화 필드
//     public GameObject bossPrefab;
//     public int spawnCount = 5;
// }

[MenuItem("MyGame/Prefabs/스포너 프리팹 설정")]
static void SetupSpawnerPrefab()
{
    var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Prefabs/Enemies/Slime.prefab");
    var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Prefabs/Enemies/Boss.prefab");

    using var s = SindyEdit.Open("Assets/Prefabs/Spawner.prefab");
    if (s == null) return;

    // EnemySpawner 컴포넌트의 직렬화 필드에 프리팹 참조 연결
    // (세션 레벨 SetRef: 모든 컴포넌트를 순회해 해당 프로퍼티를 찾음)
    s.Root()
     .SetRef("enemyPrefab", enemyPrefab)
     .SetRef("bossPrefab", bossPrefab)
     .SetInt("spawnCount", 8);
}
```

### 특정 컴포넌트에만 참조 연결

이름이 같은 프로퍼티가 여러 컴포넌트에 있다면 `GetComponent<T>(action)`으로 명시합니다.

```csharp
// 두 개의 커스텀 컴포넌트가 모두 "prefabRef" 필드를 가질 때
[MenuItem("MyGame/Prefabs/UI 패널 참조 연결")]
static void SetupUIPanelRefs()
{
    var mainPanel  = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Prefabs/UI/MainPanel.prefab");
    var settingsPanel = AssetDatabase.LoadAssetAtPath<GameObject>(
        "Assets/Prefabs/UI/SettingsPanel.prefab");

    using var s = SindyEdit.Open("Assets/Prefabs/UI/RootUI.prefab");
    if (s == null) return;

    // NavigationController 컴포넌트에만 참조 설정
    s.Root().GetComponent<NavigationController>(nav =>
    {
        nav.SetRef("mainPanelPrefab", mainPanel);
        nav.SetRef("settingsPanelPrefab", settingsPanel);
    });
}
```

### 여러 프리팹에 공통 에셋 참조 일괄 연결

```csharp
[MenuItem("MyGame/Prefabs/모든 UI 프리팹에 폰트 에셋 연결")]
static void ApplyFontAssetToAllUIPrefabs()
{
    var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(
        "Assets/Fonts/GameFont SDF.asset");

    if (fontAsset == null)
    {
        Debug.LogError("폰트 에셋을 찾을 수 없습니다.");
        return;
    }

    var guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/UI" });
    int count = 0;

    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        using var s = SindyEdit.Open(path);
        if (s == null) continue;

        // 프리팹 내 모든 TextMeshProUGUI를 찾아 폰트 에셋 연결
        // (FindGameObject + HasComponent 패턴으로 여러 텍스트 GO 처리)
        bool changed = false;
        void ApplyFont(AssetEditSession session)
        {
            if (!session.HasComponent<TMPro.TextMeshProUGUI>()) return;
            session.GetComponent<TMPro.TextMeshProUGUI>(tmp =>
                tmp.SetRef("m_fontAsset", fontAsset));
            changed = true;
        }

        ApplyFont(s.Root());

        // 직계 자식 순회
        for (int i = 0; i < 20; i++)
        {
            var child = s.Root().Child(i);
            if (!child.HasComponent<UnityEngine.Transform>()) break;
            ApplyFont(child);
        }

        if (changed)
        {
            count++;
            Debug.Log($"폰트 적용됨: {System.IO.Path.GetFileName(path)}");
        }
    }

    Debug.Log($"총 {count}개 프리팹에 폰트 에셋 적용 완료.");
}
```

---

## 9. ScriptableObject 생성 · 삭제

### NewAsset\<T\> — 새 SO 생성 (throw if exists)

`NewAsset<T>()`은 파일이 이미 있으면 `InvalidOperationException`을 던집니다. 안전한 초기 생성에 사용합니다.

```csharp
[MenuItem("MyGame/Config/초기 설정 SO 생성")]
static void CreateInitialConfigs()
{
    var configs = new (string path, System.Action<AssetEditSession> setup)[]
    {
        (
            "Assets/Config/PlayerConfig.asset",
            s => s.SetFloat("moveSpeed", 5f)
                  .SetFloat("jumpForce", 10f)
                  .SetInt("maxHealth", 100)
                  .SetBool("doubleJumpEnabled", false)
        ),
        (
            "Assets/Config/EnemyConfig.asset",
            s => s.SetFloat("patrolSpeed", 2f)
                  .SetFloat("chaseSpeed", 4f)
                  .SetInt("maxHealth", 50)
                  .SetFloat("attackRange", 1.5f)
        ),
        (
            "Assets/Config/GameConfig.asset",
            s => s.SetFloat("gravity", 9.81f)
                  .SetString("gameVersion", "1.0.0")
                  .SetBool("debugMode", false)
        ),
    };

    foreach (var (path, setup) in configs)
    {
        if (SindyEdit.Exists(path))
        {
            Debug.Log($"스킵 (이미 존재): {path}");
            continue;
        }

        using var s = SindyEdit.NewAsset<GameConfigSO>(path);
        setup(s);
        Debug.Log($"생성됨: {path}");
    }
}
```

### Create\<T\> — 덮어쓰기 포함 생성

기존 파일을 덮어쓰는 초기화가 필요하면 `Create<T>()`를 사용합니다.

```csharp
[MenuItem("MyGame/Config/플레이어 설정 리셋")]
static void ResetPlayerConfig()
{
    // 이미 있으면 덮어씀 (기존 데이터 손실 주의)
    using var s = SindyEdit.Create<PlayerConfigSO>("Assets/Config/PlayerConfig.asset");
    if (s == null) return;

    s.SetFloat("moveSpeed", 5f)
     .SetFloat("jumpForce", 10f)
     .SetInt("maxHealth", 100)
     .SetBool("doubleJumpEnabled", false);

    Debug.Log("PlayerConfig 초기화 완료.");
}
```

### SO 삭제

**세션 메서드 `DeleteAsset()`** — `.asset` 모드에서만 사용. 미저장 변경사항을 폐기하고 파일을 삭제합니다.

```csharp
using var s = SindyEdit.Open("Assets/Config/TempConfig.asset");
if (s == null) return;

s.DeleteAsset(); // 파일 삭제, 세션 무효화
// Dispose 시 이미 무효화된 세션이므로 저장 시도 없음
```

**정적 메서드 `SindyEdit.Delete()`** — 세션 없이 바로 삭제. `.unity` / `.prefab` / `.asset` 모두 지원.

```csharp
[MenuItem("MyGame/Config/디버그 설정 파일 삭제")]
static void DeleteDebugConfigs()
{
    string[] targets =
    {
        "Assets/Config/Debug/DebugConfig.asset",
        "Assets/Config/Debug/CheatConfig.asset",
    };

    foreach (var path in targets)
    {
        if (SindyEdit.Exists(path))
        {
            SindyEdit.Delete(path);
            Debug.Log($"삭제됨: {path}");
        }
    }
}
```

---

## 10. ScriptableObject 값 변경

### 기본 타입 필드 수정

```csharp
[MenuItem("MyGame/Config/게임 밸런스 조정")]
static void TweakBalance()
{
    using var s = SindyEdit.Open("Assets/Config/GameConfig.asset");
    if (s == null) return;

    s.SetFloat("gravity", 9.81f)
     .SetInt("maxEnemyCount", 20)
     .SetBool("debugMode", false)
     .SetString("gameVersion", "1.2.3")
     .SetColor("ambientLightColor", new Color(0.8f, 0.85f, 1f))
     .SetVector3("spawnOffset", new UnityEngine.Vector3(0f, 1f, 0f));
}
```

### 값 읽고 조건에 따라 수정

```csharp
[MenuItem("MyGame/Config/난이도별 체력 스케일링")]
static void ScaleHealthByDifficulty()
{
    using var s = SindyEdit.Open("Assets/Config/EnemyConfig.asset");
    if (s == null) return;

    int baseHp   = s.GetInt("maxHealth");
    float scale  = s.GetFloat("difficultyMultiplier");
    bool  isHard = s.GetBool("hardMode");

    int newHp = isHard ? (int)(baseHp * scale * 1.5f) : (int)(baseHp * scale);
    s.SetInt("maxHealth", newHp);

    Debug.Log($"체력 업데이트: {baseHp} → {newHp} (scale={scale}, hard={isHard})");
}
```

### Object Reference 필드 수정

```csharp
[MenuItem("MyGame/Config/스폰 포인트 참조 연결")]
static void LinkSpawnPointRefs()
{
    var spawnSO  = AssetDatabase.LoadAssetAtPath<SpawnPointDataSO>(
        "Assets/Data/SpawnPoints.asset");
    var themeSO  = AssetDatabase.LoadAssetAtPath<UiThemeSO>(
        "Assets/UI/DarkTheme.asset");

    using var s = SindyEdit.Open("Assets/Config/StageConfig.asset");
    if (s == null) return;

    s.SetRef("spawnPointData", spawnSO)
     .SetRef("uiTheme", themeSO);
}
```

### 여러 SO 일괄 수정

```csharp
[MenuItem("MyGame/Config/모든 EnemyData 체력 20% 증가")]
static void BuffAllEnemies()
{
    var guids = AssetDatabase.FindAssets("t:EnemyDataSO", new[] { "Assets/Data/Enemies" });

    foreach (var guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);

        using var s = SindyEdit.Open(path);
        if (s == null) continue;

        int oldHp = s.GetInt("maxHealth");
        s.SetInt("maxHealth", (int)(oldHp * 1.2f));

        Debug.Log($"{System.IO.Path.GetFileName(path)}: {oldHp} → {s.GetInt("maxHealth")}");
    }
}
```

---

## 11. 복잡한 구조의 ScriptableObject 편집

### 중첩 구조체 — dot notation

`SerializedProperty` 경로에 `.`을 사용하면 중첩 구조체의 서브 필드에 접근합니다.

```csharp
// C# 데이터 구조 예시:
// [System.Serializable]
// public struct StatBlock
// {
//     public int baseValue;
//     public float growthRate;
//     public int maxValue;
// }
//
// public class HeroDataSO : ScriptableObject
// {
//     public StatBlock health;
//     public StatBlock mana;
//     public StatBlock attack;
// }

[MenuItem("MyGame/Data/히어로 스탯 설정")]
static void SetHeroStats()
{
    using var s = SindyEdit.Open("Assets/Data/Heroes/Warrior.asset");
    if (s == null) return;

    // "필드명.서브필드명" 경로로 중첩 구조체 접근
    s.SetInt("health.baseValue", 200)
     .SetFloat("health.growthRate", 1.1f)
     .SetInt("health.maxValue", 500);

    s.SetInt("mana.baseValue", 50)
     .SetFloat("mana.growthRate", 1.05f)
     .SetInt("mana.maxValue", 200);

    s.SetInt("attack.baseValue", 35)
     .SetFloat("attack.growthRate", 1.08f)
     .SetInt("attack.maxValue", 150);
}
```

### ScriptableObjectReference 패턴 (UseConstant / ConstantValue / Variable)

```csharp
// ScriptableObjectReference 내부 구조:
// public class FloatReference
// {
//     public bool UseConstant;
//     public float ConstantValue;
//     public FloatVariable Variable;  // ScriptableObject 참조
// }
//
// public class PlayerConfigSO : ScriptableObject
// {
//     public FloatReference moveSpeed;
//     public FloatReference jumpForce;
// }

[MenuItem("MyGame/Config/플레이어 이동속도 상수로 설정")]
static void SetMoveSpeedConstant()
{
    using var s = SindyEdit.Open("Assets/Config/PlayerConfig.asset");
    if (s == null) return;

    // UseConstant = true → ConstantValue 직접 사용 (Variable 무시)
    s.SetBool("moveSpeed.UseConstant", true)
     .SetFloat("moveSpeed.ConstantValue", 6.5f);

    // UseConstant = false → Variable SO 참조 사용
    var speedVar = AssetDatabase.LoadAssetAtPath<FloatVariable>(
        "Assets/Data/Variables/PlayerSpeed.asset");

    s.SetBool("jumpForce.UseConstant", false)
     .SetRef("jumpForce.Variable", speedVar);
}
```

### 배열/리스트 크기 조정 및 원소 편집

Unity의 배열 SerializedProperty 경로 형식: `"fieldName.Array.size"`, `"fieldName.Array.data[i].subField"`

```csharp
// public class WaveConfigSO : ScriptableObject
// {
//     public WaveEntry[] waves;
// }
// [System.Serializable]
// public struct WaveEntry
// {
//     public int enemyCount;
//     public float spawnInterval;
//     public string waveLabel;
// }

[MenuItem("MyGame/Data/웨이브 설정 초기화")]
static void InitWaveConfig()
{
    using var s = SindyEdit.Open("Assets/Config/WaveConfig.asset");
    if (s == null) return;

    // 배열 크기를 5로 설정
    s.SetInt("waves.Array.size", 5);

    // 각 원소 설정
    var waveData = new (int count, float interval, string label)[]
    {
        (5,  2.0f, "Wave 1 - Intro"),
        (10, 1.5f, "Wave 2 - Normal"),
        (15, 1.2f, "Wave 3 - Hard"),
        (20, 1.0f, "Wave 4 - Surge"),
        (1,  0f,   "Wave 5 - Boss"),
    };

    for (int i = 0; i < waveData.Length; i++)
    {
        var (count, interval, label) = waveData[i];
        s.SetInt($"waves.Array.data[{i}].enemyCount", count)
         .SetFloat($"waves.Array.data[{i}].spawnInterval", interval)
         .SetString($"waves.Array.data[{i}].waveLabel", label);
    }

    Debug.Log("웨이브 설정 완료 (5단계).");
}
```

### 정확한 직렬화 경로 확인

중첩 구조나 배열의 정확한 직렬화 경로를 모를 때는 `FieldPeeker`를 사용합니다.

```csharp
// 에디터 메뉴: Sindy/Tools/Field Peeker Window
// → 에셋 또는 컴포넌트를 드래그하면 전체 직렬화 경로 목록 출력

// 코드에서 직접 확인:
// using Sindy.Editor.EditorTools;
// FieldPeeker.Print<WaveConfigSO>(myGameObject);
// FieldPeeker.Print(myScriptableObjectInstance);
```

---

## 부록 A — 직렬화 필드명 레퍼런스

자주 사용하는 Unity 컴포넌트의 직렬화 필드명입니다.

| 컴포넌트 | C# 프로퍼티 | 직렬화 경로 |
|----------|------------|------------|
| `TextMeshProUGUI` | `text` | `"m_text"` |
| `TextMeshProUGUI` | `fontSize` | `"m_fontSize"` |
| `TextMeshProUGUI` | `color` | `"m_fontColor"` |
| `TextMeshProUGUI` | `enableAutoSizing` | `"m_enableAutoSizing"` |
| `Image` | `color` | `"m_Color"` |
| `Image` | `sprite` | `"m_Sprite"` |
| `Image` | `fillAmount` | `"m_fillAmount"` |
| `Image` | `raycastTarget` | `"m_RaycastTarget"` |
| `CanvasGroup` | `alpha` | `"m_Alpha"` |
| `CanvasGroup` | `interactable` | `"m_Interactable"` |
| `CanvasGroup` | `blocksRaycasts` | `"m_BlocksRaycasts"` |
| `Button` | `interactable` | `"m_Interactable"` |
| `Animator` | `runtimeAnimatorController` | `"m_Controller"` |
| `RectTransform` | `sizeDelta` | `"m_SizeDelta"` |
| `RectTransform` | `anchoredPosition` | `"m_AnchoredPosition"` |

정확한 경로는 항상 `Sindy/Tools/Field Peeker Window`로 확인하세요.

---

## 부록 B — API 빠른 참조

### SindyEdit (static)

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `Open(path)` | `AssetEditSession?` | 경로로 세션 열기 |
| `Find(nameOrPath)` | `AssetEditSession?` | 이름으로 에셋 탐색 후 열기 |
| `Create<T>(path)` | `AssetEditSession?` | SO 생성 (덮어쓰기 포함) |
| `NewScene(path)` | `AssetEditSession?` | 씬 생성. 이미 있으면 throw |
| `NewPrefab(path, rootName)` | `AssetEditSession?` | 프리팹 생성. 이미 있으면 throw |
| `NewAsset<T>(path)` | `AssetEditSession` | SO 생성. 이미 있으면 throw |
| `Delete(path)` | `void` | 에셋 삭제. 없으면 throw |
| `Exists(path)` | `bool` | 파일 존재 여부 확인 |

### AssetEditSession — 탐색 (새 세션 반환)

| 메서드 | 설명 |
|--------|------|
| `GO(path)` | `/` 또는 `.` 구분자 경로로 GO 탐색 |
| `Root()` | 루트 GO 세션 반환 |
| `FindGameObject(name)` | 이름으로 전체 계층 재귀 탐색 |
| `Child(int)` / `Child(string)` | 현재 GO의 직계 자식 탐색 |

### AssetEditSession — 컴포넌트

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `HasComponent<T>(index?)` | `bool` | 컴포넌트 유무 확인 |
| `GetComponent<T>(action?, index?)` | `ComponentScope?` | 컴포넌트 접근. 없으면 null |
| `GetOrAddComponent<T>(action?, index?)` | `ComponentScope` | 없으면 추가 후 반환 |
| `AddComponent<T>()` | `ComponentScope` | 컴포넌트 추가 |
| `RemoveComponent<T>(index?)` | `AssetEditSession` | 컴포넌트 제거 |

### AssetEditSession — 세터 / getter (AssetEditSession 반환)

| 메서드 | 설명 |
|--------|------|
| `SetString / SetInt / SetFloat / SetBool` | 기본 타입 세터 |
| `SetColor / SetVector3 / SetVector2` | 값 타입 세터 |
| `SetRef(prop, Object)` | ObjectReference 세터 |
| `SetProperty(prop, object)` | 타입 자동 판별 세터 |
| `GetString / GetInt / GetFloat / GetBool / GetColor / GetRef<T>` | 값 읽기 |
| `GetFloat<TComp> / GetString<TComp> / ...` | 컴포넌트 타입 명시 읽기 |

### ComponentScope — 세터 / getter

| 메서드 | 반환 | 설명 |
|--------|------|------|
| `SetProperty(prop, object)` | `ComponentScope` | 타입 자동 판별 세터 |
| `SetRef(prop, Object)` | `ComponentScope` | ObjectReference 세터 |
| `GetProperty<T>(prop)` | `T` | 타입 T로 읽기 |
| `GetFloat / GetString / GetInt / GetBool / GetColor / GetRef<TRef>` | 해당 타입 | 값 읽기 |
