using UnityEngine;

public class PlayerHealth : MonoBehaviour
{

    private PlayerMovement playerMovement;
    private PlayerShield playerShield;

    public int maxHealth = 100;
    public int currentHealth { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;
        playerMovement = GetComponent<PlayerMovement>();
        playerShield = GetComponent<PlayerShield>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        Debug.Log("Player took " + damage + " damage. Current health: " + currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log("Player healed. Current health: " + currentHealth);
    }

    public void Die()
    {
        // Handle player death (e.g., respawn, game over screen)
        Debug.Log("Player has died!");
    }

    // Knockback
    public void TakeHit(Vector3 attackerPosition, int damage, float force)
    {
        HitDirection dir = GetHitDirection(attackerPosition);

        if (playerShield != null && playerShield.TryBlock(dir))
        {
            Debug.Log("Hit blocked by shield!");
            return; // no damage, no knockback
        }

        TakeDamage(damage);
        playerMovement?.StartKnockback(dir, force);
    }

    private HitDirection GetHitDirection(Vector3 attackerPosition)
    {
        Vector3 dir = (transform.position - attackerPosition).normalized;
        float dotForward = Vector3.Dot(transform.forward, dir);
        float dotRight   = Vector3.Dot(transform.right, dir);

        if (Mathf.Abs(dotForward) >= Mathf.Abs(dotRight))
            return dotForward >= 0 ? HitDirection.Back : HitDirection.Front;
        else
            return dotRight >= 0 ? HitDirection.Left : HitDirection.Right;
    }
}
