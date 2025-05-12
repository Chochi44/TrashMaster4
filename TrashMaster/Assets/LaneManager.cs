using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaneManager : MonoBehaviour
{
    [Header("Lane Prefabs")]
    public GameObject leftSideLanePrefab;
    public GameObject standardLanePrefab;
    public GameObject rightSideLanePrefab;

    [Header("Lane Settings")]
    public int laneCount = 7;
    public float laneWidth = 60f;

    // Debug settings
    [Header("Debug")]
    public bool drawGizmos = true;
    public Color gizmoColor = Color.yellow;

    private List<GameObject> lanes = new List<GameObject>();

    private void Awake()
    {
        // Initialize only local components
    }

    private void Start()
    {
        // Now it's safe to access GameManager
        CreateLanes();
    }

    // Create all lanes based on prefabs
    private void CreateLanes()
    {
        Debug.Log("Creating lanes. Lane count: " + laneCount);

        // Calculate total width
        float totalWidth = laneCount * laneWidth;
        float startX = -totalWidth / 2 + laneWidth / 2;

        // Create lanes
        for (int i = 0; i < laneCount; i++)
        {
            GameObject lanePrefab;

            // Select the correct lane prefab based on position
            if (i == 0)
                lanePrefab = leftSideLanePrefab;
            else if (i == laneCount - 1)
                lanePrefab = rightSideLanePrefab;
            else
                lanePrefab = standardLanePrefab;

            if (lanePrefab == null)
            {
                Debug.LogError("Lane prefab is null for lane " + i + ". Make sure all lane prefabs are assigned!");
                continue;
            }

            // Instantiate lane
            GameObject lane = Instantiate(lanePrefab, transform);

            // Position lane
            float xPos = startX + (i * laneWidth);
            lane.transform.position = new Vector3(xPos, 0, 0);
            lane.name = "Lane_" + i;

            Debug.Log("Created lane " + i + " at position " + xPos);

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
                Debug.LogWarning("RoadScroller component missing on lane " + i);
            }

            // Check if sprite renderer exists
            SpriteRenderer spriteRenderer = lane.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer component missing on lane " + i);
            }
        }

        // Log total lanes created
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

    // Draw gizmos to visualize lane positions
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = gizmoColor;

        // Draw lane positions in edit mode
        if (!Application.isPlaying)
        {
            float totalWidth = laneCount * laneWidth;
            float startX = -totalWidth / 2 + laneWidth / 2;

            for (int i = 0; i < laneCount; i++)
            {
                float xPos = startX + (i * laneWidth);
                Gizmos.DrawWireCube(new Vector3(xPos, 0, 0), new Vector3(laneWidth * 0.9f, 10, 0));
            }
        }
        else
        {
            // Draw existing lanes in play mode
            foreach (GameObject lane in lanes)
            {
                if (lane != null)
                {
                    Gizmos.DrawWireCube(lane.transform.position, new Vector3(laneWidth * 0.9f, 10, 0));
                }
            }
        }
    }
}