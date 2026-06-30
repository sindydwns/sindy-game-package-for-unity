# 기본 부품 키트 (Parts)

`ComponentBlueprint`로 UI를 조합하려면 카탈로그에 등록된 **부품 프리팹**이 필요합니다. 이 폴더는 라벨·아이콘·버튼·컨테이너 같은 자주 쓰는 부품을 미리 만들어 둬, 패키지를 받자마자 `SindyComponent`를 조합해 화면을 만들 수 있게 합니다.

> 조합 규칙의 상세는 [SINDY_COMPONENT.md §프리팹 조합](../../../SINDY_COMPONENT.md#프리팹-조합--componentblueprint--layoutfeature)을 참고하세요. 이 문서는 동봉된 기본 부품 자체를 설명합니다.

## 구성

```
Runtime/View/Parts/
├─ Prefabs/                  원자 부품 9종 (키 = 프리팹 이름)
├─ Fonts/SindyDefault SDF    한글 표시용 Dynamic TMP 폰트 (Spoqa 기반)
├─ SindyDefaultParts.prefab  부품 카탈로그 (GameObjectCollection)
├─ PartKeys.cs               부품 키 상수 (오타 방지)
└─ SindyKit.cs               자주 쓰는 합성 Blueprint 6종
```

## 원자 부품 9종

각 부품은 `SindyComponent`(허브) 하나와 해당 FeatureView가 배선된 상태입니다. 모델만 `Bind`하면 동작합니다.

| 키 (`PartKeys`) | 구성 | 받는 모델 |
|---|---|---|
| `label` | TMP_Text + TextFeatureView | `TextFeature` (텍스트·폰트크기) |
| `icon` | Image + ImageFeatureView | `ImageFeature` (스프라이트) |
| `button` | Image(bg) + ButtonFeatureView + InteractableFeatureView | `ButtonFeature` / `InteractableFeature` |
| `panel` | Image(bg) + LayoutFeatureView | (Blueprint 레이아웃 + 자식) |
| `container` | LayoutFeatureView (투명) | (Blueprint 레이아웃 + 자식) |
| `spacer` | LayoutElement(flexible) | — (남는 공간 차지) |
| `divider` | Image(얇은 선) | — (구분선) |
| `gauge` | bg + Fill(Image Filled) + GaugeFeatureView | `GaugeFeature` (0~1 비율) |
| `toggle` | uGUI Toggle + ToggleFeatureView | `ToggleFeature` (on/off) |

모든 부품은 모바일 기준입니다 — 버튼·토글·행은 터치 타겟 최소 96px(≈48dp@2x)을 보장하고, 절대 크기 대신 `LayoutGroup`/`LayoutElement`에 의존하므로 프로젝트의 `CanvasScaler`가 최종 크기를 결정합니다.

## 합성 Blueprint 6종 (`SindyKit`)

원자 부품을 조합한 재사용 설계도입니다. **디자인(레이아웃)만 정의하며 모델은 호출부가 주입**합니다.

| Blueprint | 구조 |
|---|---|
| `Card` | panel + icon + label (세로) |
| `LabeledRow` | container + icon + label(남는 폭 채움) (가로) |
| `ButtonLabel` | button + 가운데 label |
| `ToggleRow` | container + label(남는 폭 채움) + toggle (가로) |
| `Popup` | panel + 제목 + 내용 컨테이너 + 버튼 행 |
| `Dialog` | Popup + 취소/확인 라벨 버튼 |

## 사용법

### 1. 카탈로그 연결

씬의 `ComponentManager`의 `prefabs` 필드에 `SindyDefaultParts`를 연결합니다. 프로젝트 고유 부품도 함께 쓰려면, 자체 `GameObjectCollection`에 기본 부품들을 추가해 함께 등록하면 됩니다. (기본 부품 사용 여부는 프로젝트의 선택입니다.)

### 2. Blueprint로 조합

```csharp
// 카드 한 장 — 디자인은 SindyKit이, 내용은 모델이 책임진다
ComponentBlueprint.Create(SindyKit.Card)
    .Patch("icon", PartKeys.Icon).WithModel(() => Models.Empty().AddImageFeature(itemSprite))
    .Patch("label", PartKeys.Label).WithModel(() => Models.Empty().AddTextFeature("파이어볼"))
    .Open();

// 팝업 — 제목 크기는 모델에서 지정(Models.Label의 fontSize 인자)
ComponentBlueprint.Create(SindyKit.Dialog)
    .Patch("title", PartKeys.Label).WithModel(() => Models.Empty().AddTextFeature("삭제할까요?", 48))
    .Open();
```

### 3. 스타일 변경 — 프리팹 Variant

기본 부품은 흰/회색 무채색 + Unity 내장 스프라이트로 중립적입니다. 색·폰트·스프라이트를 바꾸려면 부품 프리팹의 **Prefab Variant**를 만들어 해당 값만 오버라이드하고, 그 Variant들을 담은 카탈로그를 `ComponentManager`에 연결하세요. 런타임 비용 없이 에디터에서 바로 보이며(WYSIWYG), 기본 부품의 구조·배선은 그대로 상속됩니다.

> 동적인 색은 Variant가 아니라 `ColorFeature`로, 동적 폰트 크기는 `TextFeature`의 `fontSize`로 모델에서 구동할 수 있습니다. Variant는 "기본 룩"을 담당합니다.

## 폰트와 한글

`label`은 Spoqa Han Sans Neo 기반 **Dynamic** TMP 폰트(`SindyDefault SDF`)를 사용합니다. 동적 아틀라스라 서브셋 누락 없이 어떤 한글이든 렌더됩니다. 다른 폰트를 쓰려면 `label`의 Variant에서 폰트 에셋만 교체하세요. (라이선스: `Fonts/SpoqaHanSansNeo-LICENSE.txt`)

## 한계·주의

- `Popup`/`Card`의 제목·라벨 폰트 크기는 기본 36입니다. 제목을 키우려면 모델에서 `Models.Empty().AddTextFeature("제목", 48)`처럼 크기를 지정하거나, 큰 폰트 `label` Variant를 별도 키로 등록하세요.
- `LabeledRow`/`ToggleRow`의 라벨은 `Flexible(1)`로 남는 가로폭을 채웁니다. 더 복잡한 다자식 행(여러 칼럼 등)은 SindyKit 대신 원자 부품을 직접 조합하세요.
- 부품 인스턴스의 GameObject 이름은 `키 (프리팹명)` 형식입니다(예: `name (label)`) — 하이라키에서 어떤 키에 어떤 부품이 붙었는지 바로 보입니다. 코드에서 자식을 찾을 때는 이름이 아니라 `hub.TryGetView("키", out var child)`로 키 기준 조회를 쓰세요.
