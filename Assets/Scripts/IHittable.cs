using UnityEngine;

public interface IHittable
{
    void TakeDamage(float damage, Vector3 knockbackDir);
}
