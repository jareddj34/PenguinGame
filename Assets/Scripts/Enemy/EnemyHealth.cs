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
    bool isDying;
    NavMeshAgent agent;
    EnemyAI enemyAI;
    bool isKnockedBack;

    private SkinnedMeshRenderer[] m_Renderers;
    private Color[] m_OriginalColors;
    private Coroutine m_FlashCoroutine;

    void Awake()
    {
        currentHealth = maxHealth;
        agent = GetComponent<NavMeshAgent>();
        enemyAI = GetComponent<EnemyAI>();

        m_Renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        m_OriginalColors = new Color[m_Renderers.Length];
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_OriginalColors[i] = m_Renderers[i].material.color;
        }
    }

    /// <summary>
    /// Deal damage and apply knockback in the given direction.
    /// knockbackDirection should be a normalized world-space vector (attacker -> enemy).
    /// </summary>
    public void TakeDamage(float amount, Vector3 knockbackDirection)
    {
        if (isDying) return;

        currentHealth -= amount;
        Debug.Log($"[EnemyHealth] {gameObject.name} took {amount} damage. HP: {currentHealth}/{maxHealth}");

        // Hook for hit animations/sounds here
        // e.g. animator.SetTrigger("Hit");

        FlashRed();

        isDying = currentHealth <= 0f;

        if (agent != null && !isKnockedBack)
            StartCoroutine(KnockbackCoroutine(knockbackDirection, isDying));
        else if (isDying) {
            Die();
        }

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
        if (m_FlashCoroutine != null)
            StopCoroutine(m_FlashCoroutine); // cancel the flash so it stays red
        SetMeshColor(Color.red);             // lock it red on death

        // Hook for death effects (particles, sounds) here
        // e.g. Instantiate(deathVFX, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.5f);
    }


    // Flash red  stuff
    public void FlashRed(float duration = 0.5f)
    {
        if (m_FlashCoroutine != null)
            StopCoroutine(m_FlashCoroutine);
        m_FlashCoroutine = StartCoroutine(FlashRedCoroutine(duration));
    }

    IEnumerator FlashRedCoroutine(float duration)
    {
        SetMeshColor(Color.red);
        yield return new WaitForSeconds(duration);
        if (!isDying)
            ResetMeshColor();
    }

    private void SetMeshColor(Color color)
    {
        foreach (var r in m_Renderers)
            r.material.color = color;
    }

    private void ResetMeshColor()
    {
        for (int i = 0; i < m_Renderers.Length; i++)
            m_Renderers[i].material.color = m_OriginalColors[i];
    }
}
