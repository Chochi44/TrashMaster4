using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance { get; private set; }

    [Header("Lane Sprites")]
    public Sprite centerLaneSprite;
    public Sprite leftSideSprite;
    public Sprite rightSideSprite;

    [Header("Lane Configuration")]
    public int centerLaneCount = 5; // Number of center lanes
    [Range(0.0f, 0.3f)]
    public float sideLaneWidthRatio = 0.15f; // Side lanes take 15% of screen width each

    [Header("Camera Settings")]
    public Color backgroundColor = Color.cyan;
    public float verticalViewportPercentage = 0.7f;

    [Header("Scrolling Settings")]
    public int segmentsPerLane = 3;
    public float scrollSpeed = 0.5f;

    private List<GameObject> lanes = new List<GameObject>();
    private Camera mainCamera;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found!");
            return;
        }

        // Set camera background color
        mainCamera.backgroundColor = backgroundColor;
    }

    void Start()
    {
        // Clear any existing lanes
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Create lanes with direct screen-based sizing
        CreateLanesForScreen();
    }

    void CreateLanesForScreen()
    {
        lanes.Clear();

        // Get actual screen width in world units
        float screenWidth = 2 * mainCamera.orthographicSize * mainCamera.aspect;
        float screenHeight = 2 * mainCamera.orthographicSize;

        // Calculate lane widths based on screen width
        float sideLaneWidth = screenWidth * sideLaneWidthRatio;
        float centerLanesTotalWidth = screenWidth - (2 * sideLaneWidth);
        float centerLaneWidth = centerLanesTotalWidth / centerLaneCount;

        // Calculate total number of lanes
        int totalLanes = centerLaneCount + 2; // center lanes + 2 side lanes

        // Calculate segment height based on viewport percentage
        float viewportHeight = screenHeight * verticalViewportPercentage;
        float segmentHeight = viewportHeight / segmentsPerLane;

        // Starting X position (left edge of screen)
        float startX = -screenWidth / 2;

        Debug.Log($"Screen size: {screenWidth}x{screenHeight}");
        Debug.Log($"Lane widths - side: {sideLaneWidth}, center: {centerLaneWidth}");

        // Create all lanes to fill screen width exactly
        for (int i = 0; i < totalLanes; i++)
        {
            // Determine lane properties
            float width;
            Sprite sprite;

            if (i == 0) // Left side lane
            {
                width = sideLaneWidth;
                sprite = leftSideSprite;
            }
            else if (i == totalLanes - 1) // Right side lane
            {
                width = sideLaneWidth;
                sprite = rightSideSprite;
            }
            else // Center lanes
            {
                width = centerLaneWidth;
                sprite = centerLaneSprite;
            }

            // Create lane object
            GameObject lane = new GameObject($"Lane_{i}");
            lane.transform.parent = transform;
            lanes.Add(lane);

            // Calculate lane position
            float xPos;
            if (i == 0) // Left lane
            {
                xPos = startX + width / 2;
            }
            else if (i == totalLanes - 1) // Right lane
            {
                xPos = startX + screenWidth - width / 2;
            }
            else // Center lanes
            {
                xPos = startX + sideLaneWidth + (i - 1) * centerLaneWidth + centerLaneWidth / 2;
            }

            lane.transform.position = new Vector3(xPos, 0, 0);

            // Create segments for vertical scrolling
            for (int j = 0; j < segmentsPerLane * 2; j++)
            {
                GameObject segment = new GameObject($"Segment_{j}");
                segment.transform.parent = lane.transform;

                // Add sprite renderer
                SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;

                // Side lanes behind center lanes
                renderer.sortingOrder = (i == 0 || i == totalLanes - 1) ? 0 : 1;

                // Scale sprite to lane width and segment height
                if (sprite != null)
                {
                    float spriteWidth = sprite.bounds.size.x;
                    float spriteHeight = sprite.bounds.size.y;

                    float scaleX = width / spriteWidth;
                    float scaleY = segmentHeight / spriteHeight;

                    segment.transform.localScale = new Vector3(scaleX, scaleY, 1);
                }

                // Position segment vertically
                float yPos = (j - segmentsPerLane) * segmentHeight;
                segment.transform.localPosition = new Vector3(0, yPos, 0);
            }

            // Add scrolling component
            SegmentScroller scroller = lane.AddComponent<SegmentScroller>();
            scroller.scrollSpeed = scrollSpeed;
            scroller.segmentHeight = segmentHeight;

            Debug.Log($"Created lane {i} at x={xPos} with width {width}");
        }
    }

    // Configure camera based on desired vertical percentage
    void ConfigureCamera()
    {
        // Get screen dimensions
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float aspect = screenWidth / screenHeight;

        // Position camera at screen bottom to show lanes
        mainCamera.transform.position = new Vector3(
            0,
            mainCamera.orthographicSize * (1 - verticalViewportPercentage),
            -10
        );
    }

    // Helper method to get lane position for player
    public Vector3 GetLanePosition(int laneIndex)
    {
        if (laneIndex < 0 || laneIndex >= lanes.Count)
        {
            Debug.LogWarning($"Invalid lane index: {laneIndex}");
            return new Vector3(0, -mainCamera.orthographicSize + 1f, 0);
        }

        // Get lane X position
        float xPos = lanes[laneIndex].transform.position.x;

        // Bottom of screen with offset for player
        float yPos = -mainCamera.orthographicSize + 1f;

        return new Vector3(xPos, yPos, 0);
    }

    // Get lane width
    public float GetLaneWidth(int laneIndex)
    {
        // Calculate based on screen width
        float screenWidth = 2 * mainCamera.orthographicSize * mainCamera.aspect;
        float sideLaneWidth = screenWidth * sideLaneWidthRatio;
        float centerLaneWidth = (screenWidth - 2 * sideLaneWidth) / centerLaneCount;

        return (laneIndex == 0 || laneIndex == lanes.Count - 1) ? sideLaneWidth : centerLaneWidth;
    }
}