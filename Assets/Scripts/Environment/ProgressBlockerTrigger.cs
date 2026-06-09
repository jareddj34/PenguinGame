using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to a GameObject with a trigger BoxCollider.
/// When the player walks in, stops them, has the assigned NPC turn to face them
/// and play their dialogue, then walks the player back one step — just like the
/// "you can't go this way yet" moment in Pokémon.
///
/// After the blocking dialogue has played once the trigger can optionally be
/// disabled so the player is never stopped again (set <see cref="disableAfterFirstTrigger"/>).
/// To permanently unlock the path, call <see cref="Unlock"/> from a Yarn command
/// or another script.
/// </summary>
public class ProgressBlockerTrigger : MonoBehaviour
{
    [Header("NPC")]
    [Tooltip("The NPC that should turn to the player and speak.")]
    [SerializeField] private NPC blockerNPC;

    [Header("Pushback")]
    [Tooltip("How far (in world units) the player is nudged back after the dialogue ends.")]
    [SerializeField] private float pushbackDistance = 1.2f;

    [Tooltip("How long the pushback slide takes in seconds.")]
    [SerializeField] private float pushbackDuration = 0.35f;

    [Header("Behaviour")]
    [Tooltip("Disable this trigger permanently after it fires once.")]
    [SerializeField] private bool disableAfterFirstTrigger = false;

    // -------------------------------------------------------------------------

    private bool _isRunning;

    // -------------------------------------------------------------------------

    void Start()
    {
        StatefulObject stateful = this.GetComponent<StatefulObject>();

        if(stateful.IsDone)
        {
            this.GetComponent<Collider>().enabled = false;
        }
    }

    // -------------------------------------------------------------------------
    // Trigger
    // -------------------------------------------------------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (_isRunning) return;
        if (!other.CompareTag("Player")) return;
        if (blockerNPC == null) return;
        if (!blockerNPC.CanInteract()) return;

        StartCoroutine(BlockerSequence(other));
    }

    // -------------------------------------------------------------------------
    // Sequence
    // -------------------------------------------------------------------------

    private IEnumerator BlockerSequence(Collider playerCollider)
    {
        _isRunning = true;

        // 1. Grab the components we need from the player.
        PlayerMovement playerMovement = playerCollider.GetComponent<PlayerMovement>();
        CharacterController cc = playerCollider.GetComponent<CharacterController>();

        // 2. Stop the player immediately.
        if (playerMovement != null)
            playerMovement.enabled = false;

        // Record where the player entered so we know which way "back" is.
        Vector3 entryPosition = playerCollider.transform.position;
        // "Back" is the direction pointing away from this trigger's centre.
        Vector3 toPlayer = (entryPosition - transform.position);
        toPlayer.y = 0f;
        // Vector3 pushDir = toPlayer.sqrMagnitude > 0.001f
        //     ? toPlayer.normalized
        //     : -playerCollider.transform.forward; // fallback
        Vector3 pushDir = -transform.forward;

        // 3. Turn the player to face the NPC.
        GameStateManager.Instance?.FacePlayer(blockerNPC.transform);
        yield return new WaitForSeconds(0.25f); // let the rotation finish

        // 4. Trigger the NPC — this locks input via GameStateManager and starts dialogue.
        //    Skip the dialogue camera since the NPC may be far from the player.
        FindFirstObjectByType<CameraController>()?.SuppressNextDialogueCamera();
        blockerNPC.Interact();

        // 5. Wait for dialogue to finish (GameStateManager returns to Normal state).
        yield return new WaitUntil(() =>
            GameStateManager.Instance == null ||
            GameStateManager.Instance.CurrentState == GameState.Normal);

        // Small pause so it doesn't feel instant.
        yield return new WaitForSeconds(0.1f);

        // 6. Turn the player to face away from the NPC (back the way they came).
        if (playerCollider != null)
        {
            Quaternion awayFromNPC = Quaternion.LookRotation(pushDir, Vector3.up);
            playerCollider.transform.rotation = awayFromNPC;
        }

        yield return new WaitForSeconds(0.1f);

        // 7. Nudge the player back.
        if (playerCollider != null && cc != null)
        {
            Vector3 start = playerCollider.transform.position;
            Vector3 end   = start + pushDir * pushbackDistance;
            float elapsed = 0f;

            while (elapsed < pushbackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / pushbackDuration);
                Vector3 target = Vector3.Lerp(start, end, t);
                Vector3 delta  = target - playerCollider.transform.position;
                cc.Move(delta);
                yield return null;
            }
        }

        // 8. Re-enable player movement (GameStateManager may have already done this,
        //    but be explicit so the freeze never sticks if the sequence was interrupted).
        if (playerMovement != null)
            playerMovement.enabled = true;

        if (disableAfterFirstTrigger)
            gameObject.SetActive(false);

        _isRunning = false;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this to permanently remove the blocker (e.g. after the player
    /// obtains the required item). The trigger collider is disabled so the
    /// player can walk through freely.
    /// </summary>
    public void Unlock()
    {
        this.GetComponent<StatefulObject>().MarkDone();

        this.GetComponent<Collider>().enabled = false;
    }
}
