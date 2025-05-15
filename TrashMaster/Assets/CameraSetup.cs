using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // Ensure camera is orthographic
            mainCamera.orthographic = true;

            // Set initial size - will be adjusted by LaneManager
            mainCamera.orthographicSize = 5f;

            // Center camera on lanes
            transform.position = new Vector3(0, 0, -10);
        }
    }
}