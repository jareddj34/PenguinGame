using UnityEngine;

public class SnowballPickup : Collectable
{
    public int snowballs = 3;

    protected override void OnCollect(GameObject player)
    {
        player.GetComponent<PlayerThrow>()?.AddAmmo(snowballs);
    }
}
