using UnityEngine;

/// <summary>
/// Attach to a child GameObject with a Trigger Collider.
/// The GameObject should be inactive by default — it is switched on
/// for the active window of each attack swing, then switched off again.
///
/// Two ways to drive it:
///   1. Animation events  — call EnableHitbox() / DisableHitbox() directly.
///   2. EnemyAI fallback  — EnemyAI enables it in PerformAttack() and a
///                          coroutine auto-disables it after hitboxActiveTime.
/// </summary>
public class EnemyHitbox : MonoBehaviour
{
    [Tooltip("Damage dealt to the player per swing. " +
             "Can also be driven from EnemyAI if you prefer one source of truth.")]
    [SerializeField] int damage = 10;

    public float knockBackForce = 10f;  // How strongly the player is knocked back when hit

    // Prevents hitting the player more than once per swing activation
    bool hitRegistered;

    // Cached reference to the parent EnemyAI for stagger callbacks
    EnemyAI enemyAI;

    void Awake()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void OnEnable()
    {
        // Reset each time the hitbox is switched on so every swing can land
        hitRegistered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hitRegistered) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            bool blocked = playerHealth.TakeHit(transform.parent.transform.position, damage, knockBackForce);
            hitRegistered = true; // Only one hit per swing

            if (blocked)
                enemyAI?.EnterStagger();
        }
    }

    // ── Public API — callable from EnemyAI or Animation Events ───────────

    /// <summary>Activate the hitbox for this swing.</summary>
    public void EnableHitbox()
    {
        gameObject.SetActive(true);
    }

    /// <summary>Deactivate the hitbox at the end of the swing.</summary>
    public void DisableHitbox()
    {
        gameObject.SetActive(false);
    }
}
