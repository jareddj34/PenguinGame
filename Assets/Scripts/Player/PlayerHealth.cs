using UnityEngine;

public class PlayerHealth : MonoBehaviour
{

    public int maxHealth = 5;
    public int currentHealth { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;

    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
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
}
