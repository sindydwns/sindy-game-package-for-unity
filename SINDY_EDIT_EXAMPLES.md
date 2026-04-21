# SindyEdit 실용 예시

씬·프리팹·ScriptableObject를 `SindyEdit`으로 편집하는 상황별 예시 모음.

모든 예시는 `using` 블록 패턴을 사용합니다. `Dispose` 시 변경사항이 자동으로 저장됩니다.

---

## 1. 씬 편집

### GO 경로로 프로퍼티 변경

```csharp
[MenuItem("MyGame/Edit/씬 텍스트 변경")]
static void EditSceneText()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GO("Canvas/Panel/Title")
     .SetString("m_text", "Hello World")
     .SetColor("m_fontColor", Color.white)
     .SetFloat("m_fontSize", 24f);
}
```

### GOFind로 깊은 계층 탐색

계층 위치를 모를 때 씬 전체를 재귀 탐색합니다.

```csharp
[MenuItem("MyGame/Edit/버튼 비활성화")]
static void DisableButton()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GOFind("SubmitButton")
     .SetBool("m_Interactable", false)
     .SetColor("m_Color", new Color(0.5f, 0.5f, 0.5f, 1f));
}
```

### GO 생성하고 컴포넌트 추가

`CreateGO` 후 컨텍스트가 새 GO로 이동합니다.

```csharp
[MenuItem("MyGame/Edit/DimPanel 생성")]
static void CreateDimPanel()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    // 씬 루트에 Canvas가 없으면 먼저 생성
    s.CreateGO("Canvas");

    // Canvas 아래에 DimPanel 생성 후 Image 추가
    s.GO("Canvas")
     .CreateGO("DimPanel")
     .AddComp<UnityEngine.UI.Image>()
     .SetColor("m_Color", new Color(0f, 0f, 0f, 0.6f));
}
```

### 계층을 순서대로 생성

```csharp
[MenuItem("MyGame/Edit/HUD 계층 생성")]
static void CreateHUDHierarchy()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GO("Canvas").CreateGO("HUD");

    s.GO("Canvas/HUD").CreateGO("Title")
     .AddComp<TMPro.TextMeshProUGUI>()
     .SetString("m_text", "스테이지 1")
     .SetFloat("m_fontSize", 28f)
     .SetColor("m_fontColor", new Color(0.5f, 1f, 0.9f));

    s.GO("Canvas/HUD").CreateGO("Background")
     .AddComp<UnityEngine.UI.Image>()
     .SetColor("m_Color", new Color(0f, 0f, 0f, 0.6f));
}
```

### GO 삭제

```csharp
[MenuItem("MyGame/Edit/디버그 패널 삭제")]
static void DeleteDebugPanel()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GO("Canvas/DebugPanel").DeleteGO();
    // DeleteGO 후 컨텍스트는 부모(Canvas)로 이동됨
}
```

### 값 읽기

```csharp
[MenuItem("MyGame/Edit/씬 값 읽기")]
static void ReadSceneValues()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    s.GO("Canvas/Panel/Title");
    float fontSize = s.GetFloat("m_fontSize");
    string text    = s.GetString("m_text");
    Color color    = s.GetColor("m_fontColor");

    Debug.Log($"fontSize={fontSize}, text={text}");
}
```

### Root(), Child()로 계층 탐색

```csharp
[MenuItem("MyGame/Edit/계층 탐색 예시")]
static void NavigateHierarchy()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    if (s == null) return;

    // Root: 씬 첫 번째 루트 GO로 이동
    s.Root();
    var rootName = s.GetComp<UnityEngine.Transform>()?.gameObject.name;
    Debug.Log($"루트 GO: {rootName}");

    // Child(string): 직계 자식 이름으로 이동
    s.GO("Canvas").Child("HUD").Child("Title")
     .EditComp<TMPro.TextMeshProUGUI>(tmp =>
         tmp.Set("m_text", "수정된 텍스트"));

    // Child(int): 인덱스로 직계 자식 이동
    s.GO("Canvas").Child("HUD").Child(0);
    var firstChild = s.GetComp<UnityEngine.Transform>();
    Debug.Log($"HUD 첫 번째 자식: {firstChild?.gameObject.name}");
}
```

---

## 2. 프리팹 편집

### 프리팹 열고 자식 GO 색상 변경

```csharp
[MenuItem("MyGame/Edit/GaugeBar Fill 색상 변경")]
static void EditGaugeBarFill()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    if (s == null) return;

    s.GOFind("Fill").EditComp<UnityEngine.UI.Image>(img =>
        img.Set("m_Color", new Color(0.9f, 0.25f, 0.25f)));
}
```

