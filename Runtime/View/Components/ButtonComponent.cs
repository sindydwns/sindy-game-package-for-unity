using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sindy.Common;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace Sindy.View.Components
{
    [RequireComponent(typeof(HoldButton))]
    public class ButtonComponent : SindyComponent<SubjModel<Unit>>
    {
        private HoldButton button;

        void Awake()
        {
            button = GetComponent<HoldButton>();
        }

        protected override void Init(SubjModel<Unit> model)
        {
            void OnClick() => model.OnNext(Unit.Default);
            button.onClick.AddListener(OnClick);
            disposables.Add(Disposable.Create(() => button.onClick.RemoveListener(OnClick)));
        }
    }

    public class ButtonModel : SubjModel<Unit>
    {
    }

    public class HoldButton : Button, ICancelable
    {
        public float holdTime = 0.5f;
        public float holdingRepeatTime = 0.05f;
        public bool allowHold = true;

        private float holdingTime = 0f;
        public float HoldingTime => holdingTime;
        private bool touchDown = false;
        private int activePointerId = -1;
        private int repeatTimes = 0;
        public int RepeatTimes => repeatTimes;
        public bool IsHolding { get; private set; }
        public bool AllowHold
        {
            get => allowHold;
            set
            {
                Cancel();
                allowHold = value;
            }
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (touchDown) return;
            holdingTime = 0f;
            touchDown = true;
            activePointerId = eventData.pointerId;
            repeatTimes = 0;
            enabled = true;
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.pointerId != activePointerId) return;
            Cancel();
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (repeatTimes != 0)
            {
                return;
            }
            base.OnPointerClick(eventData);
        }

        void Update()
        {
            if (!touchDown)
            {
                enabled = false;
                return;
            }
            if (!AllowHold)
            {
                Cancel();
                return;
            }
            var old = holdingTime;
            holdingTime += Time.deltaTime;
            if (holdingTime < holdTime)
            {
                return;
            }
            if (holdingRepeatTime <= 0f)
            {
                if (repeatTimes == 0)
                {
                    IsHolding = true;
                    repeatTimes++;
                    onClick?.Invoke();
                }
                return;
            }
            var oldCounter = (int)(old / holdingRepeatTime);
            var newCounter = (int)(holdingTime / holdingRepeatTime);
            if (oldCounter == newCounter)
            {
                return;
            }
            IsHolding = true;
            repeatTimes++;
            onClick?.Invoke();
        }

        public void Cancel()
        {
            touchDown = false;
            holdingTime = 0f;
            repeatTimes = 0;
            activePointerId = -1;
            IsHolding = false;
            enabled = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            touchDown = false;
            holdingTime = 0f;
            repeatTimes = 0;
            activePointerId = -1;
            IsHolding = false;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HoldButton))]
    public class HoldingButtonEditor : ButtonEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            HoldButton holdingButton = (HoldButton)target;
            holdingButton.allowHold = EditorGUILayout.Toggle("Allow Hold", holdingButton.allowHold);
            holdingButton.holdTime = EditorGUILayout.FloatField("Hold Time", holdingButton.holdTime);
            holdingButton.holdingRepeatTime = EditorGUILayout.FloatField("Holding Repeat Time", holdingButton.holdingRepeatTime);
        }
    }
#endif
}
