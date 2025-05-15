using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance { get; private set; }

    [Header("Lane Sprites")]
    public Sprite centerLaneSprite; // Gray road with white dashed lines
    public Sprite leftSideSprite;   // Green left side
    public Sprite rightSideSprite;  // Green right side

    [Header("Lane Settings")]
    public int totalLanes = 7;
    public float laneWidth = 60f;
    public float sideLaneWidth = 69f;

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
        // Remove any existing lane objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Configure camera and create lanes
        ConfigureCamera();
        CreateLanes();
    }

    void ConfigureCamera()
    {
        // Ensure orthographic camera
        if (!mainCamera.orthographic)
        {
            mainCamera.orthographic = true;
        }

        // Calculate total lane width
        float totalWidth = (totalLanes - 2) * laneWidth + 2 * sideLaneWidth;

        // Calculate camera size based on lane width and aspect ratio
        float screenAspect = (float)Screen.width / Screen.height;
        float sizeForWidth = totalWidth / (2f * screenAspect);

        // Apply vertical percentage adjustment
        mainCamera.orthographicSize = sizeForWidth / verticalViewportPercentage;

        // Position camera to center lanes at bottom portion
        mainCamera.transform.position = new Vector3(
            0,
            mainCamera.orthographicSize * (1 - verticalViewportPercentage),
            -10
        );
    }

    void CreateLanes()
    {
        lanes.Clear();

        // Calculate total width
        float totalWidth = (totalLanes - 2) * laneWidth + 2 * sideLaneWidth;

        // Calculate screen dimensions
        float screenHeight = 2f * mainCamera.orthographicSize;
        float viewportHeight = screenHeight * verticalViewportPercentage;

        // Calculate segment height
        float segmentHeight = viewportHeight / segmentsPerLane;

        // Calculate starting X position (leftmost point)
        float startX = -totalWidth / 2f;
        float posX = startX;

        // Create each lane
        for (int i = 0; i < totalLanes; i++)
        {
            // Set lane properties based on position
            float width;
            Sprite sprite;

            if (i == 0) // Left side green lane
            {
                width = sideLaneWidth;
                sprite = leftSideSprite;
            }
            else if (i == totalLanes - 1) // Right side green lane
            {
                width = sideLaneWidth;
                sprite = rightSideSprite;
            }
            else // Center road lanes (1-5)
            {
                width = laneWidth;
                sprite = centerLaneSprite;
            }

            // Create lane parent object
            GameObject lane = new GameObject($"Lane_{i}");
            lane.transform.parent = transform;
            lanes.Add(lane);

            // Calculate lane X position
            float laneX;
            if (i == 0) // Left side
            {
                laneX = startX + width / 2;
            }
            else // Other lanes
            {
                laneX = startX + sideLaneWidth + (i - 1) * laneWidth;
                if (i == totalLanes - 1) // Right side
                {
                    laneX = startX + totalWidth - sideLaneWidth / 2;
                }
                else // Center lanes
                {
                    laneX += width / 2;
                }
            }

            lane.transform.position = new Vector3(laneX, 0, 0);

            // Create segments for this lane (stacked vertically)
            for (int j = 0; j < segmentsPerLane * 2; j++) // Double for smooth scrolling
            {
                GameObject segment = new GameObject($"Segment_{j}");
                segment.transform.parent = lane.transform;

                // Add sprite renderer
                SpriteRenderer renderer = segment.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;

                // Set proper sorting order (side lanes behind center lanes)
                renderer.sortingOrder = (i == 0 || i == totalLanes - 1) ? 0 : 1;

                // Scale sprite to match lane width and segment height
                if (sprite != null)
                {
                    float spriteWidth = sprite.bounds.size.x;
                    float spriteHeight = sprite.bounds.size.y;

                    // Calculate scale to match width and height
                    float scaleX = width / spriteWidth;
                    float scaleY = segmentHeight / spriteHeight;

                    segment.transform.localScale = new Vector3(scaleX, scaleY, 1);
                }

                // Position segment vertically with spacing
                float yPos = (j - segmentsPerLane) * segmentHeight;
                segment.transform.localPosition = new Vector3(0, yPos, 0);
            }

            // Add scrolling behavior
            SegmentScroller scroller = lane.AddComponent<SegmentScroller>();
            scroller.scrollSpeed = scrollSpeed;
            scroller.segmentHeight = segmentHeight;

            // Move to next lane position
            posX += width;
        }
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
        return (laneIndex == 0 || laneIndex == totalLanes - 1) ? sideLaneWidth : laneWidth;
    }
}