using UnityEngine;

namespace Sindy.View
{
    public class ComponentPreset
    {
        public SindyComponent Component { get; private set; }
        public object Model { get; set; }
        public int Layer { get; set; }

        public ComponentPreset(SindyComponent prefab, object model = null, int layer = 0)
        {
            Component = prefab;
            Model = model;
            Layer = layer;
        }

        public SindyComponent Build(Transform parent = null)
        {
            if (Component == null)
            {
                throw new System.ArgumentNullException(nameof(Component), "Prefab cannot be null.");
            }

            var com = Component.IsPrefab == false ? Component :
                parent == null ?
                Object.Instantiate(Component) :
                Object.Instantiate(Component, parent);
            com.Preset = this;
            com.SetModel(Model);

            return com;
        }
    }

    public class ComponentPreset<T> : ComponentPreset where T : class
    {
        public ComponentPreset(SindyComponent prefab, T model = null) : base(prefab, model) { }

        public new T Model
        {
            get => base.Model as T;
            set => base.Model = value;
        }
    }

    public class ComponentPreset<T1, T2> : ComponentPreset where T1 : SindyComponent where T2 : class
    {
        public ComponentPreset(T1 prefab, T2 model = null) : base(prefab, model) { }
    }
}
