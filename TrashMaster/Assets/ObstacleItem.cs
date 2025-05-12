using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleItem : MonoBehaviour
{
    [Header("Obstacle Properties")]
    public bool isComplexObstacle = false;

    // For complex obstacles like islands that are made of multiple parts
    public ObstacleItem[] linkedParts;

    // No GameManager access in Awake
    private void Awake()
    {
        // Initialize only local components
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Safe to access GameManager here since we're not in Awake
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}