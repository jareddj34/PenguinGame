using UnityEngine;

public abstract class WorldItem : InteractableBase
{
    [SerializeField] protected ItemData itemData;
    public override string InteractionPrompt => "Pick Up [E]";

    public override void Interact()
    {
        OnPickup();
        GameEvents.Instance.ItemPickedUp(itemData);

        gameObject.SetActive(false); // or Destroy(gameObject) if you don't need pooling

        base.OnStopHover(); // Ensure prompt is hidden after pickup
    }

    protected abstract void OnPickup();

}
