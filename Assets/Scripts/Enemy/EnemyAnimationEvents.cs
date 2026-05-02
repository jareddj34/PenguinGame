using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
   public EnemyAI enemyAI;

    public void EnableHitbox()
    {
        enemyAI.ActivateHitbox();
    }

    public void DisableHitbox()
    {
        enemyAI.DeactivateHitbox();
    }
}
