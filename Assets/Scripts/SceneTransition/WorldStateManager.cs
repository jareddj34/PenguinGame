using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that remembers what has happened in each scene so that
/// when the player returns, the world reflects their previous actions.
///
/// Two kinds of flags:
///
///   Scene-scoped  — tied to a specific scene (e.g. "chest_01 opened in Dungeon1").
///                   Use these for chests, breakables, switches, one-time enemies.
///
///   Global        — not tied to any scene (e.g. "boss_walrus_defeated", "has_sword").
///                   Use these for story progress, permanent item unlocks, boss kills.
///
/// Quick-start example (inside a Chest script):
///
///   void Open()
///   {
///       WorldStateManager.Instance.SetSceneFlag("chest_big_key", true);
///       PlayOpenAnimation();
///   }
///
///   void Start()
///   {
///       if (WorldStateManager.Instance.GetSceneFlag("chest_big_key"))
///           ShowAlreadyOpenState();
///   }
///
/// Or just add a StatefulObject component and wire it up in the Inspector — no code needed.
/// </summary>
public class WorldStateManager : MonoBehaviour
{
    public static WorldStateManager Instance { get; private set; }

    // Scene-scoped flags: key = "SceneName/objectID"
    private readonly Dictionary<string, bool> _sceneFlags = new();

    // Global flags: key = objectID (no scene prefix)
    private readonly Dictionary<string, bool> _globalFlags = new();

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------------------------
    // Scene-Scoped Flags
    // -------------------------------------------------------------------------

    /// <summary>
    /// Save a flag for <paramref name="objectID"/> in the currently active scene.
    /// </summary>
    public void SetSceneFlag(string objectID, bool value)
    {
        string key = SceneKey(objectID);
        _sceneFlags[key] = value;
    }

    /// <summary>
    /// Save a flag for <paramref name="objectID"/> in a specific named scene.
    /// Useful when setting flags for another scene (rare, but handy).
    /// </summary>
    public void SetSceneFlag(string sceneName, string objectID, bool value)
    {
        _sceneFlags[SceneKey(sceneName, objectID)] = value;
    }

    /// <summary>
    /// Returns the flag value for <paramref name="objectID"/> in the currently active scene.
    /// Returns <c>false</c> if no flag has been set.
    /// </summary>
    public bool GetSceneFlag(string objectID)
    {
        return _sceneFlags.TryGetValue(SceneKey(objectID), out bool val) && val;
    }

    /// <summary>
    /// Returns the flag value for <paramref name="objectID"/> in a specific named scene.
    /// </summary>
    public bool GetSceneFlag(string sceneName, string objectID)
    {
        return _sceneFlags.TryGetValue(SceneKey(sceneName, objectID), out bool val) && val;
    }

    /// <summary>
    /// Clears all flags for a specific scene. Call this if you want a scene to
    /// fully reset (e.g. a dungeon that should respawn everything on a new run).
    /// </summary>
    public void ClearSceneFlags(string sceneName)
    {
        string prefix = sceneName + "/";
        List<string> toRemove = new();

        foreach (string key in _sceneFlags.Keys)
        {
            if (key.StartsWith(prefix))
                toRemove.Add(key);
        }

        foreach (string key in toRemove)
            _sceneFlags.Remove(key);
    }

    // -------------------------------------------------------------------------
    // Global Flags
    // -------------------------------------------------------------------------

    /// <summary>
    /// Save a global flag not tied to any scene (story progress, boss kills, items).
    /// </summary>
    public void SetGlobalFlag(string flagID, bool value)
    {
        _globalFlags[flagID] = value;
    }

    /// <summary>
    /// Returns the global flag value. Returns <c>false</c> if not set.
    /// </summary>
    public bool GetGlobalFlag(string flagID)
    {
        return _globalFlags.TryGetValue(flagID, out bool val) && val;
    }

    /// <summary>
    /// Clears all flags — scene-scoped and global. Useful for a "New Game".
    /// </summary>
    public void ClearAll()
    {
        _sceneFlags.Clear();
        _globalFlags.Clear();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static string SceneKey(string objectID)
        => $"{SceneManager.GetActiveScene().name}/{objectID}";

    private static string SceneKey(string sceneName, string objectID)
        => $"{sceneName}/{objectID}";

    // -------------------------------------------------------------------------
    // Debug
    // -------------------------------------------------------------------------

    /// <summary>Logs all currently stored flags to the Console.</summary>
    [ContextMenu("Dump All Flags")]
    public void DumpFlags()
    {
        Debug.Log("=== WorldStateManager: Scene Flags ===");
        foreach (var kv in _sceneFlags)
            Debug.Log($"  [{kv.Key}] = {kv.Value}");

        Debug.Log("=== WorldStateManager: Global Flags ===");
        foreach (var kv in _globalFlags)
            Debug.Log($"  [{kv.Key}] = {kv.Value}");
    }
}
