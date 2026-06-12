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

부품 프리팹 카탈로그 — Prefabs/
├─ ShopFrame               틀: 헤더·스크롤러(가상화 기계장치)·로그바·빈 상세 패널 영역
│                          + 반응형 variant 좌표(ResponsiveLayoutFeatureView, 디자이너 몫)
└─ CaptionRow · CaptionSmall · InfoRow · QtyRow · BuyButton · BgmRow · SkipRow
                           상세 패널 행 부품 — 내부 FeatureView 배선·키 매핑 완성 상태

런타임 — Open()이 만들어낸 화면
└─ ShopFrame 인스턴스
   ├─ Header / Scroller / LogBar   ← 프리팹에 미리 존재, 모델만 주입 (하이브리드)
   └─ Detail                        ← Blueprint가 부품 10행을 순서대로 조립
      CaptionRow → InfoRow → CaptionSmall → QtyRow → BuyButton → CaptionSmall
      → BgmRow → SkipRow → CaptionSmall → PartContainer(생명주기 데모 버튼 3개)
```

씬에 상점 화면은 존재하지 않습니다. 카탈로그는 이름→프리팹 사전이라
코드가 `"QtyRow"` 같은 문자열만으로 부품을 찾습니다.

## ShopDemoController — 디자인과 기능의 분리

코드가 두 구역으로 나뉘어 있는 것이 이 데모의 핵심입니다.

| 구역 | 메서드 | 내용 | 금지 |
|---|---|---|---|
| **디자인** | `BuildDetailPanel()` | 수직 플로우·간격 14·행 순서 등 구조와 배치 선언 | 로직 0줄 |
| **기능** | `BuildModel()` | 골드·수량 상태(PropModel), 버튼 구독, 구매 로직 | 좌표·간격 0줄 |

```csharp
// 디자인 — 설계도(데이터). 선언 시점엔 아무것도 생성되지 않는다.
private ComponentBlueprint BuildDetailPanel() => ComponentBlueprint
    .Create("DetailPanel")
        .Layout(Direction.Vertical, spacing: 14)
        .Padding(top: 12, right: 32, bottom: 16, left: 32)
    .Patch("caption", "CaptionRow").WithModel(() => Models.Label("..."))
    .Patch("info", "InfoRow").WithModel(BuildInfoModel)
    ...
```

## 실행 흐름

`Start()`의 `BuildBlueprint().Open()` 한 줄에서 모든 것이 만납니다.

1. **모델 트리 생성** — 루트 팩토리(`BuildModel`)가 먼저 실행되어 상태·구독이 준비되고,
   패치 팩토리들이 그 필드를 ViewModel로 감싼다.
2. **틀 인스턴스화** — ShopFrame이 Canvas 아래 생성되고 모델이 바인딩된다.
   틀에 이미 있는 키(title/gold/list/log/detail)는 모델만 주입된다.
3. **부품 조립** — 설계도의 각 Patch가 카탈로그에서 부품을 찾아
   상세 패널에 선언 순서대로 인스턴스화·부착·바인딩한다.

## 생명주기 3패턴 — 상세 패널 하단 데모 버튼

| 버튼 | 패턴 | 코드 | 일어나는 일 |
|---|---|---|---|
| 상점 교체 | **값 변경** | `SwapShop()` | 재바인딩 없이 PropModel 값·셀 목록만 교체 — 기본 사용법 |
| 재시작 | **재-Open** | `Reopen()` | `Bind(null)` → 파괴 → 모델 `Dispose()` → 설계도 재실행. 상태 초기화 |
| 모델 재주입 | **BuildModelTree** | `Reinject()` | 설계도가 새 모델 트리(레이아웃 포함)를 생성 → 기존 인스턴스에 `Bind`. 뷰 유지, 내용 통째 교체 |

재-Open/재주입은 버튼 OnClick 방출 중 모델을 Dispose하지 않도록
`pendingAction`으로 한 프레임 지연 후 실행한다 (`Update` 참조).

### 재사용·재주입 시 주의

- **같은 Blueprint 다회 Open**: Blueprint 자체는 안전하다(모델은 팩토리로 매번 새로 생성,
  레이아웃은 클론 적용). 단 **이 데모의 컨트롤러는 단일 인스턴스 전제** —
  모델 팩토리가 컨트롤러 필드(gold, qty...)를 공유하므로 두 번 Open하면
  필드가 마지막 인스턴스를 가리킨다. 다중 인스턴스가 필요하면 모델 컨텍스트를
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
| `Scripts/ShopDemoController.cs` | 설계도(디자인) + 모델(기능) + 상호작용 |
| `Scripts/ShopCells.cs` | 스크롤러 셀 VM 정적 팩토리 — 타 프로젝트 복사용 표준 패턴 |
| `Scripts/ShopCellKeys.cs` | 셀 키 const 모음 |
| `Prefabs/` | 틀 + 부품 프리팹 8종, 셀 프리팹 3종 |
| `ShopCellCatalog.asset` | 스크롤러 셀 키→프리팹 카탈로그 |
| `ItemSectionOption.asset` 등 | 스크롤러 섹션 레이아웃 설정 |
