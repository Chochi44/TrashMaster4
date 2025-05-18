using UnityEngine;

public class ObstacleItem : MonoBehaviour
{
    [Header("Obstacle Properties")]
    public bool isComplexObstacle = false;
    public ObstacleItem[] linkedParts;
    public GameObject collisionEffectPrefab;

    // Used for level completion tracking
    public bool isMainObject = true;

    private BoxCollider2D myCollider;
    private PlayerController player;
    private LevelManager levelManager;

    private void Awake()
    {
        myCollider = GetComponent<BoxCollider2D>();
        if (myCollider == null)
        {
            myCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Make sure it's a trigger collider
        myCollider.isTrigger = true;

        // Set appropriate size if needed
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && myCollider.size.x <= 0.001f)
        {
            myCollider.size = spriteRenderer.bounds.size;
        }
    }

    private void Start()
    {
        // Find player reference
        player = FindObjectOfType<PlayerController>();

        // Find level manager
        levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("[ObstacleItem] Obstacle collision detected with: " + other.gameObject.name);

        if (other.CompareTag("Player"))
        {
            TriggerGameOver();
        }
    }

    // Manual collision check in case triggers fail
    private void Update()
    {
        if (player != null && myCollider != null)
        {
            BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
            if (playerCollider != null && myCollider.bounds.Intersects(playerCollider.bounds))
            {
                TriggerGameOver();
            }
        }
    }

    private void TriggerGameOver()
    {
        if (GameManager.Instance != null && !GameManager.Instance.gameOver)
        {
            Debug.Log("[ObstacleItem] Game over triggered by obstacle: " + gameObject.name);

            // Play effect if available
            if (collisionEffectPrefab != null)
            {
                Instantiate(collisionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Notify level manager (although game over will happen)
            if (levelManager != null && isMainObject)
            {
                levelManager.NotifyObjectProcessed(isMainObject);
            }

            // End the game
            GameManager.Instance.GameOver();
        }
    }
}