namespace Sindy.View.Features
{
    public class InteractableFeature : ViewModelFeature
    {
        public PropModel<bool> Interactable { get; }

        public InteractableFeature(bool initialValue = true)
        {
            Interactable = new PropModel<bool>(initialValue);
            Interactable.AddTo(this);
        }
    }
}
