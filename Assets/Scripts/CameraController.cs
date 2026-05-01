using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("The virtual camera to activate during dialogue. Should start disabled.")]
    [SerializeField] private CinemachineCamera dialogueCamera;
    [SerializeField] private CinemachineCamera itemGotCamera;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void OnEnable()
    {
        GameStateManager.OnStateChanged += HandleStateChanged;
    }

    private void OnDisable()
    {
        GameStateManager.OnStateChanged -= HandleStateChanged;
    }

    // -------------------------------------------------------------------------
    // State Handling
    // -------------------------------------------------------------------------

    private void HandleStateChanged(GameState newState)
    {
        if (dialogueCamera == null) return;

        // Enable the dialogue cam when talking; disable it for everything else.
        // CinemachineBrain blends to/from it automatically.
        if(newState == GameState.Dialogue) {
            dialogueCamera.gameObject.SetActive(true);
            itemGotCamera.gameObject.SetActive(false);
        }
        else if (newState == GameState.ReceivingItem) {
            dialogueCamera.gameObject.SetActive(false);
            itemGotCamera.gameObject.SetActive(true);
        }
        else {
            dialogueCamera.gameObject.SetActive(false);
            itemGotCamera.gameObject.SetActive(false);
        }
    }
}