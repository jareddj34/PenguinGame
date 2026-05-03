using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShield : MonoBehaviour
{
    [Header("Requirements")]
    [Tooltip("The player must have picked up a shield before they can use it.")]
    public bool hasShield = false;
    public GameObject shieldObject;
    public GameObject shieldBlockEffectPrefab;
    public Transform shieldBlockEffectSpawnPoint;

    [Header("Settings")]
    [Tooltip("Speed multiplier applied while the shield is raised (e.g. 0.6 = 60% of normal speed).")]
    [SerializeField] private float shieldMoveSpeedMultiplier = 0.6f;

    [Tooltip("If true the player must hold the button to keep the shield up; if false it toggles on each press.")]
    [SerializeField] private bool holdToShield = true;

    // Public read-only state
    public bool IsShielding { get; private set; }

    /// <summary>Returns the speed multiplier to apply during movement (1 when shield is down).</summary>
    public float SpeedMultiplier => IsShielding ? shieldMoveSpeedMultiplier : 1f;

    // Components
    private PlayerMovement playerMovement;
    private Animator animator;

    private static readonly int ShieldUpHash = Animator.StringToHash("ShieldUp");

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    // -------------------------------------------------------------------------
    // Input Messages (called automatically by PlayerInput "Send Messages")
    // -------------------------------------------------------------------------

    private void OnShield(InputValue value)
    {
        if (!hasShield)
            return;

        if (holdToShield)
        {
            // Hold: raise on press, lower on release
            if (value.isPressed) {
                TryRaiseShield();
                Debug.Log("Shield raised");
            }
            else {
                LowerShield();
                Debug.Log("Shield lowered");
            }
        }
        else
        {
            // Toggle: flip state on each press
            if (value.isPressed)
            {
                if (IsShielding)
                    LowerShield();
                else
                    TryRaiseShield();
            }
        }
    }

    // -------------------------------------------------------------------------
    // Shield State
    // -------------------------------------------------------------------------

    private void TryRaiseShield()
    {
        // Can't raise shield while dashing, mid-attack, or during cutscenes
        if (playerMovement.isDashing || playerMovement.isAttacking || playerMovement.isReceivingItem)
            return;

        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlayerInputEnabled)
            return;

        SetShield(true);
    }

    private void LowerShield()
    {
        // Guard prevents the trigger from being spammed when shield is already down
        if (!IsShielding) return;
        SetShield(false);
    }

    /// <summary>Drops the shield immediately — call this from a dash, knockback, etc.</summary>
    public void ForceDropShield()
    {
        if (IsShielding)
            SetShield(false);
    }

    private void SetShield(bool up)
    {
        IsShielding = up;
        if (animator != null)
            animator.SetBool(ShieldUpHash, up);
    }

    // -------------------------------------------------------------------------
    // Hit Blocking
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this from PlayerHealth.TakeHit with the resolved hit direction.
    /// Returns true if the shield successfully blocked the hit (caller should skip damage + knockback).
    /// The shield only blocks attacks that come from in front of the player.
    /// </summary>
    public bool TryBlock(HitDirection hitDir)
    {
        Debug.Log("Trying to block hit from direction: " + hitDir);
        if (!IsShielding)
            return false;

        if (hitDir == HitDirection.Front)
        {
            // Spawn block effect at player's position
            if (shieldBlockEffectPrefab != null)
            {
                GameObject blockEffect = Instantiate(shieldBlockEffectPrefab, shieldBlockEffectSpawnPoint.position, Quaternion.identity);
                Destroy(blockEffect, 1f); // Clean up the effect after 1 second
            }
        }

        // Shield faces forward — it can only block hits coming from the front
        return hitDir == HitDirection.Front;
    }

    // -------------------------------------------------------------------------
    // Items
    // -------------------------------------------------------------------------

    /// <summary>Called when the player picks up a shield item.</summary>
    public void GotShield()
    {
        hasShield = true;
        if (shieldObject != null)
            shieldObject.SetActive(true);
    }
}
