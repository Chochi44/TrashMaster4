using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    public static LaneManager Instance { get; private set; }

    // Event system for lane changes
    public delegate void LaneConfigurationChanged(int centerLaneCount);
    public static event LaneConfigurationChanged OnLaneConfigurationChanged;

    [Header("Lane Sprites")]
    public Sprite centerLaneSprite;
    public Sprite leftSideSprite;
    public Sprite rightSideSprite;

    [Header("Lane Configuration")]
    public int baseCenterLaneCount = 5; // Starting number of center lanes (at level 1)
    public int maxCenterLaneCount = 9;  // Maximum center lanes at highest level
    [Range(0.0f, 0.3f)]
    public float sideLaneWidthRatio = 0.15f; // Side lanes take 15% of screen width each

    [Header("Level Scaling")]
    public int lanesIncreaseEveryNLevels = 2; // Add lane every N levels
    public int additionalLanesPerStep = 1;    // How many lanes to add each step

    [Header("Camera Settings")]
    public Color backgroundColor = Color.cyan;
    public float verticalViewportPercentage = 0.7f;

    [Header("Scrolling Settings")]
    public int segmentsPerLane = 3;
    public float baseScrollSpeed = 0.5f;      // Starting scroll speed at level 1
    public float scrollSpeedMultiplier = 1.0f; // Additional multiplier if needed

    private List<GameObject> lanes = new List<GameObject>();
    private Camera mainCamera;
    private int currentLevel = 1;
    private int currentCenterLaneCount;
    private float currentScrollSpeed;

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

        // Initialize scrolling speed
        currentScrollSpeed = baseScrollSpeed;
    }

    void Start()
    {
        // Set initial lane count based on level 1
        currentCenterLaneCount = baseCenterLaneCount;

        // Check if GameManager exists and get current level and speed
        if (GameManager.Instance != null)
        {
            currentLevel = GameManager.Instance.currentLevel;
            UpdateLaneCountForLevel(currentLevel);
            UpdateScrollSpeed();
        }

        // Clear any existing lanes
        ClearLanes();

        // Create lanes with direct screen-based sizing
        CreateLanesForScreen();
    }

    // Call this method when level changes
    public void UpdateForLevel(int level)
    {
        if (level != currentLevel)
        {
            int previousLaneCount = currentCenterLaneCount;

            currentLevel = level;
            UpdateLaneCountForLevel(level);
            UpdateScrollSpeed();

            // Recreate lanes with new count if it changed
            if (previousLaneCount != currentCenterLaneCount)
            {
                ClearLanes();
                CreateLanesForScreen();

                // Notify listeners about lane count change
                NotifyLaneConfigurationChanged();
            }
        }
    }

    // Notify listeners when lane configuration changes
    private void NotifyLaneConfigurationChanged()
    {
        Debug.Log($"Lane configuration changed: {currentCenterLaneCount} center lanes");
        OnLaneConfigurationChanged?.Invoke(currentCenterLaneCount);
    }

    // Update scroll speed based on current game speed
    private void UpdateScrollSpeed()
    {
        if (GameManager.Instance != null)
        {
            // Sync with GameManager's speed
            float gameSpeed = GameManager.Instance.currentSpeed;
            float baseSpeed = GameManager.Instance.baseSpeed;

            // Calculate speed ratio (how much faster than base speed)
            float speedRatio = gameSpeed / baseSpeed;

            // Apply to scroll speed
            currentScrollSpeed = baseScrollSpeed * speedRatio * scrollSpeedMultiplier;

            Debug.Log($"Level {currentLevel}: Updated scroll speed to {currentScrollSpeed} (game speed: {gameSpeed})");

            // Update existing scrollers if any
            UpdateAllScrollers();
        }
    }

    // Update all existing lane scrollers with new speed
    private void UpdateAllScrollers()
    {
        SegmentScroller[] scrollers = GetComponentsInChildren<SegmentScroller>();
        foreach (SegmentScroller scroller in scrollers)
        {
            scroller.scrollSpeed = currentScrollSpeed;
        }
    }

    // Calculate how many center lanes for a given level
    private void UpdateLaneCountForLevel(int level)
    {
        // Calculate lane additions based on level
        int additionalLanes = ((level - 1) / lanesIncreaseEveryNLevels) * additionalLanesPerStep;

        // Apply the new lane count (clamped to max)
        currentCenterLaneCount = Mathf.Min(baseCenterLaneCount + additionalLanes, maxCenterLaneCount);

        Debug.Log($"Level {level}: Using {currentCenterLaneCount} center lanes");
    }

    // Clear existing lanes
    private void ClearLanes()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        lanes.Clear();
    }

    void CreateLanesForScreen()
    {
        // Get actual screen width in world units
        float screenWidth = 2 * mainCamera.orthographicSize * mainCamera.aspect;
        float screenHeight = 2 * mainCamera.orthographicSize;

        // Calculate lane widths based on screen width
        float sideLaneWidth = screenWidth * sideLaneWidthRatio;
        float centerLanesTotalWidth = screenWidth - (2 * sideLaneWidth);
        float centerLaneWidth = centerLanesTotalWidth / currentCenterLaneCount;

        // Calculate total number of lanes
        int totalLanes = currentCenterLaneCount + 2; // center lanes + 2 side lanes

        // Calculate segment height based on viewport percentage
        float viewportHeight = screenHeight * verticalViewportPercentage;
        float segmentHeight = viewportHeight / segmentsPerLane;

        // Starting X position (left edge of screen)
        float startX = -screenWidth / 2;

        Debug.Log($"Creating {totalLanes} lanes ({currentCenterLaneCount} center + 2 side) for level {currentLevel}");
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

            // Add scrolling component with current speed
            SegmentScroller scroller = lane.AddComponent<SegmentScroller>();
            scroller.scrollSpeed = currentScrollSpeed;
            scroller.segmentHeight = segmentHeight;
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
        // Calculate based on screen width
        float screenWidth = 2 * mainCamera.orthographicSize * mainCamera.aspect;
        float sideLaneWidth = screenWidth * sideLaneWidthRatio;
        float centerLaneWidth = (screenWidth - 2 * sideLaneWidth) / currentCenterLaneCount;

        return (laneIndex == 0 || laneIndex == lanes.Count - 1) ? sideLaneWidth : centerLaneWidth;
    }

    // Get the current number of lanes (including side lanes)
    public int GetTotalLaneCount()
    {
        return currentCenterLaneCount + 2; // Center lanes + 2 side lanes
    }

    // Get the current number of center lanes (excluding side lanes)
    public int GetCenterLaneCount()
    {
        return currentCenterLaneCount;
    }

    // Get current scroll speed
    public float GetScrollSpeed()
    {
        return currentScrollSpeed;
    }
}