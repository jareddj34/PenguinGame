using UnityEngine;

public class CliffCameraChange : MonoBehaviour
{

    public CameraController camController;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        camController.ChangeToCliffCamera();

    }

    void OnTriggerExit(Collider other)
    {
        if(!other.CompareTag("Player")) return;

        camController.ChangeFromCliffCamera();
    }
}
