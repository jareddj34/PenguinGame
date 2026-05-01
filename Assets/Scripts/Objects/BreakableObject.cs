using UnityEngine;

public class BreakableObject : MonoBehaviour, IHittable
{

    public int hitsToBreak = 2;
    public GameObject breakEffectPrefab;

    private int currentHits = 0;

    public void TakeDamage(float damage, Vector3 knockbackDir)
    {
        currentHits++;

        if (currentHits >= hitsToBreak)
        {
            Break();
        }
    }

    public void Break()
    {
        if (breakEffectPrefab != null)
        {
            Vector3 rotation = Quaternion.Euler(-90f, 0, 0f).eulerAngles;
            GameObject effect = Instantiate(breakEffectPrefab, transform.position + Vector3.up * 0.2f, Quaternion.Euler(rotation));
            Destroy(effect, 2f); // Destroy the effect after 2 seconds
        }

        Destroy(gameObject);
    }

}