### Root()로 루트 GO 접근

```csharp
[MenuItem("MyGame/Edit/프리팹 루트 편집")]
static void EditPrefabRoot()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Card.prefab");
    if (s == null) return;

    // Root(): 프리팹 루트 GO로 이동
    s.Root().SetColor("m_Color", Color.white);

    // Root 기준 직계 자식으로 이동
    s.Root().Child("Background").SetColor("m_Color", new Color(0.2f, 0.2f, 0.2f));
}
```

### EditComp\<T\> 콜백으로 특정 컴포넌트 편집

```csharp
[MenuItem("MyGame/Edit/Label 텍스트 초기화")]
static void ResetLabelText()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Label.prefab");
    if (s == null) return;

    s.GOFind("Label").EditComp<TMPro.TextMeshProUGUI>(tmp =>
    {
        tmp.Set("m_text", "기본 텍스트");
        tmp.Set("m_fontSize", 18f);
        tmp.Set("m_fontColor", Color.white);
    });
}
```

### SORef로 Sprite 참조 연결

```csharp
[MenuItem("MyGame/Edit/아이콘 Sprite 연결")]
static void SetIconSprite()
{
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(
        "Assets/Art/Icons/star.png");

    using var s = SindyEdit.Open("Assets/Prefabs/UI/Icon.prefab");
    if (s == null) return;

    // 세션 레벨에서 SetRef 사용
    s.GOFind("Image").SetRef("m_Sprite", sprite);
}
```

### 컴포넌트 추가 / 제거

```csharp
[MenuItem("MyGame/Edit/CanvasGroup 추가")]
static void AddCanvasGroup()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Panel.prefab");
    if (s == null) return;

    // AddComp: 없을 때만 추가
    s.Root().AddComp<UnityEngine.CanvasGroup>();
}

[MenuItem("MyGame/Edit/불필요한 컴포넌트 제거")]
static void RemoveOutlineComp()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Button.prefab");
    if (s == null) return;

    s.GOFind("Label").RemoveComp<UnityEngine.UI.Outline>();
}
```

### 여러 프리팹 일괄 편집

```csharp
[MenuItem("MyGame/Edit/GaugeBar 색상 통일")]
static void BatchEditGaugeBars()
{
    var allPrefabs = AssetFinder.AllPrefabs<GaugeComponent>();

    foreach (var go in allPrefabs)
    {
        string path = UnityEditor.AssetDatabase.GetAssetPath(go);

        using var s = SindyEdit.Open(path);
        if (s == null) continue;

        s.GOFind("Fill").EditComp<UnityEngine.UI.Image>(img =>
            img.Set("m_Color", new Color(0.2f, 0.8f, 0.4f)));
    }

    Debug.Log($"[BatchEdit] {allPrefabs.Count}개 프리팹 색상 통일 완료.");
}
```

---

## 3. ScriptableObject 편집

### SO 열고 값 변경

```csharp
[MenuItem("MyGame/Edit/GameConfig 수정")]
static void EditGameConfig()
{
    using var s = SindyEdit.Open("Assets/Config/GameConfig.asset");
    if (s == null) return;

    s.SetFloat("gravity", 9.81f)
     .SetInt("maxHealth", 200)
     .SetBool("godMode", false)
     .SetString("gameVersion", "1.2.0");
}
```

### SO 신규 생성 (Create\<T\>)

디렉터리가 없으면 자동 생성합니다.

```csharp
[MenuItem("MyGame/Edit/PlayerSpeed SO 생성")]
static void CreateSpeedSO()
{
    using var s = SindyEdit.Create<FloatVariable>("Assets/Data/PlayerSpeed.asset");
    if (s == null) return;

    s.SetFloat("Value", 5f)
     .SetString("description", "플레이어 이동 속도");
}
```

### 새 씬 / 프리팹 생성

```csharp
[MenuItem("MyGame/Edit/빈 씬 생성")]
static void CreateEmptyScene()
{
    using var s = SindyEdit.NewScene("Assets/Scenes/NewLevel.unity");
    if (s == null) return;

    s.CreateGO("Canvas");
    s.CreateGO("EventSystem");
}

[MenuItem("MyGame/Edit/빈 프리팹 생성")]
static void CreateEmptyPrefab()
{
    using var s = SindyEdit.NewPrefab("Assets/Prefabs/UI/NewButton.prefab", "Button");
    if (s == null) return;

    s.CreateGO("Label")
     .AddComp<TMPro.TextMeshProUGUI>()
     .SetString("m_text", "버튼");
}
```

### SO 삭제 (DeleteAsset)

