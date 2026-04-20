# SindyEdit 실용 예시

씬·프리팹·ScriptableObject를 동일한 API로 편집하는 통합 파사드 예시 모음.

---

## 1. 씬 편집

### 씬 열고 GO 찾아서 프로퍼티 변경

```csharp
using Sindy.Editor.EditorTools;
using UnityEditor;

[MenuItem("Sindy/Examples/씬 텍스트 변경")]
static void EditSceneText()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    s.GO("Canvas/Panel/Title")
     .SOString("m_text", "Hello World")
     .SOColor("m_fontColor", Color.white);
}
```

### GOFind로 깊은 계층 GO 탐색

계층 위치를 모를 때. 씬 전체를 재귀 탐색한다.

```csharp
[MenuItem("Sindy/Examples/씬 깊은 GO 탐색")]
static void FindDeepGO()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    s.GOFind("SubmitButton")
     .SOBool("m_Interactable", false)
     .SOColor("m_Color", new Color(0.5f, 0.5f, 0.5f, 1f));
}
```

### GO 새로 생성하고 컴포넌트 추가

`CreateGO` 후 컨텍스트가 새 GO로 이동한다.

```csharp
[MenuItem("Sindy/Examples/씬 GO 생성")]
static void CreateGOInScene()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    s.GO("Canvas")
     .CreateGO("DimPanel")
     .AddComp<UnityEngine.UI.Image>()
     .SOColor("m_Color", new Color(0f, 0f, 0f, 0.6f));
}
```

### 값 읽기

```csharp
[MenuItem("Sindy/Examples/씬 값 읽기")]
static void ReadSceneValues()
{
    using var s = SindyEdit.Open("Assets/Scenes/Main.unity");
    s.GO("Canvas/Panel/Title");

    float fontSize = s.GetFloat("m_fontSize");
    string text    = s.GetString("m_text");
    bool active    = s.GetBool("m_IsActive");
    Color color    = s.GetColor("m_fontColor");

    Debug.Log($"size={fontSize} text={text} active={active}");
}
```

---

## 2. 프리팹 편집

### 프리팹 열고 루트 컴포넌트 수정

```csharp
[MenuItem("Sindy/Examples/프리팹 루트 수정")]
static void EditPrefabRoot()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    s.Root()
     .SOColor("m_Color", Color.white)
     .SOFloat("m_fillAmount", 1f);
}
```

### Child()로 자식 탐색 후 Image 색상 변경

```csharp
[MenuItem("Sindy/Examples/프리팹 자식 Image 색상")]
static void EditChildImage()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/GaugeBar.prefab");
    s.Root()
     .Child("Fill")       // 직계 자식 "Fill"로 이동
     .Child(0)            // Fill의 첫 번째 자식으로 이동
     .SOColor("m_Color", Color.green);
}
```

### WithComp\<T\> 콜백으로 컴포넌트 프로퍼티 편집

특정 컴포넌트를 명시적으로 지정해 편집할 때.

```csharp
[MenuItem("Sindy/Examples/WithComp 패턴")]
static void EditWithComp()
{
    using var s = SindyEdit.Open("Assets/Prefabs/UI/Card.prefab");
    s.GOFind("Icon").WithComp<UnityEngine.UI.Image>(img =>
    {
        img.Set("m_Color", new Color(1f, 0.8f, 0.2f, 1f));
    });
}
```

### SORef로 Sprite 참조 연결

```csharp
[MenuItem("Sindy/Examples/Sprite 참조 연결")]
static void SetSpriteRef()
{
    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Icons/star.png");

    using var s = SindyEdit.Open("Assets/Prefabs/UI/Icon.prefab");
    s.GOFind("Image").SORef("m_Sprite", sprite);
}
```

---

## 3. ScriptableObject 편집

### SO 열고 float/string/bool 값 변경

```csharp
[MenuItem("Sindy/Examples/SO 값 변경")]
static void EditSO()
{
    using var s = SindyEdit.Open("Assets/Config/GameConfig.asset");
    s.SOFloat("gravity", 9.81f)
     .SOInt("maxHealth", 200)
     .SOBool("godMode", false)
     .SOString("gameVersion", "1.2.0");
}
```

### SO 새로 생성 (Create\<T\>)

디렉터리가 없으면 자동 생성된다.

```csharp
[MenuItem("Sindy/Examples/SO 생성")]
static void CreateSO()
{
    using var s = SindyEdit.Create<FloatVariable>("Assets/Data/PlayerSpeed.asset");
    s.SOFloat("value", 5f);
}
```

