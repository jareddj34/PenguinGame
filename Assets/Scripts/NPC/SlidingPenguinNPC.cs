using UnityEngine;
using Yarn.Unity;
using System.Collections;

/// <summary>
/// A special penguin NPC that continuously belly-slides between two waypoints.
///
/// Sequence per leg:
///   SlideStart anim  →  move to waypoint  →  SlideEnd anim
///   →  idle pause (INTERACTABLE)  →  wait for dialogue to finish
///   →  rotate to face next waypoint (INTERACTABLE)  →  wait for dialogue to finish
///   →  repeat
///
/// The player can interact during the idle pause and rotation.
/// After dialogue ends, the penguin resumes exactly where the coroutine left off.
/// </summary>
public class SlidingPenguinNPC : InteractableBase
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Dialogue")]
    [Tooltip("The scene's Yarn Spinner DialogueRunner.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("The title of the Yarn node to run when the player presses E.")]
    [SerializeField] private string yarnNodeName;

    [Header("Waypoints")]
    [Tooltip("First waypoint. The penguin snaps here on Start and slides toward Point B first.")]
    [SerializeField] private Transform pointA;

    [Tooltip("Second waypoint.")]
    [SerializeField] private Transform pointB;

    [Header("Movement")]
    [Tooltip("Units per second while sliding.")]
    [SerializeField] private float slideSpeed = 3f;

    [Tooltip("Distance from the waypoint at which the penguin is considered to have arrived.")]
    [SerializeField] private float arrivalThreshold = 0.05f;

    [Header("Timing")]
    [Tooltip("How long the SlideStart animation plays before the penguin begins moving.")]
    [SerializeField] private float slideStartAnimDuration = 0.5f;

    [Tooltip("How long the SlideEnd animation plays after the penguin stops moving.")]
    [SerializeField] private float slideEndAnimDuration = 0.5f;

    [Tooltip("How long the penguin idles at each waypoint before rotating. " +
             "This is the main interactable window.")]
    [SerializeField] private float idlePauseDuration = 1.5f;

    [Tooltip("How many seconds the penguin takes to rotate toward the next waypoint.")]
    [SerializeField] private float rotationDuration = 0.4f;

    [Header("Animation")]
    public Animator animator;

    // -------------------------------------------------------------------------
    // Runtime State
    // -------------------------------------------------------------------------

    /// <summary>True while a Yarn dialogue is open.</summary>
    private bool _isInDialogue;

    /// <summary>True during the idle pause and rotation — the player can talk then.</summary>
    private bool _canInteractNow;

    /// <summary>Index of the waypoint we are sliding *toward* this leg (0 = A, 1 = B).</summary>
    private int _nextTargetIndex;

    public override string InteractionPrompt => "Talk [E]";

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

        // Snap to pointA facing pointB, then begin the loop.
        if (pointA != null)
            transform.position = pointA.position;

        if (pointA != null && pointB != null)
        {
            Vector3 initialDir = pointB.position - pointA.position;
            initialDir.y = 0f;
            if (initialDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(initialDir);
        }

        _nextTargetIndex = 1; // first leg goes toward pointB
        StartCoroutine(SlideLoop());
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    // -------------------------------------------------------------------------
    // IInteractable
    // -------------------------------------------------------------------------

    public override bool CanInteract()
    {
        return _canInteractNow
            && dialogueRunner != null
            && !string.IsNullOrEmpty(yarnNodeName)
            && !dialogueRunner.IsDialogueRunning;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        _isInDialogue = true;
        animator.SetBool("Talking", true);

        GameStateManager.Instance.EnterDialogue();
        GameStateManager.Instance.FacePlayer(transform);
        StartCoroutine(FacePlayerCoroutine(GameStateManager.Instance.PlayerTransform));

        dialogueRunner.StartDialogue(yarnNodeName);
    }

    // -------------------------------------------------------------------------
    // Main Slide Loop
    // -------------------------------------------------------------------------

    private IEnumerator SlideLoop()
    {
        while (true)
        {
            // ── SLIDE START ──────────────────────────────────────────────────
            _canInteractNow = false;
            Transform destination = _nextTargetIndex == 0 ? pointA : pointB;

            animator.SetTrigger("SlideStart");
            yield return new WaitForSeconds(slideStartAnimDuration);

            // ── MOVE ─────────────────────────────────────────────────────────
            while (Vector3.Distance(transform.position, destination.position) > arrivalThreshold)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    destination.position,
                    slideSpeed * Time.deltaTime);
                yield return null;
            }
            transform.position = destination.position;

            // ── SLIDE END ────────────────────────────────────────────────────
            animator.SetTrigger("SlideEnd");
            yield return new WaitForSeconds(slideEndAnimDuration);

            // ── IDLE PAUSE (interactable) ────────────────────────────────────
            _canInteractNow = true;
            yield return new WaitForSeconds(idlePauseDuration);

            // If dialogue started during the idle pause, wait for it to finish.
            yield return new WaitUntil(() => !_isInDialogue);

            // ── ROTATE TO FACE NEXT WAYPOINT (still interactable) ────────────
            _nextTargetIndex = 1 - _nextTargetIndex;
            Transform nextDestination = _nextTargetIndex == 0 ? pointA : pointB;

            Vector3 dir = nextDestination.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
                yield return StartCoroutine(RotateToFace(dir));

            // If dialogue started during the rotation, wait for it to finish.
            yield return new WaitUntil(() => !_isInDialogue);

            _canInteractNow = false;
        }
    }

    /// <summary>
    /// Smoothly rotates to face <paramref name="direction"/>.
    /// Pauses rotation while dialogue is active, then resumes smoothly from
    /// wherever the penguin ended up facing (e.g. toward the player).
    /// </summary>
    private IEnumerator RotateToFace(Vector3 direction)
    {
        Quaternion targetRot = Quaternion.LookRotation(direction);
        Quaternion startRot  = transform.rotation;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            if (!_isInDialogue)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rotationDuration);
                transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            }
            else
            {
                // Dialogue is running — FacePlayerCoroutine is moving our rotation.
                // Reset so we smoothly re-rotate to the waypoint after it ends.
                startRot = transform.rotation;
                elapsed  = 0f;
            }

            yield return null;
        }

        if (!_isInDialogue)
            transform.rotation = targetRot;
    }

    // -------------------------------------------------------------------------
    // Dialogue Callbacks
    // -------------------------------------------------------------------------

    private void OnDialogueComplete()
    {
        GameStateManager.Instance.ExitDialogue();
        animator.SetBool("Talking", false);

        // Unblock the SlideLoop coroutine — it will handle re-orienting and resuming.
        _isInDialogue = false;
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Quickly rotates the penguin to face the player (mirrors NPC.cs).</summary>
    private IEnumerator FacePlayerCoroutine(Transform target)
    {
        if (target == null) yield break;

        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) yield break;

        Quaternion startRotation  = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;
        const float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }
}
