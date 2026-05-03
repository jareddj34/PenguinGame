using UnityEngine;

public class Snowball : MonoBehaviour
{
    public GameObject particleEffect;

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

        if(other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }

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

        GameObject particle = Instantiate(particleEffect, transform.position, Quaternion.identity);
        Destroy(particle, 3f);
        Destroy(gameObject);
    }
}