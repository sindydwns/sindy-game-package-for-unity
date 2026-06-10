namespace Sindy.View.Features
{
    public class InteractableFeature : ModelFeature
    {
        public PropModel<bool> Interactable { get; }

        public InteractableFeature(bool initialValue = true)
        {
            Interactable = new PropModel<bool>(initialValue);
            Interactable.AddTo(this);
        }
    
        /// <summary>외부 모델 주입. Feature와 함께 Dispose된다.</summary>
        public InteractableFeature(PropModel<bool> external)
        {
            Interactable = external ?? throw new System.ArgumentNullException(nameof(external));
            Interactable.AddTo(this);
        }
}
}
