using UnityEngine;

public abstract class Collectable : MonoBehaviour
{

    [SerializeField] private ItemData firstPickupItem;
    [SerializeField] private string firstPickupFlagID;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        bool isFirstTime =
            firstPickupItem != null &&
            !string.IsNullOrEmpty(firstPickupFlagID) &&
            WorldStateManager.Instance != null &&
            !WorldStateManager.Instance.GetGlobalFlag(firstPickupFlagID);

        if (isFirstTime)
        {
            WorldStateManager.Instance.SetGlobalFlag(firstPickupFlagID, true);
            GameEvents.Instance.ItemPickedUp(firstPickupItem);
        }
        else
        {
            OnCollect(other.gameObject);
        }

        Destroy(gameObject);
    }

    protected abstract void OnCollect(GameObject player);

    Vector3 pos;
    
    void Start()
    {
        pos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0, 50 * Time.deltaTime, 0);

        float newY = Mathf.Sin(Time.time * 2f) * 0.25f + pos.y;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}