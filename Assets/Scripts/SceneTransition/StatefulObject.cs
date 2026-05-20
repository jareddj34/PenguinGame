using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Add this component to any GameObject that should remember its state between
/// scene visits: chests, breakable crates, one-time enemies, levers, locked doors, etc.
///
/// How it works:
///   • On Start, it asks WorldStateManager "has this object been completed before?"
///   • If YES → it either disables the GameObject or fires <see cref="onAlreadyDone"/>
///             so you can show an already-opened chest, a smashed crate, etc.
///   • If NO  → nothing happens; the object behaves normally.
///   • When the action completes, call <see cref="MarkDone"/> from your own script
///     (or wire it up in the Inspector via a Button or Animation Event).
///
/// ── Inspector Setup ──────────────────────────────────────────────────────────
///
///   Object ID          Unique string within this scene. Convention: "chest_big_key",
///                      "crate_entrance_01", "enemy_miniboss". Must be unique per scene.
///
///   Disable When Done  Tick this for objects that should simply vanish on revisit
///                      (destroyed crates, collected items, dead one-time enemies).
///                      Leave unticked for objects that need a visual change instead
///                      (chest stays visible but shows as already opened).
///
///   On Already Done    UnityEvent fired when the scene loads and this object's flag
///                      is already set. Wire up your "show open state" method here.
///
/// ── Code Usage ────────────────────────────────────────────────────────────────
///
///   // In your Chest script:
///   [SerializeField] private StatefulObject stateful;
///
///   void OnOpen()
///   {
///       stateful.MarkDone();   // saves the flag and handles disable/event
///       PlayOpenAnimation();
///   }
///
///   // Or get the component at runtime:
///   GetComponent<StatefulObject>().MarkDone();
///
/// </summary>
public class StatefulObject : MonoBehaviour
{
    [Tooltip("Unique ID for this object within its scene. Examples: 'chest_big_key', 'crate_north_01'.")]
    [SerializeField] private string objectID;

    [Tooltip(
        "If ticked, the GameObject is deactivated when MarkDone() is called, " +
        "and on future visits it is deactivated immediately in Start before anyone sees it. " +
        "Best for destroyed objects, collected items, dead one-time enemies.\n\n" +
        "If unticked, onAlreadyDone fires instead — use this for chests that should " +
        "appear visually open rather than disappear entirely."
    )]
    [SerializeField] private bool disableWhenDone = false;

    [Space]
    [Tooltip("Fired in Start if this object was already completed in a previous visit to this scene.")]
    public UnityEvent onAlreadyDone;

    // -------------------------------------------------------------------------
    // Public Properties
    // -------------------------------------------------------------------------

    /// <summary>True if this object's flag is set in WorldStateManager.</summary>
    public bool IsDone =>
        !string.IsNullOrEmpty(objectID) &&
        WorldStateManager.Instance != null &&
        WorldStateManager.Instance.GetSceneFlag(objectID);

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(objectID))
        {
            Debug.LogWarning($"[StatefulObject] '{gameObject.name}' has no Object ID set — state will not be saved.", this);
            return;
        }

        if (WorldStateManager.Instance == null)
        {
            Debug.LogWarning("[StatefulObject] WorldStateManager not found in scene. Make sure it exists in your first scene.", this);
            return;
        }

        if (!IsDone) return;

        // This object was already completed on a previous visit.
        if (disableWhenDone)
        {
            gameObject.SetActive(false);
        }
        else
        {
            onAlreadyDone?.Invoke();
        }
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Call this when the object's action is completed (chest opened, enemy killed,
    /// crate smashed, lever pulled, etc.). Saves the flag and handles disable/event.
    /// </summary>
    public void MarkDone()
    {
        if (string.IsNullOrWhiteSpace(objectID))
        {
            Debug.LogWarning($"[StatefulObject] '{gameObject.name}' has no Object ID — cannot save state.", this);
            return;
        }

        if (WorldStateManager.Instance == null)
        {
            Debug.LogWarning("[StatefulObject] WorldStateManager not found. State not saved.", this);
            return;
        }

        WorldStateManager.Instance.SetSceneFlag(objectID, true);

        if (disableWhenDone)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Resets this object's flag in WorldStateManager, allowing it to be
    /// triggered again (useful for testing or special gameplay mechanics).
    /// </summary>
    public void ResetState()
    {
        if (WorldStateManager.Instance == null) return;
        WorldStateManager.Instance.SetSceneFlag(objectID, false);
    }

    // -------------------------------------------------------------------------
    // Editor Gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Highlight the object in the scene view with a label showing its ID.
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 1.2f,
            string.IsNullOrEmpty(objectID)
                ? "⚠ No Object ID"
                : $"State ID: {objectID}"
        );
    }
#endif
}
