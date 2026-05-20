using UnityEngine;

[RequireComponent(typeof(Collider))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("Exact scene name as it appears in File → Build Settings.")]
    [SerializeField] private string targetScene;

    [Tooltip("Must match the Spawn Point ID on a SpawnPoint component in the target scene.")]
    [SerializeField] private string spawnPointID;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        // Ensure the collider is always a trigger — saves a step in the Inspector.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogWarning($"[DoorTrigger] '{gameObject.name}' has no Target Scene set!", this);
            return;
        }

        SceneTransitionManager.Instance?.TransitionTo(targetScene, spawnPointID);
    }

    // -------------------------------------------------------------------------
    // Editor Gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // Draw a transparent green fill so the trigger is visible in the Scene view.
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.25f);
        DrawColliderGizmo(col);

        // Draw a solid outline.
        Gizmos.color = new Color(0f, 1f, 0.4f, 0.9f);
        DrawColliderGizmoWire(col);

        // Arrow showing the "into" direction (forward of this transform).
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 1.2f);

        // Label showing the destination.
        if (!string.IsNullOrEmpty(targetScene))
        {
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.6f,
                $"→ {targetScene}\n  [{spawnPointID}]"
            );
        }
    }

    private void DrawColliderGizmo(Collider col)
    {
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        if (col is BoxCollider box)
            Gizmos.DrawCube(box.center, box.size);

        Gizmos.matrix = old;
    }

    private void DrawColliderGizmoWire(Collider col)
    {
        Matrix4x4 old = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

        if (col is BoxCollider box)
            Gizmos.DrawWireCube(box.center, box.size);

        Gizmos.matrix = old;
    }
#endif
}
