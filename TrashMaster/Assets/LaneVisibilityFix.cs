using UnityEngine;

/// <summary>
/// Simple script to fix lane visibility by changing background colors and adding outlines
/// </summary>
public class LaneVisibilityFix : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera gameCamera;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f); // Dark gray instead of black

    [Header("Lane Enhancement")]
    public bool addOutlinesToLanes = true;
    public Color outlineColor = Color.white;
    public float outlineWidth = 2f;

    void Start()
    {
        // Find camera if not assigned
        if (gameCamera == null)
            gameCamera = Camera.main;

        // Change camera background color for better visibility
        if (gameCamera != null)
        {
            gameCamera.backgroundColor = backgroundColor;
            Debug.Log("Camera background color changed to: " + backgroundColor);
        }

        // Add outlines to lanes
        if (addOutlinesToLanes)
            AddOutlinesToLanes();
    }

    void AddOutlinesToLanes()
    {
        // Find lanes by name since the tag isn't defined
        GameObject[] possibleLanes = FindObjectsOfType<GameObject>();
        int lanesEnhanced = 0;

        foreach (GameObject obj in possibleLanes)
        {
            if (obj.name.Contains("Lane"))
            {
                // Add outline component
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Create a child object for the outline
                    GameObject outline = new GameObject("Outline");
                    outline.transform.SetParent(obj.transform);
                    outline.transform.localPosition = Vector3.zero;
                    outline.transform.localScale = Vector3.one;

                    // Add sprite renderer with the same sprite
                    SpriteRenderer outlineSr = outline.AddComponent<SpriteRenderer>();
                    outlineSr.sprite = sr.sprite;

                    // Make it slightly larger for outline effect
                    outline.transform.localScale = new Vector3(1.05f, 1.0f, 1.0f);

                    // Set color and sorting order
                    outlineSr.color = outlineColor;
                    outlineSr.sortingOrder = sr.sortingOrder - 1;

                    // Also slightly brighten the lane sprite itself
                    sr.color = new Color(0.8f, 0.8f, 0.8f); // Light gray for better contrast

                    lanesEnhanced++;
                }
            }
        }

        Debug.Log("Enhanced visibility for " + lanesEnhanced + " lanes");
    }
}