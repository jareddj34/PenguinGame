using UnityEngine;
using Yarn.Unity;
using System.Collections;

public class NPC : InteractableBase
{
    [Header("Dialogue")]
    [Tooltip("The scene's Yarn Spinner DialogueRunner.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("The title of the Yarn node to run when the player presses E.\n" +
             "Must match the 'title:' header in your .yarn file exactly.")]
    [SerializeField] private string yarnNodeName;

    public override string InteractionPrompt => "Talk [E]";

    [Header("Animation")]
    public Animator animator;
    private Quaternion originalRotation;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {

        // Re-enable player control when dialogue ends
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

        originalRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    // -------------------------------------------------------------------------
    // IInteractable — called by InteractionZone / InteractionManager
    // -------------------------------------------------------------------------

    /// <summary>Prevent interaction if dialogue is already running.</summary>
    public override bool CanInteract()
    {
        return dialogueRunner != null
            && !string.IsNullOrEmpty(yarnNodeName)
            && !dialogueRunner.IsDialogueRunning;
    }

    /// <summary>Lock the player and start the assigned Yarn node.</summary>
    public override void Interact()
    {
        if (!CanInteract()) return;

        animator.SetBool("Talking", true);

        GameStateManager.Instance.EnterDialogue();
        GameStateManager.Instance.FacePlayer(transform);

        StartCoroutine(FacePlayerCoroutine(GameStateManager.Instance.PlayerTransform));

        dialogueRunner.StartDialogue(yarnNodeName);
    }

    public IEnumerator FacePlayerCoroutine(Transform target)
    {

        Vector3 direction = target.position - transform.position;
        direction.y = 0f; // keep it flat — no tilting up/down

        if (direction.sqrMagnitude < 0.001f) yield break;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;
        const float duration = .2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.rotation = targetRotation;
    }

    // -------------------------------------------------------------------------
    // Dialogue Completion
    // -------------------------------------------------------------------------

    private void OnDialogueComplete()
    {
        GameStateManager.Instance.ExitDialogue();

        animator.SetBool("Talking", false);

        StartCoroutine(ChangeRotationCoroutine(originalRotation));
    }

    IEnumerator ChangeRotationCoroutine(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.rotation;
        float elapsed = 0f;
        const float duration = .2f;

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