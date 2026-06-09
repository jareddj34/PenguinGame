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

    // public void SpawnSmokeEffect()
    // {
    //     EnemyHealth enemyHealth = this.GetComponentInParent<EnemyHealth>();
    //     if(enemyHealth != null)
    //     {
    //         enemyHealth.SpawnSmokeEffect();
    //     }

    //     OctopusHealth octoHealth = this.GetComponentInParent<OctopusHealth>();
    //     if(octoHealth != null)
    //     {
    //         octoHealth.SpawnSmokeEffect();
    //     }
    // }
}
