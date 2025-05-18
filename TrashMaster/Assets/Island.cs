using System.Collections.Generic;
using UnityEngine;

public class Island : MonoBehaviour
{
    [Header("Island Parts")]
    public GameObject topPart;
    public GameObject middlePart;
    public GameObject bottomPart;
    public int middlePartCount = 1; // How many middle parts to use

    private List<ObstacleItem> allPartObstacles = new List<ObstacleItem>();
    private float totalHeight = 0f;

    private void Awake()
    {
        CreateIslandParts();
    }

    private void CreateIslandParts()
    {
        if (topPart == null || middlePart == null || bottomPart == null)
        {
            Debug.LogError("Island parts are not assigned!");
            return;
        }

        // Create top part
        GameObject top = Instantiate(topPart, transform);
        ObstacleItem topObstacle = top.GetComponent<ObstacleItem>();
        if (topObstacle == null)
        {
            topObstacle = top.AddComponent<ObstacleItem>();
        }
        topObstacle.isComplexObstacle = true;
        allPartObstacles.Add(topObstacle);

        // Adjust position of top part
        SpriteRenderer topRenderer = top.GetComponent<SpriteRenderer>();
        float currentHeight = topRenderer ? topRenderer.bounds.size.y : 1f;
        totalHeight += currentHeight;

        // Create middle parts
        for (int i = 0; i < middlePartCount; i++)
        {
            GameObject middle = Instantiate(middlePart, transform);
            ObstacleItem middleObstacle = middle.GetComponent<ObstacleItem>();
            if (middleObstacle == null)
            {
                middleObstacle = middle.AddComponent<ObstacleItem>();
            }
            middleObstacle.isComplexObstacle = true;
            allPartObstacles.Add(middleObstacle);

            // Position middle part
            middle.transform.localPosition = new Vector3(0, -totalHeight, 0);

            // Adjust for next part
            SpriteRenderer middleRenderer = middle.GetComponent<SpriteRenderer>();
            float middleHeight = middleRenderer ? middleRenderer.bounds.size.y : 1f;
            totalHeight += middleHeight;
        }

        // Create bottom part
        GameObject bottom = Instantiate(bottomPart, transform);
        ObstacleItem bottomObstacle = bottom.GetComponent<ObstacleItem>();
        if (bottomObstacle == null)
        {
            bottomObstacle = bottom.AddComponent<ObstacleItem>();
        }
        bottomObstacle.isComplexObstacle = true;
        allPartObstacles.Add(bottomObstacle);

        // Position bottom part
        bottom.transform.localPosition = new Vector3(0, -totalHeight, 0);

        // Add bottom part height
        SpriteRenderer bottomRenderer = bottom.GetComponent<SpriteRenderer>();
        float bottomHeight = bottomRenderer ? bottomRenderer.bounds.size.y : 1f;
        totalHeight += bottomHeight;

        // Now link all parts together
        foreach (ObstacleItem part in allPartObstacles)
        {
            List<ObstacleItem> linkedParts = new List<ObstacleItem>();
            foreach (ObstacleItem otherPart in allPartObstacles)
            {
                if (otherPart != part)
                {
                    linkedParts.Add(otherPart);
                }
            }
            part.linkedParts = linkedParts.ToArray();
        }
    }

    public float GetHeight()
    {
        return totalHeight;
    }
}