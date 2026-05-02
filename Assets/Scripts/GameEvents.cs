using System;
using UnityEngine;
using Yarn.Unity;

public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }

    public event Action<string> OnPromptShow;
    public event Action OnPromptHide;
    // public event Action OnSwordPickedUp;
    public event Action<ItemData> OnItemPickedUp;

    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private PlayerHealth playerHealth;
    private PlayerShield playerShield;
    private DialogueRunner dialogueRunner;
    public GameObject dialogueUI;

    private ItemPopup itemPopup;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        // DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();
        playerAttack = FindFirstObjectByType<PlayerAttack>();
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        playerShield = FindFirstObjectByType<PlayerShield>();
        itemPopup = FindFirstObjectByType<ItemPopup>();

        dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        dialogueRunner.AddCommandHandler("GiveSword", GiveSword);
        dialogueRunner.AddCommandHandler("GiveShield", GiveShield);
        dialogueRunner.AddCommandHandler("FirstHealthPickup", FirstHealthPickup);
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);

    }

    private void OnDestroy()
    {
        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
    }

    public void ItemPickedUp(ItemData item)
    {
        GameStateManager.Instance.EnterReceivingItem();
        OnItemPickedUp?.Invoke(item);
        dialogueRunner.StartDialogue(item.yarnNodeName);
    }

    public void EndItemGot()
    {
        playerMovement.EndItemGot();
        itemPopup.Hide();
    }

    public void ShowInteractionPrompt(string prompt)
    {
        // UI script will handle this
        OnPromptShow?.Invoke(prompt);
    }

    public void HideInteractionPrompt()
    {
        OnPromptHide?.Invoke();
    }

    public void GiveSword()
    {
        playerAttack.GotSword();

    }

    public void GiveShield()
    {
        playerShield.GotShield();
    }

    public void FirstHealthPickup()
    {
        playerHealth.Heal(10);
    }

    private void OnDialogueComplete()
    {

    }

}