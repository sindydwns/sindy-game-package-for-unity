# ComponentShowcase — 미니 상점 데모

SindyComponent의 **사용 패턴**을 보여주는 쇼케이스입니다. 개별 Feature의 동작이 아니라,
부품 프리팹을 코드에서 조합해 화면을 만들어내는 ComponentBlueprint 중심의 개발 흐름을 시연합니다.

> 상세 규칙: [SINDY_COMPONENT.md §프리팹 조합](../../SINDY_COMPONENT.md) · 실습: [SINDY_COMPONENT_TUTORIAL.md Step 8](../../SINDY_COMPONENT_TUTORIAL.md)

## 전제 — 두 세계

이 패키지에서 UI는 항상 두 세계로 나뉩니다.

- **모델 세계** — 순수 C# 객체. `ViewModel` + 능력 단위 Feature(`TextFeature`, `ButtonFeature`...).
  "골드가 12,345다, 구매 버튼이 눌렸다" 같은 상태와 이벤트만 다룬다.
- **뷰 세계** — GameObject. 각 오브젝트에 허브(`SindyComponent`) 하나와 FeatureView들이 붙는다.
  허브에 모델을 `Bind`하면 FeatureView가 같은 이름의 Feature를 찾아 구독하고,
  이후 모델 값이 바뀔 때마다 화면이 알아서 갱신된다.

Controller는 GameObject를 직접 만지지 않습니다. 모델 값 변경까지만 책임집니다.

## 구조

```
씬 ComponentShowcase.unity — 셸만 남는다
├─ Canvas                  배경 + UI 레이어 (생성 위치)
├─ ComponentManager        레이어 RectTransform + 프리팹 카탈로그(GameObjectCollection)
└─ ShopDemo                ShopDemoController — 설계도 작성·실행

프리팹 — Prefabs/
├─ ShopFrame               틀: 헤더·스크롤러(가상화 기계장치)·로그바·빈 상세 패널 슬롯(detail)
│                          + 반응형 variant 좌표(ResponsiveLayoutFeatureView, 디자이너 몫)
└─ ItemCell · HeaderCell · BannerCell
                           스크롤러 셀 프리팹 — 섹션 콘텐츠/헤더/배너

상세 패널 부품은 별도 프리팹이 아니라 **기본 부품 키트**(PartKeys.Container/Icon/Label/
Spacer/Toggle)와 합성 Blueprint(SindyKit.ButtonLabel/ToggleRow)를 코드에서 경로로 조합합니다.

런타임 — Open()이 만들어낸 화면
└─ ShopFrame 인스턴스
   ├─ title / gold / list / log     ← 프리팹에 미리 존재, 루트 모델 트리로 주입
   └─ detail                        ← 사전 배치된 슬롯(Patch("detail"))을 재사용해 행을 순서대로 조립
      info(아이콘+이름+티어+설명) → qty(수량 ±) → buy(구매 버튼)
      → skip(구매 확인 생략 토글) → demo(생명주기 3패턴 버튼)
```

씬에 상점 화면은 존재하지 않습니다. 카탈로그는 이름→프리팹 사전이라
코드가 `PartKeys.Label`·`ShopCellKeys.Item` 같은 키 문자열만으로 부품·셀을 찾습니다.

## 디자인과 기능의 분리 — 파일로 나뉜다

코드가 디자인과 기능 두 축으로 분리돼 있는 것이 이 데모의 핵심입니다. 두 축은 **파일로도 나뉩니다.**

| 축 | 위치 | 내용 | 금지 |
|---|---|---|---|
| **디자인** | `ShopDemoController.BuildBlueprint()` | 수직 플로우·간격 14·행 순서 등 구조와 배치 선언 | 로직 0줄 |
| **기능** | `ShopModel` | 골드·수량 상태(PropModel), 버튼 구독, 구매 로직 | 좌표·간격 0줄 |

`Controller`는 생명주기·설계도·뷰 연결만 담당하고, 반응형 상태와 상점 로직은 전부 `ShopModel` 한 곳에 모입니다.
`BuildModel()`은 새 `ShopModel`을 만들어 그 `Root` 트리를 돌려줄 뿐이고, 설계도의 패치 팩토리는
`currentModel`을 **지연 참조**하므로 재주입 시 항상 최신 세대의 모델을 가리킵니다.

```csharp
// 디자인 — 설계도(데이터). 선언 시점엔 아무것도 생성되지 않는다.
private ComponentBlueprint BuildBlueprint() => ComponentBlueprint
    .Create("ShopFrame")
    .WithModel(BuildModel)                       // 루트 팩토리 = new ShopModel(...).Root
    // 사전 배치된 detail 슬롯을 재사용(Patch("detail"))하고 그 안에 내용을 채운다
    .Patch("detail")
        .Layout(Direction.Vertical, spacing: 14)
        .Padding(top: 12, right: 32, bottom: 16, left: 32)
        .WithModel(() => new ViewModel())
    // 복합 행은 전용 프리팹 대신 원자 부품을 경로로 중첩 조합한다
    .Patch("detail.info", PartKeys.Container).Layout(Direction.Vertical, spacing: 6).WithModel(() => new ViewModel())
    .Patch("detail.info.name", PartKeys.Label).WithModel(() => Models.Empty().AddTextFeature(currentModel.ItemName))
    // 버튼·토글 행은 합성 Blueprint로 치환
    .Patch("detail.buy", SindyKit.ButtonLabel).WithModel(() => new ViewModel().With(currentModel.BuyBtn))
    .Patch("detail.buy.label", PartKeys.Label).WithModel(() => Models.Empty().AddTextFeature(currentModel.BuyLabel))
    ...
```

