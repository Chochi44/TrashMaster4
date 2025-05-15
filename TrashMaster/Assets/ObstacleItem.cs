using UnityEngine;

public class ObstacleItem : MonoBehaviour
{
    [Header("Obstacle Properties")]
    public bool isComplexObstacle = false;

    // For complex obstacles like islands that are made of multiple parts
    public ObstacleItem[] linkedParts;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}