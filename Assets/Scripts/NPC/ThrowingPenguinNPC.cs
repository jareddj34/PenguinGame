using UnityEngine;
using Yarn.Unity;
using System.Collections;

public class ThrowingPenguinNPC : InteractableBase
{
    // -------------------------------------------------------------------------
    // Inspector Fields
    // -------------------------------------------------------------------------

    [Header("Dialogue")]
    [Tooltip("The scene's Yarn Spinner DialogueRunner.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("The title of the Yarn node to run when the player presses E.")]
    [SerializeField] private string yarnNodeName;

    [Header("Throwing")]
    [Tooltip("Snowball prefab to instantiate on each throw.")]
    [SerializeField] private GameObject snowballPrefab;

    [Tooltip("Spawn point for snowballs. If left empty, uses this NPC's transform.")]
    [SerializeField] private Transform throwOrigin;

    [Tooltip("Seconds between each throw attempt.")]
    [SerializeField] private float throwInterval = 2f;

    [Tooltip("Seconds after the Throw trigger fires before the snowball spawns. " +
             "Tune this to match the release frame of your throw animation.")]
    [SerializeField] private float spawnDelay = 0.3f;

    [Tooltip("If true, the NPC rotates to face the player before each throw. " +
             "If false, it always throws in its original facing direction.")]
    [SerializeField] private bool aimAtPlayer = false;

    [Header("Animation")]
    public Animator animator;
    private static readonly int ThrowHash    = Animator.StringToHash("Throw");
    private static readonly int TalkingHash  = Animator.StringToHash("Talking");

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip[] honkClips;
    private AudioClip chosenHonk;

    /// <summary>True while a Yarn dialogue is open — pauses the throw loop.</summary>
    private bool _isInDialogue;

    private Quaternion _originalRotation;

    public override string InteractionPrompt => "Talk [E]";


    private void Start()
    {
        _originalRotation = transform.rotation;

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

        StartCoroutine(ThrowLoop());

        chosenHonk = honkClips[Random.Range(0, honkClips.Length)];
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }


    public override bool CanInteract()
    {
        return !_isInDialogue
            && dialogueRunner != null
            && !string.IsNullOrEmpty(yarnNodeName)
            && !dialogueRunner.IsDialogueRunning;
    }

    public override void Interact()
    {
        if (!CanInteract()) return;

        _isInDialogue = true;

        if (animator != null)
            animator.SetBool(TalkingHash, true);

        audioSource.clip = chosenHonk;
        audioSource.Play();

        GameStateManager.Instance.EnterDialogue();
        GameStateManager.Instance.FacePlayer(transform);
        StartCoroutine(FacePlayerCoroutine(GameStateManager.Instance.PlayerTransform));

        dialogueRunner.StartDialogue(yarnNodeName);
    }

    // -------------------------------------------------------------------------
    // Throw Loop
    // -------------------------------------------------------------------------

    private IEnumerator ThrowLoop()
    {
        while (true)
        {
            // ── WAIT ────────────────────────────────────────────────────────
            // Count down the interval only when not in dialogue, so the full
            // cooldown is consumed before the next throw regardless of pauses.
            float elapsed = 0f;
            while (elapsed < throwInterval)
            {
                if (!_isInDialogue)
                    elapsed += Time.deltaTime;
                yield return null;
            }

            // Skip this throw if dialogue started during the wait.
            if (_isInDialogue) continue;

            // ── AIM ──────────────────────────────────────────────────────────
            if (aimAtPlayer)
            {
                Transform player = GameStateManager.Instance.PlayerTransform;
                if (player != null)
                {
                    Vector3 dir = player.position - transform.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(dir);
                }
            }

            // ── THROW ANIMATION ───────────────────────────────────────────────
            if (animator != null)
                animator.SetTrigger(ThrowHash);

            // ── SPAWN DELAY ───────────────────────────────────────────────────
            // Waits for the animation's release frame before actually spawning.
            // The timer still runs if dialogue starts mid-windup — the snowball
            // is simply skipped thanks to the guard below.
            float spawnElapsed = 0f;
            while (spawnElapsed < spawnDelay)
            {
                spawnElapsed += Time.deltaTime;
                yield return null;
            }

            // ── SPAWN ────────────────────────────────────────────────────────
            if (!_isInDialogue)
                SpawnSnowball();
        }
    }

    private void SpawnSnowball()
    {
        if (snowballPrefab == null) return;
        Transform origin = throwOrigin != null ? throwOrigin : transform;
        GameObject snowball = Instantiate(snowballPrefab, origin.position, origin.transform.rotation);
        Snowball sb = snowball.GetComponent<Snowball>();
        if (sb != null) sb.owner = gameObject;
    }

    // -------------------------------------------------------------------------
    // Dialogue Callbacks
    // -------------------------------------------------------------------------

    private void OnDialogueComplete()
    {
        GameStateManager.Instance.ExitDialogue();

        if (animator != null)
            animator.SetBool(TalkingHash, false);

        _isInDialogue = false;

        StartCoroutine(ReturnToOriginalRotation());
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Quickly rotates the NPC to face the player (mirrors NPC.cs).</summary>
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

    /// <summary>Smoothly rotates back to the rotation the NPC had on Start.</summary>
    private IEnumerator ReturnToOriginalRotation()
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        const float duration = 0.2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, _originalRotation, t);
            yield return null;
        }

        transform.rotation = _originalRotation;
    }

    [YarnFunction("GiveSnowballs")]
    public static string GiveSnowballs()
    {
        PlayerThrow playerThrow = Object.FindObjectOfType<PlayerThrow>();

        if (playerThrow != null)
            playerThrow.AddAmmo(3);

        return "";
    }

    [YarnFunction("HasSnowballs")]
    public static bool HasSnowballs()
    {
        PlayerThrow playerThrow = Object.FindObjectOfType<PlayerThrow>();
        return playerThrow != null && playerThrow.snowballCount > 0;
    }
}
