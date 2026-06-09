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
        if(other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.EnterIce();
            
            PlayerSound ps = other.GetComponent<PlayerSound>();
            if (ps != null)
                ps.StartIceSlide();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
                pm.ExitIce();

            PlayerSound ps = other.GetComponent<PlayerSound>();
            if (ps != null)
                ps.StopIceSlide();
        }
    }
}
