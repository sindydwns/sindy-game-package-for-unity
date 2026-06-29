using UnityEngine;

namespace Sindy.View
{
    /// <summary>
    /// 프리팹(또는 씬 인스턴스) · 모델 · 레이어를 묶은 생성 명세.
    /// <see cref="Build"/>가 인스턴스화와 모델 바인딩을 수행한다.
    /// </summary>
    public class ComponentPreset
    {
        /// <summary>생성에 사용할 프리팹 또는 씬 인스턴스.</summary>
        public SindyComponent Component { get; private set; }

        /// <summary>생성 직후 바인딩할 모델.</summary>
        public IViewModel Model { get; set; }

        /// <summary>배치 레이어 인덱스.</summary>
        public int Layer { get; set; }

        public ComponentPreset(SindyComponent prefab, IViewModel model = null, int layer = 0)
        {
            Component = prefab;
            Model = model;
            Layer = layer;
        }

        /// <summary>
        /// 명세대로 컴포넌트를 만든다. 프리팹이면 인스턴스화하고(부모 지정 가능),
        /// 씬 인스턴스면 그대로 사용한 뒤 <see cref="Model"/>을 바인딩해 반환한다.
        /// </summary>
        public SindyComponent Build(Transform parent = null)
        {
            if (Component == null)
            {
                throw new System.ArgumentNullException(nameof(Component), "Prefab cannot be null.");
            }

            SindyComponent com;
            if (!Component.IsPrefab)
            {
                com = Component;                              // 씬 인스턴스: 그대로 사용
            }
            else if (parent == null)
            {
                com = Object.Instantiate(Component);          // 프리팹: 부모 없이 인스턴스화
            }
            else
            {
                com = Object.Instantiate(Component, parent);  // 프리팹: 지정한 부모 아래 인스턴스화
            }

            com.Preset = this;
            com.Bind(Model);

            return com;
        }
    }

    public class ComponentPreset<T> : ComponentPreset where T : class, IViewModel
    {
        public ComponentPreset(SindyComponent prefab, T model = null) : base(prefab, model) { }

        public new T Model
        {
            get => base.Model as T;
            set => base.Model = value;
        }
    }

    public class ComponentPreset<T1, T2> : ComponentPreset where T1 : SindyComponent where T2 : class, IViewModel
    {
        public ComponentPreset(T1 prefab, T2 model = null) : base(prefab, model) { }
    }
}
