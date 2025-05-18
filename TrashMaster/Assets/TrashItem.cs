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

    // Add a flag to track if penalty was already applied
    private bool penaltyApplied = false;

    [Header("Effects")]
    public GameObject collectionEffectPrefab;

    // Reference to player for collision check if trigger fails
    private PlayerController player;
    private BoxCollider2D myCollider;
    private LevelManager levelManager;
    private bool isCollectible = true; // Flag to track if this trash can be collected by current truck

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
        if (spriteRenderer != null && (myCollider.size.x <= 0.001f || myCollider.size.y <= 0.001f))
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

        // Determine if this trash is collectible by the current truck
        UpdateCollectibleStatus();

        // Reset penalty flag when object is created
        penaltyApplied = false;
    }

    private void OnEnable()
    {
        // When object is reactivated, reset flags
        penaltyApplied = false;
        isCollected = false;

        // Update collectible status
        if (GameManager.Instance != null)
        {
            UpdateCollectibleStatus();
        }
    }

    // Call this when truck type changes
    private void UpdateCollectibleStatus()
    {
        // Default to collectible
        isCollectible = true;

        if (GameManager.Instance != null)
        {
            // Get the current truck type
            GameManager.TruckType truckType = GameManager.Instance.currentTruckType;

            // General truck can collect any trash
            if (truckType == GameManager.TruckType.General)
                return;

            // Check if this trash matches the truck type
            if (truckType == GameManager.TruckType.Paper && !isPaper)
                isCollectible = false;
            else if (truckType == GameManager.TruckType.Plastic && !isPlastic)
                isCollectible = false;
            else if (truckType == GameManager.TruckType.Glass && !isGlass)
                isCollectible = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isCollected && !penaltyApplied)
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    // Manual collision check in case triggers fail
    private void Update()
    {
        if (!isCollected && !penaltyApplied && player != null && player.GetComponent<BoxCollider2D>() != null)
        {
            if (myCollider.bounds.Intersects(player.GetComponent<BoxCollider2D>().bounds))
            {
                HandlePlayerCollision(player.gameObject);
            }
        }
    }

    private void HandlePlayerCollision(GameObject playerObject)
    {
        // Update collectible status in case truck type changed
        UpdateCollectibleStatus();

        if (isCollectible)
        {
            // Correct type - collect with points
            CollectCorrectType();
        }
        else
        {
            // Wrong type - apply penalty and show notification
            // But only if we haven't already applied a penalty
            if (!penaltyApplied)
            {
                ApplyWrongTypePenalty();

                // Set flag to prevent multiple penalties
                penaltyApplied = true;

                // Add a short delay before allowing further collisions
                StartCoroutine(ResetPenaltyAfterDelay(1.0f));
            }
        }
    }

    private IEnumerator ResetPenaltyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        penaltyApplied = false;
    }

    private void CollectCorrectType()
    {
        if (!isCollected)
        {
            isCollected = true;
            Debug.Log("[TrashItem] Trash collected correctly: " + gameObject.name);

            // Play collection effect if available
            if (collectionEffectPrefab != null)
            {
                Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
            }

            // Add score for correct type
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

    private void ApplyWrongTypePenalty()
    {
        // Apply penalty for trying to collect wrong type
        Debug.Log("Wrong truck type for this trash - applying penalty: " + gameObject.name);

        // Apply wrong type penalty ONCE
        if (GameManager.Instance != null)
        {
            // Apply the penalty (25 points)
            GameManager.Instance.ApplyWrongTypePenalty();

            // Show wrong type notification
            if (GameManager.Instance.uiManager != null)
            {
                GameManager.Instance.uiManager.ShowWrongTypeMessage();
            }
        }

        // The trash remains on the road, not collected
    }

    // This is called by the LevelManager when the trash passes below the player
    public void Missed()
    {
        if (!isCollected)
        {
            Debug.Log("[TrashItem] Trash missed: " + gameObject.name);

            // Apply missed trash penalty ONLY if this was collectible by the current truck
            if (GameManager.Instance != null && isCollectible && !penaltyApplied)
            {
                Debug.Log("[TrashItem] Applying penalty for missing collectible trash: -25 points");
                GameManager.Instance.MissTrash();
            }
            else
            {
                Debug.Log("[TrashItem] No penalty for missing non-collectible trash or already penalized trash");
            }

            // Notify level manager that this object is processed
            if (levelManager != null && isMainObject)
            {
                levelManager.NotifyObjectProcessed(isMainObject);
            }

            // Deactivate the object
            gameObject.SetActive(false);
        }
    }
}