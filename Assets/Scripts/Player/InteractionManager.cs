using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionManager : MonoBehaviour
{

    [SerializeField] private InteractionZone zone;

    private void OnInteract(InputValue value)
    {
        if (!value.isPressed)
            return;

        zone.CurrentInteractable?.Interact();
        zone.ForceRefresh();
    }

}

public interface IInteractable
{
    void Interact();
    void OnHover();
    void OnStopHover();
    bool CanInteract();
    string InteractionPrompt { get; }
}

public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    public abstract void Interact();
    public virtual bool CanInteract() => true;
    public abstract string InteractionPrompt { get; }
    
    public virtual void OnHover()
    {
        GameEvents.Instance.ShowInteractionPrompt(InteractionPrompt);
    }

    public virtual void OnStopHover()
    {
        GameEvents.Instance.HideInteractionPrompt();
    }
}