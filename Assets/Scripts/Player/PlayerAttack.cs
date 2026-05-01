using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PlayerAttack : MonoBehaviour
{

    private PlayerMovement playerMovement;

    [Header("Settings")]
    public float attackCooldown = 0.5f; // Time between attacks
    private float nextAttackTime = 0f; // When the player can attack again

    [Header("Sword refs")]
    public bool hasSword = false;
    public GameObject swordObject;

    [Header("Slash")]
    public GameObject slashEffectPrefab;
    public Transform slashAnchor;
    public float slashLifetime = 1f;

    private Animator animator;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnAttack(InputValue value)
    {
        if (!value.isPressed)
            return;

        // Check if cooldown is still active
        if (Time.time < nextAttackTime)
            return;
        
        if(!hasSword)
        {
            return;
        }

        if (playerMovement.isDashing || playerMovement.isReceivingItem)
            return;

        // Set the attack state
        playerMovement.isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        // Play the attack animation
        if (animator != null)
            animator.SetTrigger(AttackHash);

    }

    public void SpawnSlash()
    {
        if (slashEffectPrefab == null)
            return;

        // Use the anchor if assigned, otherwise fall back to the player's own transform
        Transform origin = slashAnchor != null ? slashAnchor : transform;

        Debug.Log("Spawning slash effect at " + origin.position);
        // Stamp into world space — NOT parented, so it stays put while the VFX plays
        GameObject slash = Instantiate(slashEffectPrefab, origin.position, origin.rotation);

        // Clean up after the effect is done
        Destroy(slash, slashLifetime);
    }

    public void GotSword()
    {
        hasSword = true;
        if (swordObject != null)
            swordObject.SetActive(true);
    }
}
