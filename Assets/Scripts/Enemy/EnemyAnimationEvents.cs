using UnityEngine;

public class EnemyAnimationEvents : MonoBehaviour
{
    public EnemyAI enemyAI;
    public OctopusAI octoAI;

    public void EnableHitbox()
    {
        enemyAI.ActivateHitbox();
    }

    public void DisableHitbox()
    {
        enemyAI.DeactivateHitbox();
    }

    public void ShootProjectile()
    {
        if(octoAI != null)
        {
            octoAI.ShootProjectile();
        }
    }
}
