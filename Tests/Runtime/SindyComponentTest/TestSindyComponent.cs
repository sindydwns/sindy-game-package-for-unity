using System.Collections.Generic;
using Sindy.View;
using UnityEngine;

namespace Sindy.Test
{
    public class TestSindyComponent : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private SindyComponent labelComponent;
        [SerializeField] private SindyComponent formatLabelComponent;
        [SerializeField] private SindyComponent colorComponent;
        [SerializeField] private SindyComponent gaugeComponent;
        [SerializeField] private SindyComponent iconComponent;
        [SerializeField] private Sprite testIcon;

        [Header("Control")]
        [SerializeField] private SindyComponent buttonComponent;
        [SerializeField] private SindyComponent toggleComponent;
        [SerializeField] private SindyComponent tabComponent;
        [SerializeField] private SindyComponent pageComponent;
        [SerializeField] private SindyComponent visibilityComponent;

        [Header("Composite")]
        [SerializeField] private SindyComponent listComponent;
        [SerializeField] private SindyComponent popupComponent;
        [SerializeField] private SindyComponent timerComponent;

        private readonly List<TestCase> tests = new();

        void Start()
        {
            TryAdd(buttonComponent, c => new TestButtonComponentWork(c));
            TryAdd(labelComponent, c => new TestLabelComponentWork(c));
            TryAdd(formatLabelComponent, c => new TestFormatLabelComponentWork(c));
            TryAdd(colorComponent, c => new TestColorComponentWork(c));
            TryAdd(gaugeComponent, c => new TestGaugeComponentWork(c));
            TryAdd(iconComponent, c => new TestIconComponentWork(c, testIcon));
            TryAdd(toggleComponent, c => new TestToggleComponentWork(c));
            TryAdd(tabComponent, c => new TestTabComponentWork(c));
            TryAdd(pageComponent, c => new TestPageComponentWork(c));
            TryAdd(visibilityComponent, c => new TestVisibilityComponentWork(c));
            TryAdd(listComponent, c => new TestListComponentWork(c));
            TryAdd(popupComponent, c => new TestPopupComponentWork(c));
            TryAdd(timerComponent, c => new TestTimerComponentWork(c));

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
        }

        private void TryAdd(SindyComponent component, System.Func<SindyComponent, TestCase> factory)
        {
            if (component != null)
            {
                tests.Add(factory(component));
            }
        }
    }
}
