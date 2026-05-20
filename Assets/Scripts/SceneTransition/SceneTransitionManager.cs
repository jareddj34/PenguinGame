using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent singleton that drives scene transitions.
/// Place this GameObject in your first/main scene — DontDestroyOnLoad keeps it
/// alive for the entire game session.
///
/// Usage:
///   SceneTransitionManager.Instance.TransitionTo("DungeonScene", "entrance_south");
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [Tooltip("How long the fade to/from black takes, in seconds.")]
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("Color of the transition fade. Black is standard.")]
    [SerializeField] private Color fadeColor = Color.black;

    [Header("Walk-Through Settings")]
    [Tooltip("Speed at which the player auto-walks during the transition fade. " +
             "Set to 0 to disable the walk-through effect.")]
    [SerializeField] private float transitionWalkSpeed = 3f;

    // Cached animator parameter — matches the hash used in PlayerMovement.
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    // The spawn point ID to look for once the new scene loads.
    private string _pendingSpawnPointID;

    // Whether a transition is currently in progress (prevents double-triggers).
    private bool _isTransitioning;

    // Dynamically-created fullscreen fade overlay.
    private Image _fadeImage;

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

        CreateFadeOverlay();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    public void TransitionTo(string targetScene, string spawnPointID)
    {
        if (_isTransitioning) return;
        _pendingSpawnPointID = spawnPointID;
        StartCoroutine(TransitionCoroutine(targetScene));
    }

    // -------------------------------------------------------------------------
    // Transition Coroutine
    // -------------------------------------------------------------------------

    private IEnumerator TransitionCoroutine(string targetScene)
    {
        _isTransitioning = true;

        // Lock player input so they can't steer during the transition.
        SetPlayerInputEnabled(false);

        // Auto-walk the player forward (into the door) while fading out.
        // Started without yielding so it runs in parallel with the fade.
        PlayerMovement pmOut = FindFirstObjectByType<PlayerMovement>();
        if (pmOut != null && transitionWalkSpeed > 0f)
            StartCoroutine(AutoWalkCoroutine(pmOut, fadeDuration));

        // Fade to black (same duration as the auto-walk, so they finish together).
        yield return StartCoroutine(Fade(0f, 1f));

        // Begin async load but don't activate it yet.
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);
        op.allowSceneActivation = false;

        // Wait until Unity has loaded all assets (progress stops at 0.9 until we allow activation).
        while (op.progress < 0.9f)
            yield return null;

        // Activate the scene.
        op.allowSceneActivation = true;

        // Give Awake / Start calls a couple of frames to finish.
        yield return null;
        yield return null;
    }

    // -------------------------------------------------------------------------
    // Post-Load Handling
    // -------------------------------------------------------------------------

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Only react to transitions we started (not the initial game boot).
        if (!_isTransitioning) return;

        StartCoroutine(OnSceneLoadedCoroutine());
    }

    private IEnumerator OnSceneLoadedCoroutine()
    {
        // Wait a frame so every Awake / Start in the new scene has run.
        // This also gives PlayerPersistence time to destroy any duplicate player
        // prefab that the returning scene may have contained.
        yield return null;

        // Move the player to the correct entry point.
        PlacePlayerAtSpawnPoint();

        // Always rewire follow cameras — even if no spawn point was found
        // (e.g. returning to the starting scene which already has cameras
        // that lost their serialized player reference when the duplicate was destroyed).
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
            RewireFollowCameras(pm.transform);

        // Give Cinemachine one frame to compute the snapped position before we
        // start fading in. Without this the camera would still be at its scene-editor
        // position on the first visible frame.
        yield return null;

        // Auto-walk the player forward (out of the door) while fading in.
        // Started without yielding so it runs in parallel with the fade.
        if (pm != null && transitionWalkSpeed > 0f)
            StartCoroutine(AutoWalkCoroutine(pm, fadeDuration));

        // Fade back in.
        yield return StartCoroutine(Fade(1f, 0f));

        // Re-enable player input once the walk-through and fade are both done.
        SetPlayerInputEnabled(true);

        _isTransitioning = false;
    }

    // -------------------------------------------------------------------------
    // Player Placement
    // -------------------------------------------------------------------------

    private void PlacePlayerAtSpawnPoint()
    {
        if (string.IsNullOrEmpty(_pendingSpawnPointID)) return;

        SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);

        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.SpawnPointID != _pendingSpawnPointID) continue;

            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) break;

            // CharacterController must be disabled to warp position without fighting physics.
            CharacterController cc = pm.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            pm.transform.SetPositionAndRotation(sp.transform.position, sp.transform.rotation);

            if (cc != null) cc.enabled = true;

            break;
        }

        _pendingSpawnPointID = null;
    }

    // -------------------------------------------------------------------------
    // Camera Rewiring
    // -------------------------------------------------------------------------

    /// <summary>
    /// After a scene load, virtual cameras in the new scene have null Follow/LookAt
    /// targets because the player persists via DontDestroyOnLoad and was never placed
    /// in those scenes. This reassigns Follow and LookAt on any CinemachineCamera
    /// that has a CinemachineFollow component (i.e. a "follow the player" camera),
    /// leaving dialogue and fixed cameras untouched.
    /// </summary>
    private void RewireFollowCameras(Transform playerTransform)
    {
        CinemachineCamera[] vcams = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);

        foreach (CinemachineCamera vcam in vcams)
        {
            // CinemachineFollow is the procedural component added to follow cameras.
            // Dialogue/fixed cameras don't have it, so we skip them automatically.
            if (!vcam.TryGetComponent<CinemachineFollow>(out _)) continue;

            vcam.Follow = playerTransform;
            vcam.LookAt = playerTransform;

            // Tell Cinemachine there is no valid previous state so it snaps
            // directly to the target instead of damping from the scene's
            // default (editor-placed) camera position.
            vcam.PreviousStateIsValid = false;
        }
    }

    // -------------------------------------------------------------------------
    // Auto-Walk
    // -------------------------------------------------------------------------

    /// <summary>
    /// Moves the player forward at <see cref="transitionWalkSpeed"/> for
    /// <paramref name="duration"/> seconds, playing the walk animation throughout.
    /// Used to give the "walking through a door" feel during the fade transition.
    /// PlayerMovement is disabled while this runs so there is no conflict.
    /// </summary>
    private IEnumerator AutoWalkCoroutine(PlayerMovement pm, float duration)
    {
        CharacterController cc = pm.GetComponent<CharacterController>();
        Animator animator      = pm.GetComponentInChildren<Animator>();

        if (cc == null) yield break;

        // Snapshot the forward direction at the moment the walk starts.
        Vector3 walkDir        = pm.transform.forward;
        float   verticalVel    = 0f;
        float   elapsed        = 0f;

        if (animator != null)
            animator.SetFloat(SpeedHash, 1f);

        while (elapsed < duration)
        {
            // Keep the player grounded on slopes.
            if (cc.isGrounded && verticalVel < 0f)
                verticalVel = -2f;
            else
                verticalVel += Physics.gravity.y * Time.deltaTime;

            Vector3 motion = walkDir * transitionWalkSpeed;
            motion.y = verticalVel;
            cc.Move(motion * Time.deltaTime);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (animator != null)
            animator.SetFloat(SpeedHash, 0f);
    }

    // -------------------------------------------------------------------------
    // Input Lock
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables PlayerMovement and PlayerAttack directly.
    /// We do this ourselves rather than going through GameStateManager because
    /// this manager persists across scenes and GameStateManager does not.
    /// </summary>
    private void SetPlayerInputEnabled(bool enabled)
    {
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null) pm.enabled = enabled;

        PlayerAttack pa = FindFirstObjectByType<PlayerAttack>();
        if (pa != null) pa.enabled = enabled;
    }

    // -------------------------------------------------------------------------
    // Fade Overlay
    // -------------------------------------------------------------------------

    private void CreateFadeOverlay()
    {
        // Canvas ---------------------------------------------------------------
        GameObject canvasGO = new GameObject("TransitionFadeCanvas");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 999; // always in front of everything
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Full-screen image ----------------------------------------------------
        GameObject imgGO = new GameObject("FadeImage");
        imgGO.transform.SetParent(canvasGO.transform, false);

        _fadeImage       = imgGO.AddComponent<Image>();
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f); // start transparent

        RectTransform rect = imgGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t     = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, alpha);
            yield return null;
        }

        // Snap to the exact final value.
        _fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, toAlpha);
    }
}
