using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Ice Sliding")]
    [Tooltip("How fast the player slides across ice.")]
    [SerializeField] private float iceSlideSpeed = 7f;
    public bool isSliding;
    private int iceZoneCount;
    private bool isOnIce => iceZoneCount > 0;

    // Components
    private CharacterController controller;
    private Animator animator;
    public bool isKnockedBack;
    // Cached animator parameter IDs (faster than passing strings every frame)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int DashHash  = Animator.StringToHash("Dash");
    private static readonly int ItemGotHash    = Animator.StringToHash("ItemGot");
    private static readonly int ItemGotEndHash = Animator.StringToHash("ItemGotEnd");
    private static readonly int HitFrontHash = Animator.StringToHash("HitFront");
    private static readonly int HitBackHash  = Animator.StringToHash("HitBack");
    private static readonly int HitLeftHash  = Animator.StringToHash("HitLeft");
    private static readonly int HitRightHash = Animator.StringToHash("HitRight");

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

    // Freeze state
    public bool isFrozen;

    // Shield
    private PlayerShield playerShield;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        playerShield = GetComponent<PlayerShield>();
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
        if (isDashing || isKnockedBack || isSliding)
            return;

        // Freeze the player during attack or receiving item or freeze
        if (isAttacking || isReceivingItem || isFrozen)
        {
            if (animator != null)
                animator.SetFloat(SpeedHash, 0f);
            return;
        }

        // Ice sliding: while on ice, override normal movement
        if (isOnIce)
        {
            if (!isSliding)
            {
                Vector3 iceInput = new Vector3(moveInput.x, 0f, moveInput.y);
                if (iceInput.sqrMagnitude > 0.01f)
                {
                    // Player pressed a direction — start sliding
                    StartCoroutine(IceSlideCoroutine(SnapToCardinal(iceInput)));
                }
                else
                {
                    // Standing still on ice: apply gravity but no movement
                    ApplyGravity();
                    controller.Move(new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
                    if (animator != null) animator.SetFloat(SpeedHash, 0f);
                }
            }
            return; // coroutine handles everything while isSliding is true
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

        if (isDashing || dashCooldownTimer > 0f || isSliding)
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

        Vector3 motion = moveDirection * moveSpeed * (playerShield != null ? playerShield.SpeedMultiplier : 1f);

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
        playerShield?.ForceDropShield();

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

    // -------------------------------------------------------------------------
    // Knock back
    // -------------------------------------------------------------------------
    public void StartKnockback(HitDirection dir, float force)
    {
        StartCoroutine(KnockbackCoroutine(dir, force));
    }

    private IEnumerator KnockbackCoroutine(HitDirection dir, float force)
    {
        isKnockedBack = true;

        // Trigger the animation
        int hash = dir switch
        {
            HitDirection.Front => HitFrontHash,
            HitDirection.Back  => HitBackHash,
            HitDirection.Left  => HitLeftHash,
            HitDirection.Right => HitRightHash,
            _ => HitFrontHash
        };
        if (animator != null) animator.SetTrigger(hash);

        // Calculate launch direction (opposite of hit side)
        Vector3 knockbackDir = dir switch
        {
            HitDirection.Front => -transform.forward,
            HitDirection.Back  =>  transform.forward,
            HitDirection.Left  =>  transform.right,
            HitDirection.Right => -transform.right,
            _ => Vector3.zero
        };

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration); // decelerate
            Vector3 motion = knockbackDir * force * t;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        isKnockedBack = false;
    }

    // -------------------------------------------------------------------------
    // Ice Sliding
    // -------------------------------------------------------------------------

    /// <summary>Called by IceZone when the player enters the trigger.</summary>
    public void EnterIce()
    {
        iceZoneCount++;

        Vector3 input = new Vector3(moveInput.x, 0f, moveInput.y);
        if (!isSliding && input.sqrMagnitude > 0.01f)
            StartCoroutine(IceSlideCoroutine(SnapToCardinal(input)));
    }

    public void ExitIce()
    {
        iceZoneCount = Mathf.Max(0, iceZoneCount - 1);
    }

    private IEnumerator IceSlideCoroutine(Vector3 direction)
    {
        isSliding = true;

        // Snap rotation to face the slide direction instantly.
        transform.rotation = Quaternion.LookRotation(direction);

        if (animator != null)
            animator.SetFloat(SpeedHash, 1f);

        while (true)
        {
            // Slid off the edge of the ice zone — stop.
            if (!isOnIce) break;

            ApplyGravity();

            Vector3 motion = direction * iceSlideSpeed;
            motion.y = verticalVelocity;

            CollisionFlags flags = controller.Move(motion * Time.deltaTime);

            // Hit a wall, rock, or any solid collider on the sides — stop.
            if ((flags & CollisionFlags.Sides) != 0) break;

            yield return null;
        }

        isSliding = false;

        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }

    /// <summary>Snaps a direction to the nearest of the four cardinal axes (N/S/E/W).</summary>
    private static Vector3 SnapToCardinal(Vector3 dir)
    {
        dir.y = 0f;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return new Vector3(Mathf.Sign(dir.x), 0f, 0f);
        else
            return new Vector3(0f, 0f, Mathf.Sign(dir.z));
    }


}
