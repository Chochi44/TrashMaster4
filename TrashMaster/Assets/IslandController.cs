using System.Collections.Generic;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    [Header("Island Parts")]
    public GameObject topPartPrefab;
    public GameObject middlePartPrefab;
    public GameObject bottomPartPrefab;

    [Range(0, 4)]
    public int middlePartCount = 2; // How many middle parts to use

    private List<GameObject> allParts = new List<GameObject>();
    private float totalHeight = 0f;
    private bool isSetup = false;

    private void Awake()
    {
        // Create the island structure
        if (!isSetup)
            CreateIslandParts();
    }

    private void OnEnable()
    {
        // Re-create parts if they were destroyed
        if (allParts.Count == 0)
            CreateIslandParts();
    }

    private void CreateIslandParts()
    {
        // Clear any existing parts first
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }
        allParts.Clear();
        totalHeight = 0f;

        // Clamp middle part count to reasonable limits
        middlePartCount = Mathf.Clamp(middlePartCount, 0, 4);

        // Create parts as in your existing code but add height checking
        CreateTopPart();
        CreateMiddleParts();
        CreateBottomPart();

        // After creation, check if total height is reasonable
        float screenHeight = Camera.main ? Camera.main.orthographicSize * 2 : 10f;
        if (totalHeight > screenHeight * 0.7f)
        {
            Debug.LogWarning($"Island height ({totalHeight}) exceeds recommended screen percentage. Consider reducing middle parts.");
        }

        LinkObstacleParts();
        isSetup = true;
    }

    // Add these helper methods to break up the creation process
    private void CreateTopPart()
    {
        if (topPartPrefab != null)
        {
            GameObject top = Instantiate(topPartPrefab, transform);
            allParts.Add(top);
            top.transform.localPosition = Vector3.zero;

            SpriteRenderer topRenderer = top.GetComponent<SpriteRenderer>();
            if (topRenderer != null)
            {
                totalHeight += topRenderer.bounds.size.y;
            }

            SetupObstacleItem(top);
        }
    }

    private void CreateMiddleParts()
    {
        if (middlePartPrefab != null)
        {
            for (int i = 0; i < middlePartCount; i++)
            {
                GameObject middle = Instantiate(middlePartPrefab, transform);
                allParts.Add(middle);

                middle.transform.localPosition = new Vector3(0, -totalHeight, 0);

                SpriteRenderer middleRenderer = middle.GetComponent<SpriteRenderer>();
                if (middleRenderer != null)
                {
                    totalHeight += middleRenderer.bounds.size.y;
                }

                SetupObstacleItem(middle);
            }
        }
    }

    private void CreateBottomPart()
    {
        if (bottomPartPrefab != null)
        {
            GameObject bottom = Instantiate(bottomPartPrefab, transform);
            allParts.Add(bottom);

            bottom.transform.localPosition = new Vector3(0, -totalHeight, 0);

            SpriteRenderer bottomRenderer = bottom.GetComponent<SpriteRenderer>();
            if (bottomRenderer != null)
            {
                totalHeight += bottomRenderer.bounds.size.y;
            }

            SetupObstacleItem(bottom);
        }
    }

    private void SetupObstacleItem(GameObject part)
    {
        // Make sure the part has an ObstacleItem component
        ObstacleItem obstacleItem = part.GetComponent<ObstacleItem>();
        if (obstacleItem == null)
        {
            obstacleItem = part.AddComponent<ObstacleItem>();
        }

        // Set it as part of a complex obstacle
        obstacleItem.isComplexObstacle = true;

        // Make sure it's tagged as an obstacle
        part.tag = "Obstacle";

        // Make sure it has a collider
        BoxCollider2D collider = part.GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = part.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        // Adjust collider size based on sprite if needed
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        if (renderer != null && (collider.size.x <= 0.001f || collider.size.y <= 0.001f))
        {
            collider.size = renderer.bounds.size;
        }
    }

    private void LinkObstacleParts()
    {
        // Get all obstacle items
        List<ObstacleItem> obstacleItems = new List<ObstacleItem>();
        foreach (GameObject part in allParts)
        {
            ObstacleItem item = part.GetComponent<ObstacleItem>();
            if (item != null)
            {
                obstacleItems.Add(item);
            }
        }

        // Link each part to all other parts
        foreach (ObstacleItem item in obstacleItems)
        {
            List<ObstacleItem> linkedParts = new List<ObstacleItem>();
            foreach (ObstacleItem otherItem in obstacleItems)
            {
                if (otherItem != item)
                {
                    linkedParts.Add(otherItem);
                }
            }
            item.linkedParts = linkedParts.ToArray();
        }
    }

    public float GetHeight()
    {
        return totalHeight;
    }

    // Method to position the whole island at a specific position
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    // These are the only main object parts that count for level completion
    public void SetMainObjectParts(bool isMainObject)
    {
        // Typically only count the first part or specific parts
        if (allParts.Count > 0)
        {
            ObstacleItem firstPart = allParts[0].GetComponent<ObstacleItem>();
            if (firstPart != null)
            {
                firstPart.isMainObject = isMainObject;
            }

            // Make other parts not main objects to avoid duplicate counting
            for (int i = 1; i < allParts.Count; i++)
            {
                ObstacleItem part = allParts[i].GetComponent<ObstacleItem>();
                if (part != null)
                {
                    part.isMainObject = false;
                }
            }
        }
    }
}