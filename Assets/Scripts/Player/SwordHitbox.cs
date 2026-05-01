using System.Collections.Generic;
using UnityEngine;

public class SwordHitbox : MonoBehaviour
{
    [SerializeField] private float damage = 10f;

    public GameObject impactEffectPrefab;

    // Tracks which enemies have already been hit during this swing
    // so they can only take damage once per attack, even if they stay
    // inside the collider across multiple frames
    private readonly HashSet<IHittable> hitThisSwing = new HashSet<IHittable>();

    

    private void OnEnable()
    {
        // Clear the hit list every time the hitbox is activated for a new swing
        hitThisSwing.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        IHittable hittable = other.GetComponent<IHittable>();
        if (hittable == null)
            return;

        // Only damage each enemy once per swing
        if (hitThisSwing.Contains(hittable))
            return;

        hitThisSwing.Add(hittable);

        // Spawn impact effect at the collision point
        if (impactEffectPrefab != null)
        {
            Vector3 impactPoint = other.ClosestPoint(transform.position);
            GameObject impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
            Destroy(impactEffect, 1f); // Clean up the effect after 1 second
        }

        // Knockback direction is from the hitbox outward toward the enemy
        Vector3 knockbackDir = (other.transform.position - transform.parent.position).normalized;
        knockbackDir.y = 0f; // Keep knockback flat on the ground plane

        hittable.TakeDamage(damage, knockbackDir);
    }
}
