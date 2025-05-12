using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IslandController : MonoBehaviour
{
    [Header("Island Parts")]
    public GameObject topPart;
    public List<GameObject> middleParts = new List<GameObject>();
    public GameObject bottomPart;

    private List<GameObject> allParts = new List<GameObject>();

    // No GameManager access in Awake
    private void Awake()
    {
        // Initialize local components
        InitializeIslandParts();
    }

    private void InitializeIslandParts()
    {
        // Gather all parts
        if (topPart != null) allParts.Add(topPart);
        allParts.AddRange(middleParts);
        if (bottomPart != null) allParts.Add(bottomPart);

        // Link all obstacle parts
        foreach (GameObject part in allParts)
        {
            ObstacleItem obstacle = part.GetComponent<ObstacleItem>();
            if (obstacle != null)
            {
                obstacle.isComplexObstacle = true;

                // Create array of linked parts
                List<ObstacleItem> linkedObstacles = new List<ObstacleItem>();
                foreach (GameObject otherPart in allParts)
                {
                    if (otherPart != part)
                    {
                        ObstacleItem otherObstacle = otherPart.GetComponent<ObstacleItem>();
                        if (otherObstacle != null)
                        {
                            linkedObstacles.Add(otherObstacle);
                        }
                    }
                }

                obstacle.linkedParts = linkedObstacles.ToArray();
            }
        }
    }

    // Helper method to position all parts in sequence
    public void PositionIsland(float x, float y)
    {
        float currentY = y;

        if (topPart != null)
        {
            topPart.transform.position = new Vector3(x, currentY, 0);
            SpriteRenderer sr = topPart.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                currentY += sr.bounds.size.y;
            }
            else
            {
                currentY += 1f; // Default if no SpriteRenderer
            }
        }

        foreach (GameObject middle in middleParts)
        {
            if (middle != null)
            {
                middle.transform.position = new Vector3(x, currentY, 0);
                SpriteRenderer sr = middle.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    currentY += sr.bounds.size.y;
                }
                else
                {
                    currentY += 1f; // Default if no SpriteRenderer
                }
            }
        }

        if (bottomPart != null)
        {
            bottomPart.transform.position = new Vector3(x, currentY, 0);
        }
    }

    // Helper method to get the total height of the island
    public float GetTotalHeight()
    {
        float height = 0;

        if (topPart != null)
        {
            SpriteRenderer sr = topPart.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                height += sr.bounds.size.y;
            }
        }

        foreach (GameObject middle in middleParts)
        {
            if (middle != null)
            {
                SpriteRenderer sr = middle.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    height += sr.bounds.size.y;
                }
            }
        }

        if (bottomPart != null)
        {
            SpriteRenderer sr = bottomPart.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                height += sr.bounds.size.y;
            }
        }

        return height > 0 ? height : 3f; // Default height if calculation fails
    }
}