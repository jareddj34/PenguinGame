using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyHealth : MonoBehaviour, IHittable
{
    [Header("Health")]
    [SerializeField] float maxHealth = 50f;

    [Header("Knockback")]
    [SerializeField] float knockbackForce = 6f;
    [SerializeField] float knockbackDuration = 0.25f;

    float currentHealth;
    NavMeshAgent agent;
    EnemyAI enemyAI;
    bool isKnockedBack;

    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        enemyAI = GetComponent<EnemyAI>();
    }

    /// <summary>
    /// Deal damage and apply knockback in the given direction.
    /// knockbackDirection should be a normalized world-space vector (attacker -> enemy).
    /// </summary>
    public void TakeDamage(float amount, Vector3 knockbackDirection)
    {
        currentHealth -= amount;
        Debug.Log($"[EnemyHealth] {gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        // Hook for hit animations/sounds here
        // e.g. animator.SetTrigger("Hit");

        bool isDying = currentHealth <= 0f;

        if (agent != null && !isKnockedBack)
            StartCoroutine(KnockbackCoroutine(knockbackDirection, isDying));
        else if (isDying)
            Die();
    }

    IEnumerator KnockbackCoroutine(Vector3 direction, bool isDying)
    {
        isKnockedBack = true;
        agent.ResetPath();

        // If dying, shut down the AI so it doesn't resume pathfinding mid-knockback
        if (isDying && enemyAI != null)
            enemyAI.enabled = false;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            // Velocity decays from full force to zero over the duration
            float t = 1f - (elapsed / knockbackDuration);
            Vector3 delta = direction * (knockbackForce * t);
            agent.Move(delta * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;

        if (isDying)
            Die();
        // Otherwise EnemyAI will resume pathfinding naturally on its next state tick
    }

    void Die()
    {
        Debug.Log($"[EnemyHealth] {gameObject.name} has been defeated!");

        // Hook for death effects (particles, sounds) here
        // e.g. Instantiate(deathVFX, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.5f);
    }
}
