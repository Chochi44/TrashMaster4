using UnityEngine;

public class LanePositionFixer : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneWidth = 60f;
    public int laneCount = 7;

    [Header("Lane Colors")]
    public Color[] laneColors = new Color[] {
        Color.red,         // Lane 0 (leftmost)
        Color.magenta,     // Lane 1
        Color.yellow,      // Lane 2
        Color.green,       // Lane 3 (center)
        Color.cyan,        // Lane 4
        Color.blue,        // Lane 5
        Color.white        // Lane 6 (rightmost)
    };

    void Start()
    {
        Debug.Log("LanePositionFixer starting to reposition lanes...");

        // Find all the lanes
        GameObject[] allGameObjects = FindObjectsOfType<GameObject>();
        GameObject[] lanes = new GameObject[laneCount];

        // First, find lanes by name
        for (int i = 1; i <= laneCount; i++)
        {
            string laneName = "Lane" + i;

            foreach (GameObject obj in allGameObjects)
            {
                if (obj.name == laneName)
                {
                    lanes[i - 1] = obj;
                    Debug.Log("Found lane by name: " + laneName);
                    break;
                }
            }
        }

        // If we didn't find all lanes by name, look for objects containing "Lane"
        if (System.Array.IndexOf(lanes, null) >= 0)
        {
            Debug.Log("Some lanes not found by exact name, searching for partial matches...");
            int laneIndex = 0;

            foreach (GameObject obj in allGameObjects)
            {
                if (obj.name.Contains("Lane") && System.Array.IndexOf(lanes, obj) < 0)
                {
                    if (laneIndex < laneCount && lanes[laneIndex] == null)
                    {
                        lanes[laneIndex] = obj;
                        Debug.Log("Found lane by partial match: " + obj.name + " -> Lane" + (laneIndex + 1));
                        laneIndex++;
                    }
                }
            }
        }

        // Calculate starting position
        float totalWidth = laneCount * laneWidth;
        float startX = -totalWidth / 2f + laneWidth / 2f;

        Debug.Log("Calculated lane positions: Start X = " + startX + ", Lane Width = " + laneWidth);

        // Reposition each lane and set colors
        for (int i = 0; i < laneCount; i++)
        {
            if (lanes[i] != null)
            {
                // Calculate correct X position
                float xPos = startX + (i * laneWidth);

                // Position the lane
                lanes[i].transform.position = new Vector3(xPos, 0, 0);

                // Set color
                SpriteRenderer renderer = lanes[i].GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = laneColors[i];
                }

                Debug.Log("Repositioned " + lanes[i].name + " to X=" + xPos +
                          " and set color to " + laneColors[i]);
            }
            else
            {
                Debug.LogWarning("Lane at index " + i + " was not found!");
            }
        }

        // Adjust camera to see all lanes
        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.orthographic)
        {
            float requiredSize = (totalWidth / 2f) / mainCamera.aspect;
            mainCamera.orthographicSize = Mathf.Max(requiredSize, 4f);
            Debug.Log("Set camera orthographic size to " + mainCamera.orthographicSize);
        }

        Debug.Log("Lane positioning complete");
    }
}