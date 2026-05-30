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

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip pushSound;
    private bool wasPushedThisFrame;

    private BoxCollider boxCol;

    private void Awake()
    {
        boxCol = GetComponent<BoxCollider>();
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = pushSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.Stop();
    }

    // Called every frame the shielded player is in contact
    public void PushThisFrame(Vector3 direction, float distance)
    {
        Vector3 halfExtents = Vector3.Scale(boxCol.size * 0.5f, transform.lossyScale);

        if (Physics.BoxCast(transform.position, halfExtents, direction,
                out _, transform.rotation, distance + 0.05f, wallLayers))
            return;

        transform.position += direction * distance;
        wasPushedThisFrame = true;
    }

    private float lastPushedTime;
    private const float AudioGracePeriod = 0.08f;
    private void LateUpdate()
    {
        if (wasPushedThisFrame)
        {
            lastPushedTime = Time.time;
            if (!audioSource.isPlaying) {
                audioSource.Play();
            }
        }
        else if (Time.time - lastPushedTime > AudioGracePeriod)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }

        wasPushedThisFrame = false;
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