### AssetDatabase로 여러 SO 일괄 수정

```csharp
[MenuItem("Sindy/Examples/SO 일괄 수정")]
static void BatchEditSOs()
{
    var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Config" });
    foreach (var guid in guids)
    {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        using var s = SindyEdit.Open(path);
        s.SOBool("isEnabled", true);
    }
}
```

---

## 4. 탐색 방법 비교

| 방법 | 탐색 방식 | 언제 쓰나 |
|---|---|---|
| `GO("Canvas/Panel/Title")` | 경로로 정확히 탐색 (/ 또는 . 구분자) | 계층 구조를 알 때 |
| `GOFind("SubmitButton")` | 이름으로 전체 계층 재귀 탐색 | 위치를 모를 때, 깊은 계층 |
| `Root().Child("Fill")` | 루트에서 직계 자식만 이동 | 계층을 단계별로 명확히 내려갈 때 |
| `Root().Child(0)` | 인덱스로 직계 자식 이동 | 이름 없는 자식 접근 |

```csharp
// GO() — 경로로 직접 접근. 가장 빠름
s.GO("Canvas/HUD/Title").SOString("m_text", "Ready");

// GOFind() — 이름으로 재귀 탐색. 위치가 불확실할 때
s.GOFind("HealthBar").SOFloat("m_fillAmount", 0.75f);

// Root().Child() — 명시적 계층 탐색. 중간 경로를 확인하며 내려갈 때
s.Root().Child("Overlay").Child("Panel").Child(0).SOColor("m_Color", Color.red);
```

### Find()로 이름 자동 탐색

에셋 경로를 몰라도 이름만으로 탐색. 우선순위: 프리팹 → 씬 → ScriptableObject

```csharp
[MenuItem("Sindy/Examples/이름으로 탐색")]
static void FindByName()
{
    using var s = SindyEdit.Find("GaugeBar");   // 프리팹 자동 탐색
    s.GOFind("Fill").SOColor("m_Color", Color.cyan);
}
```

---

## 5. HTTP IPC로 원격 실행

Unity 에디터가 열려 있는 상태에서 외부(터미널, AI)에서 직접 편집 가능.  
기본 포트: **6060** (Edit > Preferences > Sindy에서 변경)

### 연결 확인

```bash
curl http://localhost:6060/ping
# {"id":"","success":true,"message":"pong","timestamp":"..."}
```

### /execute — static 메서드 실행

```bash
# SindyEditTest.TestSceneEdit 실행
curl -X POST http://localhost:6060/execute \
  -H "Content-Type: application/json" \
  -d '{"method": "Sindy.Editor.EditorTools.SindyEditTest.TestSceneEdit"}'

# SindyEditTest.TestCreateSO 실행
curl -X POST http://localhost:6060/execute \
  -H "Content-Type: application/json" \
  -d '{"method": "Sindy.Editor.EditorTools.SindyEditTest.TestCreateSO"}'
```

### /edit — 프로퍼티 직접 편집

```bash
# ScriptableObject 프로퍼티 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset": "Assets/Config/GameConfig.asset", "prop": "maxHealth", "value": 300}'

# 씬 GO 텍스트 변경
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset": "Assets/Scenes/Main.unity", "go": "Canvas/Panel/Title", "prop": "m_text", "value": "Stage 1"}'

# 이름으로 에셋 탐색 후 편집 (경로 불필요)
curl -X POST http://localhost:6060/edit \
  -H "Content-Type: application/json" \
  -d '{"asset": "GaugeBar", "go": "Fill/Image", "prop": "m_Color", "value": [0.2, 0.8, 0.2, 1.0]}'
```

### /edit value 타입 레퍼런스

| C# 타입 | JSON 표기 | 예시 |
|---|---|---|
| `string` | `"..."` | `"value": "Hello"` |
| `bool` | `true` / `false` | `"value": true` |
| `int` | 정수 | `"value": 100` |
| `float` | 소수점 포함 숫자 | `"value": 3.14` |
| `Vector2` | 2개 float 배열 | `"value": [1.0, 2.0]` |
| `Vector3` | 3개 float 배열 | `"value": [1.0, 2.0, 3.0]` |
| `Color` (RGBA) | 4개 float 배열 | `"value": [1.0, 0.5, 0.0, 1.0]` |
