using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OctopusHealth : MonoBehaviour, IHittable
{
    [Header("Health")]
    [SerializeField] float maxHealth = 40f;

    [Header("Knockback")]
    [SerializeField] float knockbackDuration = 0.25f;

    [Header("Sounds")]
    public AudioSource audioSource;
    [SerializeField] AudioClip hitSound;
    [SerializeField] AudioClip deathSound;

    [Header("Refs")]
    public GameObject deathVFX;
    public GameObject deathVFXPos;

    [Header("ItemDrop")]
    [SerializeField] GameObject[] itemsToDrop;
    [SerializeField] float dropChance = 0.25f;

    float currentHealth;
    bool  isDying;
    bool  isKnockedBack;

    NavMeshAgent  agent;
    OctopusAI     octopusAI;

    SkinnedMeshRenderer[] m_Renderers;
    Color[]               m_OriginalColors;
    Coroutine             m_FlashCoroutine;

    void Awake()
    {
        currentHealth = maxHealth;
        agent         = GetComponent<NavMeshAgent>();
        octopusAI     = GetComponent<OctopusAI>();

        m_Renderers      = GetComponentsInChildren<SkinnedMeshRenderer>();
        m_OriginalColors = new Color[m_Renderers.Length];
        for (int i = 0; i < m_Renderers.Length; i++)
            m_OriginalColors[i] = m_Renderers[i].material.color;
    }

    public void TakeDamage(float amount, Vector3 knockbackDirection, float knockbackForce)
    {
        if (isDying) return;

        currentHealth -= amount;

        FlashRed();

        if(!octopusAI.CanSeePlayer())
        {
            octopusAI.EnterAlert();
        }

        isDying = currentHealth <= 0f;

        if (!isDying)
        {
            octopusAI.HitAnimation();
            if (audioSource != null && hitSound != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(hitSound);
            }
        }

        if (agent != null && !isKnockedBack)
            StartCoroutine(KnockbackCoroutine(knockbackDirection, knockbackForce*1.5f, isDying));
        else if (isDying)
            Die();
    }

    IEnumerator KnockbackCoroutine(Vector3 direction, float force, bool dying)
    {
        isKnockedBack = true;
        agent.ResetPath();

        if (dying && octopusAI != null)
        {
            octopusAI.Die();
            octopusAI.enabled = false;
        }

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float t = 1f - (elapsed / knockbackDuration);
            agent.Move(direction * (force * t) * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;

        if (dying) Die();
    }

    void Die()
    {
        if (audioSource != null && deathSound != null)
            audioSource.PlayOneShot(deathSound);

        if (m_FlashCoroutine != null)
            StopCoroutine(m_FlashCoroutine);
        SetMeshColor(Color.red);

        StartCoroutine(SpawnSmokeEffect());
        StartCoroutine(DropItem());

        Destroy(gameObject, 0.5f);
    }

    IEnumerator SpawnSmokeEffect()
    {

        yield return new WaitForSeconds(0.4f);

        GameObject effect = Instantiate(deathVFX, deathVFXPos.transform.position, Quaternion.identity);
        Destroy(effect, 2f);
    }

    IEnumerator DropItem()
    {
        yield return new WaitForSeconds(0.4f); // Delay to sync with death animation

        if (itemsToDrop.Length > 0 && Random.value < dropChance)
        {
            int index = Random.Range(0, itemsToDrop.Length);
            Vector3 spawnPos = transform.position;
            spawnPos.y = 0.4f; // Adjust Y position if needed
            Instantiate(itemsToDrop[index], spawnPos, Quaternion.identity);
        }
    }

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
        if (!isDying) ResetMeshColor();
    }

    void SetMeshColor(Color color)
    {
        foreach (var r in m_Renderers) r.material.color = color;
    }

    void ResetMeshColor()
    {
        for (int i = 0; i < m_Renderers.Length; i++)
            m_Renderers[i].material.color = m_OriginalColors[i];
    }
}
