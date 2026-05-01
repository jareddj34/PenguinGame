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
        itemPopup = FindFirstObjectByType<ItemPopup>();

        dialogueRunner = FindFirstObjectByType<DialogueRunner>();
        dialogueRunner.AddCommandHandler("EndSwordDialogue", EndSwordDialogue);
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

    public void EndSwordDialogue()
    {
        playerAttack.GotSword();

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

    private bool gotFirstHealthPickup = false;
    public void FirstHealthPickup()
    {
        if (gotFirstHealthPickup) return;
        gotFirstHealthPickup = true;
        Debug.Log("First health pickup acquired. Player will be healed after dialogue.");
    }

    private void OnDialogueComplete()
    {
        if(gotFirstHealthPickup)
        {
            playerHealth.Heal(1);
            gotFirstHealthPickup = false;
        }

    }

}