using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Sindy.Common;
using Sindy.View.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

namespace Sindy.View.Components
{
    [RequireComponent(typeof(HoldButton))]
    public class ButtonComponent : SindyComponent<SubjModel<Unit>>
    {
        [SerializeField] private GameObject highlightTarget;

        private HoldButton button;
        private CanvasGroup canvasGroup;

        void Awake()
        {
            button = GetComponent<HoldButton>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        protected override void Init(SubjModel<Unit> model)
        {
            var buttonModel = model as ButtonModel;

            var hold = buttonModel?.Feature<HoldFeature>();
            if (hold != null)
            {
                void OnClickOrHold()
                {
                    if (button.IsHolding)
                        hold.OnHold.OnNext(button.RepeatTimes);
                    else
                        model.OnNext(Unit.Default);
                }
                button.onClick.AddListener(OnClickOrHold);
                disposables.Add(Disposable.Create(() => button.onClick.RemoveListener(OnClickOrHold)));

                hold.AllowHold.Subscribe(v => button.AllowHold = v).AddTo(disposables);
                hold.KeepHold.Subscribe(v => { if (!v && button.IsHolding) button.Cancel(); }).AddTo(disposables);
            }
            else
            {
                void OnClick() => model.OnNext(Unit.Default);
                button.onClick.AddListener(OnClick);
                disposables.Add(Disposable.Create(() => button.onClick.RemoveListener(OnClick)));
            }

            var interactable = buttonModel?.Feature<InteractableFeature>();
            if (interactable != null)
            {
                interactable.Interactable.Subscribe(v => button.interactable = v).AddTo(disposables);
            }

            var highlight = buttonModel?.Feature<HighlightFeature>();
            if (highlight != null && highlightTarget != null)
            {
                highlight.Highlight.Subscribe(v => highlightTarget.SetActive(v)).AddTo(disposables);
            }

            var raycastBlock = buttonModel?.Feature<RaycastBlockFeature>();
            if (raycastBlock != null && canvasGroup != null)
            {
                raycastBlock.IgnoreRaycast.Subscribe(v => canvasGroup.blocksRaycasts = !v).AddTo(disposables);
            }
        }
    }

    public class ButtonModel : SubjModel<Unit>
    {
        public new ButtonModel With<T>(T feature) where T : ViewModelFeature
        {
            base.With(feature);
            return this;
        }
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