## 실행 흐름

`Start()`의 `BuildBlueprint().Open()` 한 줄에서 모든 것이 만납니다.

1. **모델 트리 생성** — 루트 팩토리(`BuildModel`)가 먼저 실행되어 상태·구독이 준비되고,
   패치 팩토리들이 그 필드를 ViewModel로 감싼다.
2. **틀 인스턴스화** — ShopFrame이 Canvas 아래 생성되고 모델이 바인딩된다.
   틀에 미리 있는 키(title/gold/list/log)는 루트 모델 트리로 주입되고,
   detail 슬롯은 `Patch("detail")`로 재사용되어 레이아웃·모델만 주입된다.
3. **부품 조립** — 설계도의 각 `Patch(path, prefab)`가 카탈로그에서 부품을 찾아
   상세 패널(detail.*)에 선언 순서대로 인스턴스화·부착·바인딩한다.

## 생명주기 3패턴 — 상세 패널 하단 데모 버튼

| 버튼 | 패턴 | 코드 | 일어나는 일 |
|---|---|---|---|
| 상점 교체 | **값 변경** | `SwapShop()` | 재바인딩 없이 PropModel 값·셀 목록만 교체 — 기본 사용법 |
| 재시작 | **재-Open** | `blueprint.ReopenNextFrame(instance, onOpened:)` | 파괴 → 모델 `Dispose()` → 설계도 재실행. 상태 초기화 |
| 모델 재주입 | **BuildModelTree** | `instance.RebindNextFrame(blueprint.BuildModelTree(), onRebound:)` | 설계도가 새 모델 트리(레이아웃 포함)를 생성 → 기존 인스턴스에 `Bind`. 뷰 유지, 내용 통째 교체 |

`ReopenNextFrame`/`RebindNextFrame`은 교체·파괴를 다음 프레임으로 미루므로 버튼 `OnClick` 방출 중에
호출해도 방출 스택 안에서 자기 모델을 파괴하는 재진입 오류가 없다. 둘 다 `disposeOld`(기본 true)로
이전 모델을 자동 정리한다(`RebindNextFrame`은 새 모델이 이전 모델과 같은 인스턴스면 건너뛴다).

### 재사용·재주입 시 주의

- **같은 Blueprint 다회 Open**: Blueprint 자체는 안전하다(모델은 팩토리로 매번 새로 생성,
  레이아웃은 클론 적용). 단 **이 데모의 컨트롤러는 단일 인스턴스 전제** —
  패치 팩토리가 컨트롤러의 `currentModel` 필드를 지연 참조하므로 두 번 Open하면
  그 필드가 마지막 세대의 `ShopModel`을 가리킨다. 다중 인스턴스가 필요하면 모델 컨텍스트를
  Open 단위 객체로 캡슐화할 것.
- **모델 Dispose 소유권**: Open()/BuildModelTree()가 만든 모델은 인스턴스 GameObject
  파괴 시 자동 Dispose되지 않는다. 호출자가 모델을 보관했다가
  `Bind(null)` → `Dispose()` 순으로 정리해야 한다 (`OnDestroy`/`Reopen` 참조).
- **재바인딩은 구조를 바꾸지 못한다**: 부품 인스턴스화는 Open()에서만 일어난다 —
  새 키는 무시, 빠진 키는 경고 후 빈 상태. 구조가 바뀌는 교체는 재-Open으로.
  재주입할 모델은 손으로 만들지 말고 같은 설계도의 `BuildModelTree()`로 만들 것 —
  레이아웃(디자인)을 설계도가 책임지므로 `new LayoutFeature()`를 쓸 일이 없다.

## 파일 안내

| 파일 | 역할 |
|---|---|
| `ComponentShowcase.unity` | 셸 씬 |
| `Scripts/ShopDemoController.cs` | 생명주기 + 설계도(디자인) + 뷰 연결 |
| `Scripts/ShopModel.cs` | 반응형 상태 + 상점 로직(기능) — 한곳에 모음 |
| `Scripts/ShopCells.cs` | 스크롤러 셀 VM 정적 팩토리 — 타 프로젝트 복사용 표준 패턴 |
| `Scripts/ShopCellKeys.cs` | 셀 키 const 모음 |
| `Prefabs/` | 틀(`ShopFrame`) + 셀 프리팹 3종(`ItemCell`/`HeaderCell`/`BannerCell`) |
| `ShopCellCatalog.asset` | 스크롤러 셀 키→프리팹 카탈로그 |
| `ItemSectionOption.asset` · `BannerSectionOption.asset` | 스크롤러 섹션 레이아웃 설정 |
