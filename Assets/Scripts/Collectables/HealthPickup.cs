using UnityEngine;

public class HealthPickup : Collectable
{

    public int healAmount = 1;

    protected override void OnCollect(GameObject player)
    {
        player.GetComponent<PlayerHealth>()?.Heal(healAmount);
    }
}
