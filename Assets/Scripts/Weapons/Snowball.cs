using UnityEngine;

public class Snowball : MonoBehaviour
{
    public GameObject particleEffect;

    /// <summary>
    /// Set by whoever spawns this snowball. The snowball will pass through
    /// (ignore) the owner and all of its children, so it never hits the
    /// character that threw it — whether that's the player or an NPC.
    /// </summary>
    [HideInInspector] public GameObject owner;

    [Header("Settings")]
    public float speed = 12f;
    public float maxDistance = 8f;
    public int damage = 1;
    public float knockbackForce = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);

        if (Vector3.Distance(startPosition, transform.position) >= maxDistance) {
            GameObject particle = Instantiate(particleEffect, transform.position, Quaternion.identity);
            Destroy(particle, 3f);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignore the object that threw this snowball (and any of its children,
        // e.g. a hitbox that lives on a child transform).
        if (owner != null && other.transform.IsChildOf(owner.transform) || other.gameObject.layer == LayerMask.NameToLayer("Ignore Raycast") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
            return;

        // Use your existing IHittable interface so enemies react the same way
        var hittable = other.GetComponent<IHittable>();
        if (hittable != null)
        {
            Debug.Log("hit");
            // Knockback direction is from the hitbox outward toward the enemy
            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            knockbackDir.y = 0f; // Keep knockback flat on the ground plane
            hittable.TakeDamage(damage, knockbackDir, knockbackForce);
        }

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            // Knockback direction is from the hitbox outward toward the player
            Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
            knockbackDir.y = 0f; // Keep knockback flat on the ground plane
            playerHealth.TakeHit(transform.position, damage, knockbackForce);
        }

        GameObject particle = Instantiate(particleEffect, transform.position, Quaternion.identity);
        Destroy(particle, 3f);
        Destroy(gameObject);
    }
}