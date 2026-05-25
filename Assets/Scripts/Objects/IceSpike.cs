using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class IceSpike : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 6f;

    [Header("Pushing")]
    [SerializeField] private float pushSpeed = 5f;
    [SerializeField] private LayerMask wallLayers;

    private BoxCollider boxCol;

    private void Awake()
    {
        boxCol = GetComponent<BoxCollider>();
    }

    // Called every frame the shielded player is in contact
    public void PushThisFrame(Vector3 direction)
    {
        float step = pushSpeed * Time.deltaTime;
        Vector3 halfExtents = Vector3.Scale(boxCol.size * 0.5f, transform.lossyScale);

        // Don't move if a wall is in the way
        if (Physics.BoxCast(transform.position, halfExtents, direction,
                out _, transform.rotation, step + 0.05f, wallLayers))
            return;

        transform.position += direction * step;
    }

    public void DamagePlayer(PlayerMovement player)
    {
        player.GetComponent<PlayerHealth>()?.TakeDamage(damage);

        Vector3 toPlayer = (player.transform.position - transform.position);
        toPlayer.y = 0f;
        HitDirection dir = GetHitDirectionRelativeTo(player.transform, toPlayer.normalized);
        player.StartKnockback(dir, knockbackForce);
    }

    private HitDirection GetHitDirectionRelativeTo(Transform playerTransform, Vector3 fromSpikeToPlayer)
    {
        float fwd   = Vector3.Dot(playerTransform.forward, fromSpikeToPlayer);
        float right = Vector3.Dot(playerTransform.right,   fromSpikeToPlayer);

        if (Mathf.Abs(fwd) >= Mathf.Abs(right))
            return fwd > 0f ? HitDirection.Back : HitDirection.Front;
        else
            return right > 0f ? HitDirection.Left : HitDirection.Right;
    }
}