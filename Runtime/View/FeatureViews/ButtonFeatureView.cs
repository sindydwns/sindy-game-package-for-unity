using System;
using System.Collections.Generic;
using R3;
using Sindy.View.Features;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Sindy.View.FeatureViews
{
    /// <summary>
    /// 클릭 + 홀드 입력을 <see cref="ButtonFeature"/>로 전달한다.
    /// uGUI Button 없이 포인터 이벤트를 직접 구현하므로(포인터 이벤트 단일 소유)
    /// 클릭/홀드 판별이 충돌하지 않는다. 같은 오브젝트에 레이캐스트 타겟(Image 등)이 필요하다.
    /// 홀드가 발생한 프레스의 릴리스에서는 클릭이 발행되지 않는다.
    /// </summary>
    [AddComponentMenu("Sindy/Feature Views/Button Feature View")]
    public class ButtonFeatureView : FeatureView<ButtonFeature>,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Tooltip("홀드로 판정되기까지의 시간(초). 홀드 사용 시에만 의미가 있다.")]
        [SerializeField] private float holdTime = 0.5f;
        [Tooltip("홀드 반복 발행 주기(초). 0 이하이면 홀드 진입 시 1회만 발행한다.")]
        [SerializeField] private float repeatInterval = 0.05f;

        private ButtonFeature feature;
        private bool allowHold;

        private bool touchDown;
        private int activePointerId = -1;
        private float holdingTime;
        private int repeatTimes;
        private bool holdConsumedPress;

        public bool IsHolding { get; private set; }
        public int RepeatTimes => repeatTimes;

        protected override void Bind(ButtonFeature feature, ICollection<IDisposable> disposables)
        {
            this.feature = feature;

            feature.AllowHold.Subscribe(v =>
            {
                allowHold = v;
                if (!v) CancelHold();
            }).AddTo(disposables);

            feature.KeepHold.Subscribe(v =>
            {
                if (!v && IsHolding) CancelHold();
            }).AddTo(disposables);

            // 모델 교체/해제 시 진행 중인 프레스 상태를 정리
            disposables.Add(Disposable.Create(() =>
            {
                this.feature = null;
                CancelHold();
            }));
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (feature == null || touchDown) return;
            touchDown = true;
            activePointerId = eventData.pointerId;
            holdingTime = 0f;
            repeatTimes = 0;
            holdConsumedPress = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            holdConsumedPress = repeatTimes > 0;
            CancelHold();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (feature == null) return;
            if (holdConsumedPress)
            {
                holdConsumedPress = false;
                return;
            }
            feature.OnClick.OnNext(Unit.Default);
        }

        private void Update()
        {
            if (!touchDown || feature == null || !allowHold) return;

            var old = holdingTime;
            holdingTime += Time.deltaTime;
            if (holdingTime < holdTime) return;

            if (repeatInterval <= 0f)
            {
                if (repeatTimes == 0) EmitHold();
                return;
            }

            var oldCounter = (int)(old / repeatInterval);
            var newCounter = (int)(holdingTime / repeatInterval);
            if (oldCounter == newCounter) return;

            EmitHold();
        }

        private void EmitHold()
        {
            IsHolding = true;
            repeatTimes++;
            feature.OnHold.OnNext(repeatTimes);
        }

        private void CancelHold()
        {
            touchDown = false;
            holdingTime = 0f;
            repeatTimes = 0;
            activePointerId = -1;
            IsHolding = false;
        }

        private void OnDisable()
        {
            holdConsumedPress = false;
            CancelHold();
        }
    }
}
