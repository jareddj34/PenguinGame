using UnityEngine;

public class PlayerAnimationEvents : MonoBehaviour
{

    public PlayerMovement playerMovement;
    public PlayerAttack playerAttack;
    public GameObject swordHitbox;

    public void OnAttackAnimationComplete()
    {
        // This should be called as an Animation Event at the end of the attack animation
        playerMovement.isAttacking = false;
        swordHitbox.SetActive(false);
    }

    public void OnHitboxEnable()
    {
        // This should be called as an Animation Event at the moment the sword swing should be active
        if (swordHitbox != null)
            swordHitbox.SetActive(true);
    }

    public void SlashEffect()
    {
        playerAttack.SpawnSlash();
    }


}
