using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Tooltip("The virtual camera to activate during dialogue. Should start disabled.")]
    [SerializeField] private CinemachineCamera dialogueCamera;
    [SerializeField] private CinemachineCamera itemGotCamera;
    public CinemachineCamera cliffCamera;
    public CinemachineCamera cliffCamerNoTracking;

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

    private bool _suppressNextDialogueCamera;

    /// <summary>
    /// Call before EnterDialogue() to skip the dialogue camera for that one
    /// conversation (e.g. blocker NPCs that are far from the player).
    /// </summary>
    public void SuppressNextDialogueCamera() => _suppressNextDialogueCamera = true;

    private void HandleStateChanged(GameState newState)
    {
        if (dialogueCamera == null) return;

        // Enable the dialogue cam when talking; disable it for everything else.
        // CinemachineBrain blends to/from it automatically.
        if(newState == GameState.Dialogue) {
            if (_suppressNextDialogueCamera)
            {
                _suppressNextDialogueCamera = false;
            }
            else
            {
                dialogueCamera.gameObject.SetActive(true);
            }
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

    public void ChangeToCliffCamera()
    {
        cliffCamera.gameObject.SetActive(true);
    }

    public void ChangeFromCliffCamera()
    {
        cliffCamera.gameObject.SetActive(false);
    }

    public void ChangeToCliffCameraNoTracking()
    {
        cliffCamerNoTracking.gameObject.SetActive(true);
    }

    public void ChangeFromCliffCameraNoTracking()
    {
        cliffCamerNoTracking.gameObject.SetActive(false);
    }
}