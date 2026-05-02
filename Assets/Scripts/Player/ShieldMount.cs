using UnityEngine;

/// <summary>
/// Smoothly moves the shield model between two anchor positions depending on
/// whether the shield is raised or at rest.
///
/// Setup:
///   1. Under the arm bone in the armature, create two empty GameObjects:
///        - ShieldAnchor_Up   (position/rotate this to where the shield sits when raised)
///        - ShieldAnchor_Rest (position/rotate this to where the shield sits when not in use)
///   2. Add this component to the shield model GameObject (which should also be a child of the arm bone).
///   3. Assign the three references in the Inspector.
/// </summary>
public class ShieldMount : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The PlayerShield script on the player root — used to read IsShielding.")]
    [SerializeField] private PlayerShield playerShield;

    [Tooltip("Empty GameObject parented to the arm bone at the shield-raised position.")]
    [SerializeField] private Transform shieldUpAnchor;

    [Tooltip("Empty GameObject parented to the arm bone at the resting/carry position.")]
    [SerializeField] private Transform shieldRestAnchor;

    [Header("Settings")]
    [Tooltip("How quickly the shield slides between positions. Higher = snappier.")]
    [SerializeField] private float transitionSpeed = 12f;

    // -------------------------------------------------------------------------
    // Unity Lifecycle
    // -------------------------------------------------------------------------

    private void Update()
    {
        if (playerShield == null || shieldUpAnchor == null || shieldRestAnchor == null)
            return;

        Transform target = playerShield.IsShielding ? shieldUpAnchor : shieldRestAnchor;

        // Lerp local position and rotation toward the active anchor.
        // Both anchors are siblings of this object under the same arm bone,
        // so their localPosition/localRotation are in the same space as ours.
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            target.localPosition,
            transitionSpeed * Time.deltaTime
        );

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            target.localRotation,
            transitionSpeed * Time.deltaTime
        );
    }
}
