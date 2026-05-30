using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.VFX;

public class PlayerAttack : MonoBehaviour
{

    private PlayerMovement playerMovement;
    private PlayerShield playerShield;

    [Header("Debug")]
    public bool giveSwordOnStart = false;

    [Header("Settings")]
    public float attackCooldown = 0.5f; // Time between attacks
    private float nextAttackTime = 0f; // When the player can attack again
    private Coroutine attackCoroutine;

    [Header("Sword refs")]
    public bool hasSword = false;
    public GameObject swordObject;
    public GameObject swordHitbox;

    [Header("Slash")]
    public GameObject slashEffectPrefab;
    public Transform slashAnchor;
    public float slashLifetime = 1f;

    private Animator animator;
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    void Start()
    {
        
        playerMovement = GetComponent<PlayerMovement>();
        playerShield = GetComponent<PlayerShield>();
        animator = GetComponentInChildren<Animator>();

        if (giveSwordOnStart || (PlayerSaveData.hasData && PlayerSaveData.hasSword))
        {
            GotSword();
        }
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

        if (playerShield != null && playerShield.IsShielding)
            return;

        if(GameStateManager.Instance.IsPlayerInputEnabled == false)
            return;

        if (playerMovement.isDashing || playerMovement.isReceivingItem)
            return;

        // Cancel any leftover coroutine from a previous attack
        if (attackCoroutine != null)
            StopCoroutine(attackCoroutine);

        attackCoroutine = StartCoroutine(SetPlayerAttackingFalseAfterDelay(attackCooldown));


        // Set the attack state
        playerMovement.isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        // Play the attack animation
        if (animator != null)
            animator.SetTrigger(AttackHash);

    }

    IEnumerator SetPlayerAttackingFalseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        playerMovement.isAttacking = false;
        swordHitbox.SetActive(false);
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
