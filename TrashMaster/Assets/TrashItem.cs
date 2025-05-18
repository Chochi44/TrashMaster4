using System.Collections;
using UnityEngine;

public class TrashItem : MonoBehaviour
{
    [Header("Trash Type")]
    public bool isPaper = false;
    public bool isPlastic = false;
    public bool isGlass = false;
    public bool isGeneral = false;

    [Header("Game Properties")]
    public int pointValue = 100;
    public bool isCollected = false;
    public bool isMainObject = true; // Used for level completion tracking

    [Header("Effects")]
    public GameObject collectionEffectPrefab;

    // Reference to player for collision check if trigger fails
    private PlayerController player;
    private BoxCollider2D myCollider;
    private LevelManager levelManager;

    private void Awake()
    {
        myCollider = GetComponent<BoxCollider2D>();
        if (myCollider == null)
        {
            myCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Ensure the collider is properly set up
        myCollider.isTrigger = true;

        // Get sprite bounds
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && myCollider.size.x <= 0.001f)
        {
            myCollider.size = spriteRenderer.bounds.size;
        }
    }

    private void Start()
    {
        // Find player
        player = FindObjectOfType<PlayerController>();

        // Find level manager
        levelManager = FindObjectOfType<LevelManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    // Manual collision check in case triggers fail
    private void Update()
    {
        if (!isCollected && player != null)
        {
            if (myCollider.bounds.Intersects(player.GetComponent<BoxCollider2D>().bounds))
            {
                HandlePlayerCollision(player.gameObject);
            }
        }
    }

    private void HandlePlayerCollision(GameObject playerObject)
    {
        // Check if player can collect this trash type
        TruckTypeController truckController = playerObject.GetComponent<TruckTypeController>();
        if (truckController != null)
        {
            if (truckController.CanCollectTrash(this))
            {
                Collect();
            }
            else
            {
                WrongType();
            }
        }
        else
        {
            // If no TruckTypeController, just collect it
            Collect();
        }
    }

    public void Collect()
    {
        if (!isCollected)
        {
            isCollected = true;
            Debug.Log("[TrashItem] Trash collected: " + gameObject.name);

            // Play collection effect if available
            if (collectionEffectPrefab != null)
            {
                Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Add score
            if (GameManager.Instance != null)
            {
                GameManager.Instance.CollectTrash();
            }

            // Notify level manager
            if (levelManager != null)
            {
                levelManager.NotifyObjectProcessed(isMainObject);
            }

            // Hide the trash item
            gameObject.SetActive(false);
        }
    }

    private void WrongType()
    {
        Debug.Log("Wrong truck type for this trash: " + gameObject.name);

        // Apply wrong type penalty
        if (GameManager.Instance != null)
        {
            // Explicitly use the penalty amount from GameManager
            int penalty = GameManager.Instance.wrongTypePenalty;
            Debug.Log($"Applying wrong type penalty: -{penalty}");

            GameManager.Instance.ApplyWrongTypePenalty();

            // Show wrong type notification
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowWrongTypeMessage();
            }
        }
    }

    // This is called by the LevelManager when the trash passes below the player
    public void Missed()
    {
        if (!isCollected)
        {
            Debug.Log("[TrashItem] Trash missed: " + gameObject.name);

            // Apply missed trash penalty
            if (GameManager.Instance != null)
            {
                GameManager.Instance.MissTrash();
            }

            // Deactivate the object
            gameObject.SetActive(false);
        }
    }
}