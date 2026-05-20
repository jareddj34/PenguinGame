using UnityEngine;

/// <summary>
/// Marks where the player should appear when arriving in this scene from a
/// specific DoorTrigger in another scene.
///
/// Setup:
///   1. Place an empty GameObject at the desired spawn position/rotation.
///   2. Add this component and give it a unique Spawn Point ID.
///   3. In the other scene, set a DoorTrigger's Spawn Point ID to the same string.
///
/// Naming convention suggestion: "{scene}_{direction}"
///   e.g. "overworld_north", "dungeon1_entrance", "house_door"
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Must match the Spawn Point ID on the DoorTrigger that leads here.")]
    [SerializeField] private string spawnPointID;

    /// <summary>The ID SceneTransitionManager will match against.</summary>
    public string SpawnPointID => spawnPointID;

    // -------------------------------------------------------------------------
    // Editor Gizmos
    // -------------------------------------------------------------------------

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Sphere at the spawn position.
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
        Gizmos.DrawSphere(transform.position, 0.25f);

        // Arrow showing which direction the player will face.
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.9f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.8f);

        // Label showing the ID directly in the scene view.
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.55f,
            string.IsNullOrEmpty(spawnPointID) ? "(no ID)" : $"Spawn: {spawnPointID}"
        );
    }
#endif
}
