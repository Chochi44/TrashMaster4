using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance { get; private set; }

    [Header("Lane Sprites")]
    public Sprite centerLaneSprite;
    public Sprite leftSideLaneSprite;
    public Sprite rightSideLaneSprite;

    [Header("Lane Settings")]
    public int totalLanes = 7;
    public float laneWidth = 60f;
    public float sideLaneWidth = 69f;

    [Header("Display Settings")]
    public Color backgroundColor = new Color(0.5f, 0.8f, 0.9f); // Light blue sky color
    public bool fillScreen = true;
    public float laneHeightMultiplier = 2.0f; // How much taller than screen height

    private List<GameObject> lanes = new List<GameObject>();
    private Camera mainCamera;
    private float totalWidth;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
    }

    void Start()
    {
        // Set camera background color
        if (mainCamera != null)
        {
            mainCamera.backgroundColor = backgroundColor;
        }

        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return null; // Wait for GameManager to initialize
        CreateLanes();
        AdjustCamera();
    }

    void CreateLanes()
    {
        // Clear existing lanes
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        lanes.Clear();

        // Calculate total width
        totalWidth = (totalLanes - 2) * laneWidth + 2 * sideLaneWidth;

        // Calculate screen dimensions
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // Position lanes to fill screen width
        float startX = -totalWidth / 2f;

        Debug.Log($"Creating {totalLanes} lanes with total width {totalWidth}, screen width {screenWidth}");

        // Create each lane
        for (int i = 0; i < totalLanes; i++)
        {
            GameObject lane = new GameObject($"Lane_{i}");
            lane.transform.parent = transform;

            SpriteRenderer renderer = lane.AddComponent<SpriteRenderer>();

            // Set width and sprite based on lane type
            float width;
            Sprite sprite;

            if (i == 0) // Left side
            {
                width = sideLaneWidth;
                sprite = leftSideLaneSprite;
                renderer.sortingOrder = 0; // Behind center lanes
            }
            else if (i == totalLanes - 1) // Right side
            {
                width = sideLaneWidth;
                sprite = rightSideLaneSprite;
                renderer.sortingOrder = 0; // Behind center lanes
            }
            else // Center lanes
            {
                width = laneWidth;
                sprite = centerLaneSprite;
                renderer.sortingOrder = 1; // In front of side lanes
            }

            renderer.sprite = sprite;

            // Create material that supports tiling
            Material material = new Material(Shader.Find("Sprites/Default"));
            if (sprite != null && sprite.texture != null)
            {
                material.mainTexture = sprite.texture;
                sprite.texture.wrapMode = TextureWrapMode.Repeat;
            }
            renderer.material = material;

            // Set up tiling
            renderer.drawMode = SpriteDrawMode.Tiled;

            // Position the lane
            float xPos = startX + (i == 0 ? width / 2 :
                         i == totalLanes - 1 ? totalWidth - sideLaneWidth / 2 :
                         sideLaneWidth + (i - 1) * laneWidth + laneWidth / 2);

            lane.transform.position = new Vector3(xPos, 0, 0);

            // Make lane tall enough to fill screen height and allow for scrolling
            float height = screenHeight * laneHeightMultiplier;
            renderer.size = new Vector2(width, height);

            // Add scrolling component
            LaneScroller scroller = lane.AddComponent<LaneScroller>();

            // Add to tracking list
            lanes.Add(lane);

            Debug.Log($"Created lane {i} at x={xPos}, width={width}, height={height}");
        }

        Debug.Log($"Total {lanes.Count} lanes created");
    }

    // Adjust camera to better fit the gameplay area
    void AdjustCamera()
    {
        if (mainCamera == null)
        {
            Debug.LogError("Main camera not found");
            return;
        }

        // Ensure orthographic camera
        if (!mainCamera.orthographic)
        {
            mainCamera.orthographic = true;
        }

        if (fillScreen)
        {
            // Set camera size to fit lane width
            float screenAspect = (float)Screen.width / Screen.height;
            float desiredSize = totalWidth / (2f * screenAspect);

            // Set camera size
            mainCamera.orthographicSize = desiredSize;

            Debug.Log($"Camera adjusted to size {desiredSize} to fit total width {totalWidth}");
        }

        // Position camera to look at the center of lanes
        mainCamera.transform.position = new Vector3(0, 0, -10);
    }

    // Get lane position for player positioning
    public Vector3 GetLanePosition(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= lanes.Count)
        {
            Debug.LogWarning($"Invalid lane index: {laneIndex}");
            return new Vector3(0, -mainCamera.orthographicSize + 1f, 0);
        }

        Vector3 pos = lanes[laneIndex].transform.position;
        pos.y = -mainCamera.orthographicSize + 1f; // Bottom of screen + offset
        return pos;
    }

    // Get lane width
    public float GetLaneWidth(int laneIndex)
    {
        if (laneIndex == 0 || laneIndex == totalLanes - 1)
            return sideLaneWidth;
        return laneWidth;
    }
}