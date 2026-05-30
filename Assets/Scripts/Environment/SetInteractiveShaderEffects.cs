using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetInteractiveShaderEffects : MonoBehaviour
{
    [SerializeField]
    RenderTexture rt;
    [SerializeField]
    Transform target;

    [SerializeField]
    float yOffset = 20f;

    [SerializeField]
    Camera m_cam;
    // Start is called before the first frame update
    void Awake()
    {
        if (m_cam == null)
        {
            m_cam = GetComponent<Camera>();
        }
        Shader.SetGlobalTexture("_GlobalEffectRT", rt);
        Shader.SetGlobalFloat("_OrthographicCamSize", m_cam.orthographicSize);
    }

    void Start()
    {
        if(target == null)
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
                target = player.transform;
             else
                Debug.LogError("SetInteractiveShaderEffects: No target assigned and no PlayerMovement found in scene.");
        }
    }

    private void Update()
    {
        // more stable version thanks to NoveLL
         // calculate render texture pixel size in world space
        float pixelSize = 2.0f * m_cam.orthographicSize / rt.height;

        if(target == null) return;
        // round the camera's position to the nearest pixel
        Vector3 targetPosition = new Vector3(
            Mathf.Round(target.position.x / pixelSize) * pixelSize, 
            Mathf.Round((target.position.y + yOffset) / pixelSize) * pixelSize,
            Mathf.Round(target.position.z / pixelSize) * pixelSize);

        transform.position = targetPosition;
        Shader.SetGlobalVector("_Position", transform.position);
    }


}