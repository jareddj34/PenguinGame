using UnityEngine;
using System.Collections;

public class Chest : InteractableBase
{

    public ItemData itemData;
    public override string InteractionPrompt => "Open [E]";

    private Animator animator;
    private bool opened = false;

    [SerializeField] private StatefulObject stateful;

    private void Start()
    {
        animator = GetComponent<Animator>();

        // If we've been to this scene before and already opened this chest,
        // snap it straight to the open visual without running the interaction flow.
        if (stateful != null && stateful.IsDone)
        {
            opened = true;
            animator.Play("OpenIdle", 0, 1f); // jump to the last frame of the Open animation
            enabled = false;
        }
    }

    public override void Interact()
    {
        if(!opened && IsPlayerInFront())
        {
            opened = true;

            animator = GetComponent<Animator>();
            animator.SetTrigger("Open");

            FreezePlayer();

            base.OnStopHover();

        }
    }

    public void FreezePlayer()
    {
        GameEvents.Instance.FreezePlayer();
    }

    public void OnOpened()
    {
        stateful?.MarkDone(); // save "this chest is open" to WorldStateManager
        GameEvents.Instance.ItemPickedUp(itemData);
        GameEvents.Instance.UnfreezePlayer();
        GetComponent<Chest>().enabled = false;

    }

    public override void OnHover()
    {
        if(!opened && IsPlayerInFront()) {
            GameEvents.Instance.ShowInteractionPrompt(InteractionPrompt);
        }
    }

    private bool IsPlayerInFront()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return false;

        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, dirToPlayer);

        // dot > 0.5 means within roughly a 60-degree cone in front
        return dot > 0.5f;
    }
}
