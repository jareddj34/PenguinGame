using System;
using UnityEngine;
using Yarn.Unity;
using System.Collections;

public enum GameState
{
    Normal,
    Dialogue,
    ReceivingItem
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Tooltip("The scene's Yarn Spinner DialogueRunner.")]
    [SerializeField] private DialogueRunner dialogueRunner;

    public GameState CurrentState { get; private set; } = GameState.Normal;
    public static event Action<GameState> OnStateChanged;

    private PlayerMovement playerMovement;
    private PlayerAttack   playerAttack;
    public Transform PlayerTransform => playerMovement != null ? playerMovement.transform : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerAttack   = FindFirstObjectByType<PlayerAttack>();

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;
        CurrentState = newState;
        ApplyState(newState);
        OnStateChanged?.Invoke(newState);
    }

    public void EnterDialogue()      => SetState(GameState.Dialogue);
    public void ExitDialogue()       => SetState(GameState.Normal);

    public void FacePlayer(Transform target)
    {
        StartCoroutine(FacePlayerCoroutine(target));
    }
    /// <summary>Rotate the player to face a target (e.g. an NPC).</summary>
    public IEnumerator FacePlayerCoroutine(Transform target)
    {
        if (playerMovement == null) yield break;

        Vector3 direction = target.position - playerMovement.transform.position;
        direction.y = 0f; // keep it flat — no tilting up/down

        if (direction.sqrMagnitude < 0.001f) yield break;

        Quaternion startRotation = playerMovement.transform.rotation;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float elapsed = 0f;
        const float duration = .2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            playerMovement.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        playerMovement.transform.rotation = targetRotation;
    }

    public void EnterReceivingItem()
    {
        playerMovement?.TriggerItemGot(); // trigger BEFORE disabling
        SetState(GameState.ReceivingItem);
    }

    public void ExitReceivingItem()  => SetState(GameState.Normal);

    private void ApplyState(GameState state)
    {
        switch (state)
        {
            case GameState.Normal:
                if (playerMovement != null) playerMovement.enabled = true;
                if (playerAttack   != null) playerAttack.enabled   = true;
                break;
            case GameState.Dialogue:
                if (playerMovement != null) playerMovement.enabled = false;
                if (playerAttack   != null) playerAttack.enabled   = false;
                break;
            case GameState.ReceivingItem:
                // PlayerMovement stays enabled so animation events can still fire
                if (playerMovement != null) playerMovement.enabled = true;
                if (playerAttack   != null) playerAttack.enabled   = false;
                break;
        }
    }

    private void OnDialogueComplete()
    {
        if (CurrentState == GameState.Dialogue)
            ExitDialogue();
    }
}