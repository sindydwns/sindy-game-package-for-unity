using System.Collections.Generic;
using Sindy.View;
using Sindy.View.Components;
using UnityEngine;

namespace Sindy.Test
{
    public class TestViewComponent : MonoBehaviour
    {
        [SerializeField] private ViewComponent viewComponent;

        private readonly List<TestCase> tests = new();
        private ViewModel viewModel;

        void Start()
        {
            viewModel = new ViewModel()
                .AddChild("button", new ButtonModel());

            viewComponent.SetModel(viewModel);

            tests.ForEach(test => test.Run());
        }

        void OnDestroy()
        {
            tests.ForEach(test => test.Dispose());
            viewModel?.Dispose();
        }

        private void TryAdd(SindyComponent component, System.Func<SindyComponent, TestCase> factory)
        {
            if (component != null)
                tests.Add(factory(component));
        }
    }
}
