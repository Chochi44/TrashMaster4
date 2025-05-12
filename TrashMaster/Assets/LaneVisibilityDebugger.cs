using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Diagnoses lane visibility issues by adding colored debug visuals
/// </summary>
public class LaneVisibilityDebugger : MonoBehaviour
{
    [Header("Debug Options")]
    public bool addColoredOverlays = true;
    public bool adjustCameraView = true;
    public bool createBoundaryMarkers = true;

    [Header("Settings")]
    public Camera gameCamera;
    public float orthographicSize = 4.5f; // Slightly larger to see all lanes

    private Color[] debugColors = new Color[] {
        Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white
    };

    void Start()
    {
        Debug.Log("LaneVisibilityDebugger starting...");

        // Find camera if not assigned
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            Debug.Log("Main camera auto-assigned: " + (gameCamera != null));
        }

        if (adjustCameraView && gameCamera != null)
        {
            // Adjust camera settings
            gameCamera.orthographicSize = orthographicSize;
            gameCamera.transform.position = new Vector3(0, 0, -10);
            gameCamera.backgroundColor = Color.black; // Dark background to see sprites better

            Debug.Log("Camera adjusted: Size=" + orthographicSize + ", Position=" + gameCamera.transform.position);
        }

        if (addColoredOverlays)
        {
            AddColoredOverlaysToLanes();
        }

        if (createBoundaryMarkers)
        {
            CreateBoundaryMarkers();
        }

        // Log the camera's viewport information
        if (gameCamera != null)
        {
            float cameraWidth = gameCamera.orthographicSize * 2 * gameCamera.aspect;
            Debug.Log("Camera view width: " + cameraWidth + ", Left edge: " + (-cameraWidth / 2) + ", Right edge: " + (cameraWidth / 2));
        }
    }

    // Add colored overlay to each lane for better visibility
    private void AddColoredOverlaysToLanes()
    {
        // Find all lanes in the scene
        GameObject[] lanes = GameObject.FindGameObjectsWithTag("Lane");

        if (lanes.Length == 0)
        {
            // If no lanes are tagged, try finding by name
            List<GameObject> foundLanes = new List<GameObject>();

            // Look for objects with "Lane" in their name
            foreach (GameObject obj in FindObjectsOfType<GameObject>())
            {
                if (obj.name.Contains("Lane"))
                {
                    foundLanes.Add(obj);
                }
            }

            lanes = foundLanes.ToArray();
            Debug.Log("Found " + lanes.Length + " lanes by name search");
        }

        // Process found lanes
        for (int i = 0; i < lanes.Length; i++)
        {
            GameObject lane = lanes[i];
            Debug.Log("Adding overlay to lane: " + lane.name + " at position " + lane.transform.position);

            // Create a child object for the colored overlay
            GameObject overlay = new GameObject("DebugOverlay");
            overlay.transform.SetParent(lane.transform);
            overlay.transform.localPosition = Vector3.zero;

            // Add a sprite renderer with a colored square
            SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateColoredSprite(32, 32, debugColors[i % debugColors.Length]);

            // Set sorting order to be in front of the lane
            SpriteRenderer laneRenderer = lane.GetComponent<SpriteRenderer>();
            if (laneRenderer != null)
            {
                renderer.sortingOrder = laneRenderer.sortingOrder + 1;
            }

            // Set the scale to match lane height but narrower width
            renderer.transform.localScale = new Vector3(0.5f, 20f, 1f);

            // Ensure it's visible by setting alpha
            Color color = renderer.color;
            color.a = 0.7f;
            renderer.color = color;
        }
    }

    // Create boundary markers at camera edges
    private void CreateBoundaryMarkers()
    {
        if (gameCamera == null) return;

        float cameraWidth = gameCamera.orthographicSize * 2 * gameCamera.aspect;
        float leftEdge = -cameraWidth / 2;
        float rightEdge = cameraWidth / 2;

        CreateMarker("LeftBoundary", leftEdge, Color.red);
        CreateMarker("RightBoundary", rightEdge, Color.red);
        CreateMarker("CenterMarker", 0, Color.white);
    }

    private void CreateMarker(string name, float xPosition, Color color)
    {
        GameObject marker = new GameObject(name);
        marker.transform.position = new Vector3(xPosition, 0, 0);

        // Add a sprite renderer
        SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateColoredSprite(8, 32, color);
        renderer.sortingOrder = 100; // Ensure it's in front

        // Make it tall
        marker.transform.localScale = new Vector3(1, 20, 1);

        Debug.Log("Created boundary marker at X=" + xPosition);
    }

    // Utility to create a colored sprite at runtime
    private Sprite CreateColoredSprite(int width, int height, Color color)
    {
        Texture2D texture = new Texture2D(width, height);
        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}