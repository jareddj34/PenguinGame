using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IceZone : MonoBehaviour
{
    private void Awake()
    {
        // Safety: ensure the collider is always a trigger, even if misconfigured in the Inspector.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.EnterIce();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
            pm.ExitIce();
    }
}