```csharp
[MenuItem("MyGame/Edit/임시 SO 삭제")]
static void DeleteTempSO()
{
    using var s = SindyEdit.Open("Assets/Data/Temp.asset");
    if (s == null) return;

    s.DeleteAsset();
    // DeleteAsset 호출 후 세션은 무효화됨
}
```

### 여러 SO 일괄 수정

```csharp
[MenuItem("MyGame/Edit/모든 FloatVariable 초기화")]
static void ResetAllFloatVars()
{
    var guids = UnityEditor.AssetDatabase.FindAssets(
        "t:ScriptableObject", new[] { "Assets/Data" });

    foreach (var guid in guids)
    {
        var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
        using var s = SindyEdit.Open(path);
        if (s == null) continue;

        s.SetBool("isEnabled", true);
    }
}
```

### 중첩 경로 (dot notation)

```csharp
[MenuItem("MyGame/Edit/중첩 필드 편집")]
static void EditNestedField()
{
    using var s = SindyEdit.Open("Assets/Config/PlayerConfig.asset");
    if (s == null) return;

    // "필드명.서브필드명" 경로로 중첩 구조 접근
    s.SetBool("healthRef.UseConstant", true)
     .SetFloat("healthRef.ConstantValue", 100f);
}
```

---

## 4. 탐색 방법 비교

| 방법 | 탐색 방식 | 언제 쓰나 |
|------|-----------|-----------|
| `GO("Canvas/Panel/Title")` | 경로로 정확히 탐색 (`/` 또는 `.` 구분자) | 계층 구조를 알 때 |
| `GOFind("SubmitButton")` | 이름으로 전체 계층 재귀 탐색 | 위치를 모를 때, 깊은 계층 |
| `Root()` | 루트 GO로 이동 | 루트부터 순서대로 탐색할 때 |
| `Root().Child("Fill")` | 루트에서 직계 자식 이름으로 이동 | 계층을 단계별로 내려갈 때 |
| `Root().Child(0)` | 인덱스로 직계 자식 이동 | 이름 없는 자식 접근 |

```csharp
// GO() — 경로로 직접 접근. 가장 빠름
s.GO("Canvas/HUD/Title").SetString("m_text", "Ready");

// GOFind() — 이름으로 재귀 탐색
s.GOFind("HealthBar").SetFloat("m_fillAmount", 0.75f);

// Root().Child() — 계층을 명시적으로 내려갈 때
s.Root().Child("Overlay").Child("Panel").Child(0).SetColor("m_Color", Color.red);
```

### Find()로 에셋 이름 자동 탐색

에셋 경로를 몰라도 이름만으로 탐색합니다. 우선순위: 프리팹 → 씬 → ScriptableObject.

```csharp
[MenuItem("MyGame/Edit/이름으로 탐색")]
static void FindByName()
{
    using var s = SindyEdit.Find("GaugeBar");
    if (s == null) return;

    s.GOFind("Fill").SetColor("m_Color", Color.cyan);
}
```

---

## 5. HTTP IPC로 원격 실행

Unity 에디터가 열려 있는 상태에서 외부(터미널, AI)에서 직접 편집합니다.  
기본 포트: **6060** (Edit > Preferences > Sindy에서 변경)

### 연결 확인

```bash
curl http://localhost:6060/ping
# {"id":"","success":true,"message":"pong","timestamp":"..."}
```

### /execute — static 메서드 실행

```bash
# 등록된 메서드 실행
curl -X POST http://localhost:6060/execute \
  -H "Content-Type: application/json" \
  -d '{"method":"Sindy.Editor.Examples.Example_PrefabEdit.RunBatchEdit"}'
```

### /edit — 프로퍼티 직접 편집

```bash
# SO 값 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"Assets/Config/GameConfig.asset","prop":"maxHealth","value":300}'

# 씬 텍스트 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"Assets/Scenes/Main.unity","go":"Canvas/Panel/Title","prop":"m_text","value":"Stage 1"}'

# 이름으로 에셋 자동 탐색
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset":"GaugeBar","go":"Fill/Image","prop":"m_Color","value":[0.2,0.8,0.2,1.0]}'
```

### /edit value 타입 레퍼런스

| C# 타입 | JSON 표기 | 예시 |
|---------|-----------|------|
| `string` | `"..."` | `"value":"Hello"` |
| `bool` | `true` / `false` | `"value":true` |
| `int` | 정수 | `"value":100` |
| `float` | 소수점 | `"value":3.14` |
| `Vector2` | 2개 float 배열 | `"value":[1.0,2.0]` |
| `Vector3` | 3개 float 배열 | `"value":[1.0,2.0,3.0]` |
| `Color` (RGBA) | 4개 float 배열 | `"value":[1.0,0.5,0.0,1.0]` |
