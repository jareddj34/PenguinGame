using UnityEngine;
using System.Collections;
using System;

public class PlayerHealth : MonoBehaviour
{

    private PlayerMovement playerMovement;
    private PlayerSound playerSound;
    private PlayerShield playerShield;

    [Header("Heart Containers")]
    [Tooltip("How many heart slots the player starts with.")]
    public int maxHeartContainers = 3;

    /// <summary>How much HP one full heart represents. Half a heart = HealthPerContainer * 0.5.</summary>
    public const float HealthPerContainer = 2f;

    public float maxHealth { get; private set; }
    public float currentHealth { get; private set; }
    public event Action<float, float> OnHealthChanged; // (currentHealth, maxHealth)

    private bool isInvulnerable = false;

    private Renderer[] m_Renderers;
    private Color[] m_OriginalColors;
    private Coroutine m_FlashCoroutine;

    void Start()
    {
        maxHealth = maxHeartContainers * HealthPerContainer;
        currentHealth = maxHealth;
        playerMovement = GetComponent<PlayerMovement>();
        playerSound = GetComponent<PlayerSound>();
        playerShield = GetComponent<PlayerShield>();

        m_Renderers = GetComponentsInChildren<Renderer>(true);
        m_OriginalColors = new Color[m_Renderers.Length];
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_OriginalColors[i] = m_Renderers[i].material.color;
        }
    }

    /// <summary>
    /// Adds heart containers (Zelda-style upgrade). Also fills the new hearts.
    /// Call this when the player collects a Heart Container item.
    /// </summary>
    public void AddHeartContainer(int count = 1)
    {
        maxHeartContainers += count;
        maxHealth = maxHeartContainers * HealthPerContainer;
        currentHealth = Mathf.Min(currentHealth + count * HealthPerContainer, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float damage)
    {
        if (isInvulnerable) return;

        currentHealth = Mathf.Max(currentHealth - damage, 0f);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        playerSound.PlayHurt();
        playerSound.PlayHit();

        if (m_FlashCoroutine != null) StopCoroutine(m_FlashCoroutine);
        StartCoroutine(InvulnerabilityCoroutine(0.75f));
        m_FlashCoroutine = StartCoroutine(FlashCoroutine(0.75f));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void Die()
    {
        // Handle player death (e.g., respawn, game over screen)
        Debug.Log("Player has died!");
    }

    IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(0.1f);
        playerMovement.isInvulnerable = true;
        yield return new WaitForSeconds(duration - 0.1f);
        isInvulnerable = false;
        playerMovement.isInvulnerable = false;
    }

    // Knockback
    /// <summary>
    /// Returns true if the hit was successfully blocked by the shield,
    /// false if damage and knockback were applied normally.
    /// </summary>
    public bool TakeHit(Vector3 attackerPosition, float damage, float force)
    {
        
        HitDirection dir = GetHitDirection(attackerPosition);

        if (playerShield != null && playerShield.TryBlock(dir))
        {
            Debug.Log("Hit blocked by shield!");
            return true; // blocked — caller can react (e.g. enemy stagger)
        }

        if(isInvulnerable) return true;
        TakeDamage(damage);
        playerMovement?.StartKnockback(dir, force);
        return false; // not blocked
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

    IEnumerator FlashCoroutine(float duration)
    {
        float flashInterval = 0.1f;
        float elapsed = 0f;
        bool showFlash = true;

        while (elapsed < duration)
        {
            if (showFlash)
            {
                foreach (var r in m_Renderers)
                {
                    r.enabled = true;
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", Color.red);
                }
            }
            else
            {
                foreach (var r in m_Renderers)
                    r.enabled = false;
            }

            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
            showFlash = !showFlash;
        }

        // Restore original appearance
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            m_Renderers[i].enabled = true;
            m_Renderers[i].material.SetColor("_EmissionColor", Color.black);
            m_Renderers[i].material.DisableKeyword("_EMISSION");
            m_Renderers[i].material.color = m_OriginalColors[i];
        }
    }

}
