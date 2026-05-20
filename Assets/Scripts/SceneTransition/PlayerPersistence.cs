using UnityEngine;

/// <summary>
/// Attach this to the player root GameObject in your first/starting scene.
/// It calls DontDestroyOnLoad so the player — with all their health, inventory,
/// and component state — survives every scene transitions.
///
/// The starting scene MUST keep the player prefab so the game can boot correctly.
/// When the player returns to the starting scene, the scene's copy is automatically
/// destroyed and the persistent player takes over — this is expected behaviour,
/// not an error.
///
/// Only add a player prefab to your starting scene. All other scenes should have
/// only SpawnPoint markers. SceneTransitionManager handles the rest.
/// </summary>
public class PlayerPersistence : MonoBehaviour
{
    private void Awake()
    {
        PlayerPersistence[] all = FindObjectsByType<PlayerPersistence>(FindObjectsSortMode.None);
        if (all.Length > 1)
        {
            // A persistent player already exists from a previous scene.
            // Quietly destroy this scene's copy — this is normal when returning
            // to the starting scene after a transition.
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
}
