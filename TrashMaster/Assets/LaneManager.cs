using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    [Header("Lane Prefabs")]
    public GameObject leftSideLanePrefab;  // Use with leftside_0 sprite (69x630)
    public GameObject standardLanePrefab;  // Use with lane_0 sprite (60x90)
    public GameObject rightSideLanePrefab; // Use with rightside_0 sprite (69x630)

    [Header("Lane Settings")]
    public int laneCount = 7;
    public float laneWidth = 60f;    // Standard lane width
    public float sideWidth = 69f;    // Side lane width
    public float scrollLength = 630f; // Side lane height - for scrolling length

    private List<GameObject> lanes = new List<GameObject>();
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        // Wait one frame to ensure GameManager is initialized
        StartCoroutine(DelayedInit());
    }

    private IEnumerator DelayedInit()
    {
        yield return null;
        CreateLanes();
    }

    // Create all lanes based on prefabs
    private void CreateLanes()
    {
        Debug.Log("Creating lanes. Lane count: " + laneCount);

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("No main camera found!");
                return;
            }
        }

        // Calculate screen dimensions
        float screenHeight = 2f * mainCamera.orthographicSize;
        float screenWidth = screenHeight * mainCamera.aspect;

        // Calculate total width based on lane types
        float totalWidth = (laneCount - 2) * laneWidth + 2 * sideWidth;
        float startX = -totalWidth / 2f;
        float posX = startX;

        // Create lanes
        for (int i = 0; i < laneCount; i++)
        {
            GameObject lanePrefab;
            float currentWidth;

            // Select the correct lane prefab based on position
            if (i == 0)
            {
                lanePrefab = leftSideLanePrefab;
                currentWidth = sideWidth;
            }
            else if (i == laneCount - 1)
            {
                lanePrefab = rightSideLanePrefab;
                currentWidth = sideWidth;
            }
            else
            {
                lanePrefab = standardLanePrefab;
                currentWidth = laneWidth;
            }

            if (lanePrefab == null)
            {
                Debug.LogError($"Lane prefab is null for lane {i}. Make sure all lane prefabs are assigned!");
                posX += currentWidth;
                continue;
            }

            // Instantiate lane
            GameObject lane = Instantiate(lanePrefab, transform);

            // Position lane at center of its space
            lane.transform.position = new Vector3(posX + currentWidth / 2f, 0, 0);
            lane.name = $"Lane_{i}";

            // Add to list
            lanes.Add(lane);

            // Setup road scroller
            RoadScroller scroller = lane.GetComponent<RoadScroller>();
            if (scroller != null)
            {
                scroller.useGlobalSpeed = true;
            }
            else
            {
                Debug.LogWarning($"RoadScroller component missing on lane {i}");
                lane.AddComponent<RoadScroller>();
            }

            // Setup sprite renderer for tiling
            SpriteRenderer spriteRenderer = lane.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Use tiling mode for standard lanes
                if (i > 0 && i < laneCount - 1)
                {
                    // For center lanes, enable tiling of the sprite
                    spriteRenderer.drawMode = SpriteDrawMode.Tiled;
                    spriteRenderer.size = new Vector2(laneWidth, screenHeight * 2.5f);
                }
                else
                {
                    // For side lanes, just ensure they're tall enough
                    spriteRenderer.drawMode = SpriteDrawMode.Simple;
                    // Scale to fit screen height while preserving width
                    float scale = screenHeight * 2.5f / scrollLength;
                    lane.transform.localScale = new Vector3(1f, scale, 1f);
                }

                // Make sure sorting layer is set correctly
                spriteRenderer.sortingOrder = 0; // Background
            }
            else
            {
                Debug.LogError($"SpriteRenderer component missing on lane {i}");
            }

            // Move to next lane position
            posX += currentWidth;

            Debug.Log($"Created lane {i} at position {lane.transform.position}");
        }

        Debug.Log("Total lanes created: " + lanes.Count);
    }

    // Get lane center position X by lane index
    public float GetLanePositionX(int laneIndex)
    {
        if (laneIndex >= 0 && laneIndex < lanes.Count)
        {
            return lanes[laneIndex].transform.position.x;
        }

        // Default to middle lane if invalid
        Debug.LogWarning("Invalid lane index: " + laneIndex + ". Defaulting to center.");
        return 0f;
    }

    // Get lane by index
    public GameObject GetLane(int laneIndex)
    {
        if (laneIndex >= 0 && laneIndex < lanes.Count)
        {
            return lanes[laneIndex];
        }

        Debug.LogWarning("Invalid lane index: " + laneIndex + ". Returning null.");
        return null;
    }

    // Visualize lanes in editor
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            Gizmos.color = Color.yellow;

            float totalWidth = (laneCount - 2) * laneWidth + 2 * sideWidth;
            float startX = -totalWidth / 2f;
            float posX = startX;

            for (int i = 0; i < laneCount; i++)
            {
                float width = (i == 0 || i == laneCount - 1) ? sideWidth : laneWidth;
                Gizmos.DrawWireCube(new Vector3(posX + width / 2f, 0, 0), new Vector3(width * 0.9f, 10, 0));
                posX += width;
            }
        }
    }
}