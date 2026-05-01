using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : WorldItem
{
    public UnityEvent OnItemPickupEvent;

    protected override void OnPickup()
    {   
        GameEvents.Instance.ItemPickedUp(itemData);

        OnItemPickupEvent?.Invoke();
    }

}
