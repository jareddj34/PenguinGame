using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerThrow : MonoBehaviour
{
    private PlayerMovement playerMovement;

    [Header("Ammo")]
    public int snowballCount = 0;
    public int maxSnowballs = 10;

    [Header("Settings")]
    public float throwCooldown = 0.6f;
    private float nextThrowTime = 0f;

    [Header("Refs")]
    public GameObject snowballPrefab;
    public Transform throwOrigin; // assign an empty child transform at the penguin's hand

    public event Action<int> OnAmmoChanged; // (currentAmmo)

    private Animator animator;
    private static readonly int ThrowHash = Animator.StringToHash("Throw");

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnThrow(InputValue value)
    {
        if (!value.isPressed) return;
        if (Time.time < nextThrowTime) return;
        if (snowballCount <= 0) return;
        if (playerMovement.isDashing || playerMovement.isAttacking || playerMovement.isReceivingItem) return;
        if (!GameStateManager.Instance.IsPlayerInputEnabled) return;

        snowballCount--;
        OnAmmoChanged?.Invoke(snowballCount);
        nextThrowTime = Time.time + throwCooldown;

        playerMovement.isAttacking = true; // freeze movement during throw
        StartCoroutine(ResetThrowState(throwCooldown));

        if (animator != null)
            animator.SetTrigger(ThrowHash);
    }

    // Called by ThrowAnimationEvents at the release frame
    public void SpawnSnowball()
    {
        if (snowballPrefab == null) return;
        Transform origin = throwOrigin != null ? throwOrigin : transform;
        GameObject snowball = Instantiate(snowballPrefab, origin.position, transform.rotation);
        Snowball sb = snowball.GetComponent<Snowball>();
        if (sb != null) sb.owner = gameObject;
    }

    public void AddAmmo(int amount)
    {
        snowballCount = Mathf.Min(snowballCount + amount, maxSnowballs);
        OnAmmoChanged?.Invoke(snowballCount);
    }

    private IEnumerator ResetThrowState(float delay)
    {
        yield return new WaitForSeconds(delay);
        playerMovement.isAttacking = false;
    }
}