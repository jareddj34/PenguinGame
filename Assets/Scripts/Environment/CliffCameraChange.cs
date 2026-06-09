using UnityEngine;

public class CliffCameraChange : MonoBehaviour
{

    public CameraController camController;

    public bool doNoTrackingVersion = false;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (doNoTrackingVersion)
            camController.ChangeToCliffCameraNoTracking();
        else
            camController.ChangeToCliffCamera();

    }

    void OnTriggerExit(Collider other)
    {
        if(!other.CompareTag("Player")) return;

        if (doNoTrackingVersion)
            camController.ChangeFromCliffCameraNoTracking();
        else
            camController.ChangeFromCliffCamera();
    }
}
