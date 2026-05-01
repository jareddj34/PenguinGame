using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down 3D player movement controller with dash and animation.
/// Works with a PlayerInput component (Behavior: Send Messages) and a CharacterController.
///
/// Setup:
///   1. Add this script to your player GameObject (alongside the PlayerInput component).
///   2. Make sure PlayerInput > Behavior is set to "Send Messages".
///   3. Unity will auto-add a CharacterController — size the capsule to fit your mesh.
///   4. Animator setup: add a float parameter named "Speed". Use it to drive
///      Idle -> Walk (Speed > 0) and Walk -> Idle (Speed < 0.01) transitions.
///      Uncheck "Has Exit Time" on both transitions for instant response.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("How fast the player moves across the ground.")]
    [SerializeField] private float moveSpeed = 5f;

    [Tooltip("Gravity applied when the character is airborne.")]
    [SerializeField] private float gravity = -20f;

    [Header("Rotation")]
    [Tooltip("How quickly the player rotates to face the movement direction (degrees per second). Higher = snappier.")]
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Dash")]
    [Tooltip("How fast the player moves during a dash.")]
    [SerializeField] private float dashSpeed = 20f;

    [Tooltip("How long the dash lasts in seconds.")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("How long before the player can dash again in seconds. Set to 0 to disable cooldown.")]
    [SerializeField] private float dashCooldown = 1f;

    // Components
    private CharacterController controller;
    private Animator animator;
    // Cached animator parameter IDs (faster than passing strings every frame)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashHash  = Animator.StringToHash("Dash");
    private static readonly int ItemGotHash    = Animator.StringToHash("ItemGot");
    private static readonly int ItemGotEndHash = Animator.StringToHash("ItemGotEnd");

    // State
    private Vector2 moveInput;
    private float verticalVelocity;

    // Dash state
    public bool isDashing;
    private float dashCooldownTimer;
    private Vector3 dashDirection;

    // Attack state
    public bool isAttacking;

    // Item got state
    public bool isReceivingItem;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    private void OnDisable()
    {
        // Prevents the player drifting when control is restored after dialogue
        moveInput = Vector2.zero;
    }

    private void Update()
    {
        // Tick down the cooldown timer
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        // While dashing, skip normal movement — the coroutine handles it
        if (isDashing)
            return;

        // While attacking, freeze movement but zero out Speed so the walk
        // animation doesn't keep playing and interfere with the attack state
        if (isAttacking || isReceivingItem)
        {
            if (animator != null)
                animator.SetFloat(SpeedHash, 0f);
            return;
        }

        ApplyGravity();
        MoveCharacter();
        RotateCharacter();
        UpdateAnimator();
    }

    // -------------------------------------------------------------------------
    // Input Messages (called automatically by PlayerInput "Send Messages")
    // -------------------------------------------------------------------------

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnSprint(InputValue value)
    {
        if (!value.isPressed)
            return;

        if (isDashing || dashCooldownTimer > 0f)
            return;

        StartCoroutine(DashCoroutine());
    }

    // -------------------------------------------------------------------------
    // Movement & Rotation
    // -------------------------------------------------------------------------

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private void MoveCharacter()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 motion = moveDirection * moveSpeed;
        motion.y = verticalVelocity;

        controller.Move(motion * Time.deltaTime);
    }

    private void RotateCharacter()
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    // -------------------------------------------------------------------------
    // Animation
    // -------------------------------------------------------------------------

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Pass the input magnitude so the Animator knows if we're moving
        animator.SetFloat(SpeedHash, moveInput.magnitude);
    }

    // -------------------------------------------------------------------------
    // Dash
    // -------------------------------------------------------------------------

    private IEnumerator DashCoroutine()
    {
        isDashing = true;
        dashCooldownTimer = dashCooldown;

        // Fire the animator trigger so the dash animation plays immediately
        if (animator != null)
            animator.SetTrigger(DashHash);
        
        yield return new WaitForSeconds(0.1f);

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        dashDirection = input.sqrMagnitude > 0.01f ? input.normalized : transform.forward;

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            Vector3 motion = dashDirection * dashSpeed;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
    }

    // -------------------------------------------------------------------------
    // Item Got
    // -------------------------------------------------------------------------

    [ContextMenu("Trigger Item Got")] // For testing in the editor
    public void TriggerItemGot()
    {
        if(!isReceivingItem) {
            isReceivingItem = true;
            animator.SetTrigger(ItemGotHash);

            // Rotate player to face y 180
            Quaternion targetRotation = Quaternion.Euler(0f, 180f, 0f);
            transform.rotation = targetRotation;
        }
            
    }

    [ContextMenu("End Item Got")] // For testing in the editor
    public void EndItemGot()
    {
        if(isReceivingItem) {
            isReceivingItem = false;
            animator.SetTrigger(ItemGotEndHash);

            GameStateManager.Instance?.ExitReceivingItem();
        }
    }


}
