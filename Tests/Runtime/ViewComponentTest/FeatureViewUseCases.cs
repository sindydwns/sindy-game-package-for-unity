using R3;
using Sindy.View;
using Sindy.View.Features;
using UnityEngine;

namespace Sindy.Test
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // FeatureView 아키텍처 사용 예시 모음
    //
    // 핵심: 전용 모델 클래스 없이 "ViewModel + Feature 조합"으로 모든 UI 모델을 만든다.
    // 뷰 측은 GameObject에 FeatureView를 부착하는 것으로 끝난다 (작성 클래스 0개).
    //
    // ── 구 데모(ComponentBuilderUseCases)에서 발견된 필요 기능 위시리스트 (보존) ──
    //
    // [NEED] Scrollable         — Patch 대상에 ScrollRect를 자동 부여
    // [NEED] Grid(columns)      — GridLayoutGroup 지원
    // [NEED] Background(type)   — 팝업 뒤 딤/블러 배경 제어
    // [NEED] OnClose(Action)    — 팝업 닫힘 콜백
    // [NEED] Duration(sec)      — 일정 시간 후 자동 닫힘
    // [NEED] Stretch / Fill     — LayoutElement flexible 비율 지정
    // [NEED] Input(placeholder) — 텍스트 입력 필드 (InputFeature/InputFeatureView 후보)
    // [NEED] Separator          — 구분선 요소 삽입
    // [NEED] Conditional(bool)  — 조건부 Patch (false면 스킵)
    // [NEED] Badge(path)        — RedDot 바인딩 단축
    // [NEED] Animate(type)      — 열기/닫기 애니메이션 (BlinkFeature처럼 Feature 쌍 후보)
    // [NEED] Draggable          — 드래그 이동
    // [NEED] FitContent         — ContentSizeFitter 자동 부여
    //
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    static class FeatureViewUseCases
    {
        // ─────────────────────────────────────────────────────────
        // 1. 텍스트 라벨 — 최소 구성
        //    씬: TMP_Text + TextFeatureView (SindyComponent 자동 부착)
        // ─────────────────────────────────────────────────────────
        static void Case1_Label(SindyComponent sindy)
        {
            var nameModel = Models.Label("신디");
            sindy.Bind(nameModel);

            // 값 변경 → UI 자동 반영
            nameModel.Feature<TextFeature>().Text.Value = "Citrine";
        }

        // ─────────────────────────────────────────────────────────
        // 2. 클릭 버튼 — uGUI Button 불필요
        //    씬: Image(레이캐스트 타겟) + ButtonFeatureView
        // ─────────────────────────────────────────────────────────
        static void Case2_ClickButton(SindyComponent sindy)
        {
            var attackModel = Models.Button();
            sindy.Bind(attackModel);

            attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Debug.Log("Attack!"));

            // 테스트: 코드로 클릭 시뮬레이션
            attackModel.Feature<ButtonFeature>().OnClick.OnNext(Unit.Default);
        }

        // ─────────────────────────────────────────────────────────
        // 3. 버튼에 홀드 추가 — 에디터 작업 0, 모델 한 줄
        // ─────────────────────────────────────────────────────────
        static void Case3_HoldButton(SindyComponent sindy)
        {
            var attackModel = Models.Button(allowHold: true);
            sindy.Bind(attackModel);

            attackModel.Feature<ButtonFeature>().OnClick.Subscribe(_ => Debug.Log("클릭"));
            attackModel.Feature<ButtonFeature>().OnHold.Subscribe(repeat => Debug.Log($"홀드 연사 {repeat}"));

            // 런타임 토글도 가능
            attackModel.Feature<ButtonFeature>().AllowHold.Value = false;
        }

        // ─────────────────────────────────────────────────────────
        // 4. 조합 폭발 해소 — 스킬 버튼 (작성 클래스 0개)
        //    씬(한 GameObject): ImageFeatureView + TextFeatureView +
        //    ButtonFeatureView + GaugeFeatureView + InteractableFeatureView
        // ─────────────────────────────────────────────────────────
        static void Case4_SkillButton(SindyComponent sindy, Sprite fireballSprite)
        {
            var skill = new ViewModel()
                .With(new ImageFeature(fireballSprite))
                .With(new TextFeature("파이어볼"))
                .With(new ButtonFeature())
                .With(new GaugeFeature(0f))
                .With(new InteractableFeature());

            sindy.Bind(skill);

            skill.Feature<ButtonFeature>().OnClick.Subscribe(_ =>
            {
                Debug.Log("CastFireball");
                skill.Feature<InteractableFeature>().Interactable.Value = false;
                // 쿨다운 진행은 Controller가 GaugeFeature.Ratio만 갱신하면 된다:
                // skill.Feature<GaugeFeature>().Ratio.Value = ratio;
                // 완료 시: skill.Feature<InteractableFeature>().Interactable.Value = true;
            });
        }

        // ─────────────────────────────────────────────────────────
        // 5. UI 트리 구조 — SindyComponent 키 매핑 (Feature 축과 공존)
        //    씬: 팝업 루트에 SindyComponent → views 리스트에 자식 허브+키 등록
        // ─────────────────────────────────────────────────────────
        static void Case5_ShopPopup(SindyComponent shopView)
        {
            var shop = new ViewModel();
            shop["title"] = Models.Label("상점");
            shop["gold"] = Models.Label(new FormatNumberPropModel<long>(12345));   // "12,345" 자동 포맷
            shop["buy"] = new ViewModel()
                .With(new TextFeature("구매"))
                .With(new ButtonFeature())
                .With(new InteractableFeature());

            shopView.Bind(shop);

            shop["buy"].Feature<ButtonFeature>().OnClick.Subscribe(_ => Debug.Log("구매!"));
        }

        // ─────────────────────────────────────────────────────────
        // 6. 확인/취소 팝업 — Models.Notice 팩토리 (구 NoticeComponent 대체)
        // ─────────────────────────────────────────────────────────
        static void Case6_Notice(SindyComponent noticeView)
        {
            var notice = Models.Notice("알림", "정말 삭제할까요?", hasCancel: true);
            noticeView.Bind(notice);

            notice["confirm"].Feature<ButtonFeature>().OnClick.Subscribe(_ => Debug.Log("삭제"));
            notice["cancel"].Feature<ButtonFeature>().OnClick.Subscribe(_ => Debug.Log("취소"));
        }

        // ─────────────────────────────────────────────────────────
        // 7. 카운트다운 라벨 — 자가 갱신 모델 주입 (수동 배선·구독 관리 없음)
        // ─────────────────────────────────────────────────────────
        static void Case7_CountdownLabel(SindyComponent sindy)
        {
            sindy.Bind(new ViewModel().With(new TextFeature(new TimerModel(60f))));
        }

        // ─────────────────────────────────────────────────────────
        // 8. Blueprint와 조합 — 프리팹 조립 + Feature 모델 팩토리
        // ─────────────────────────────────────────────────────────
        static void Case8_Blueprint()
        {
            ComponentBlueprint
                .Create("notice_popup").WithModel(() => Models.Notice("공지", "서버 점검이 예정되어 있습니다.", hasCancel: false))
                .Open(layer: 1);
        }
    }
}
