using System.Buffers.Text;
using UnityEngine;
using static LaneManager;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneWidth = 60f;
    public float moveSpeed = 10f;
    public int currentLane = 3; // Start in the middle lane

    [Header("References")]
    public SpriteRenderer truckRenderer;

    private Vector3 targetPosition;
    private bool isMoving = false;
    private GameManager gameManager;
    private LaneManager laneManager;
    private BoxCollider2D playerCollider;

    // Dynamic lane boundaries
    private int minPlayableLane = 1;
    private int maxPlayableLane = 5;
    private int totalLanes = 7;

    private void Awake()
    {
        // Make sure we have a collider
        playerCollider = GetComponent<BoxCollider2D>();
        if (playerCollider == null)
        {
            playerCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // Set it as a trigger
        playerCollider.isTrigger = true;

        // Tag the player
        gameObject.tag = "Player";
    }

    private void Start()
    {
        // Get references
        gameManager = GameManager.Instance;
        laneManager = LaneManager.Instance;

        if (truckRenderer == null)
        {
            truckRenderer = GetComponent<SpriteRenderer>();
        }

        // Force visibility
        if (truckRenderer != null)
        {
            truckRenderer.sortingOrder = 10;
            truckRenderer.enabled = true;
        }

        // Ensure tag is set
        gameObject.tag = "Player";

        // Ensure collider is set properly
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider2D>();
        }
        collider.isTrigger = true;

        // Set collider size based on sprite if needed
        if (truckRenderer != null && (collider.size.x <= 0.001f || collider.size.y <= 0.001f))
        {
            collider.size = truckRenderer.bounds.size * 0.8f; // 80% of sprite size for better collision
        }

        // Subscribe to lane changes
        if (laneManager != null)
        {
            LaneManager.OnLaneConfigurationChanged += OnLaneConfigurationChanged;
        }

        // Initialize position
        UpdateLaneBoundaries();
        ResetPosition();

        // Update truck appearance
        if (gameManager != null)
        {
            UpdateTruckAppearance(gameManager.currentTruckType);
        }

        Debug.Log($"Player initialized. Lane boundaries: {minPlayableLane}-{maxPlayableLane}, Current lane: {currentLane}");
    }

    private void Update()
    {
        // Only process input when game is active
        if (gameManager != null && !gameManager.gameOver && gameManager.isGameStarted)
        {
            // Check if lane count changed (e.g. from level up)
            UpdateLaneBoundaries();

            // Handle keyboard input for movement
            HandleKeyboardInput();

            // Move towards target position
            MoveToTargetPosition();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Player collided with: " + other.gameObject.name + " (Tag: " + other.tag + ")");

        // Handle collisions with obstacles
        if (other.CompareTag("Obstacle") && gameManager != null && !gameManager.gameOver)
        {
            Debug.Log("Player hit obstacle: " + other.gameObject.name);
            gameManager.GameOver();
        }
    }

    // Update min and max lane boundaries based on current lane count
    private void UpdateLaneBoundaries()
    {
        if (laneManager != null)
        {
            // Get current center lane count (excluding side lanes)
            int centerLaneCount = laneManager.GetCenterLaneCount();
            totalLanes = laneManager.GetTotalLaneCount();

            // Side lanes are typically at index 0 and (total-1)
            minPlayableLane = 1; // First playable lane (after left side lane)
            maxPlayableLane = centerLaneCount; // Last playable lane (before right side lane)

            Debug.Log($"Updated lane boundaries: {minPlayableLane}-{maxPlayableLane}, Total center lanes: {centerLaneCount}");

            // Make sure current lane is within bounds
            if (currentLane < minPlayableLane)
                currentLane = minPlayableLane;
            else if (currentLane > maxPlayableLane)
                currentLane = maxPlayableLane;
        }
        else
        {
            // Fallback if lane manager not available
            minPlayableLane = 1;
            maxPlayableLane = 5; // Default assumption
            Debug.LogWarning("LaneManager not found, using default lane boundaries.");
        }
    }

    private void HandleKeyboardInput()
    {
        // Left arrow key moves left
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentLane > minPlayableLane)
        {
            MoveLeft();
        }
        // Right arrow key moves right
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentLane < maxPlayableLane)
        {
            MoveRight();
        }
    }

    public void MoveLeft()
    {
        if (!isMoving && currentLane > minPlayableLane)
        {
            currentLane--;
            UpdateTargetPosition();
            Debug.Log($"Moving left to lane {currentLane}");
        }
    }

    public void MoveRight()
    {
        if (!isMoving && currentLane < maxPlayableLane)
        {
            currentLane++;
            UpdateTargetPosition();
            Debug.Log($"Moving right to lane {currentLane}");
        }
    }

    private void UpdateTargetPosition()
    {
        if (laneManager != null)
        {
            // Use lane manager to get position
            targetPosition = laneManager.GetLanePosition(currentLane);
        }
        else
        {
            // Fallback if lane manager not available
            float laneCenter = (currentLane * laneWidth) - ((totalLanes * laneWidth) / 2) + (laneWidth / 2);
            targetPosition = new Vector3(laneCenter, transform.position.y, transform.position.z);
        }

        isMoving = true;
    }

    private void MoveToTargetPosition()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    public void ResetPosition()
    {
        // Set to middle lane (adjust based on available lanes)
        currentLane = Mathf.FloorToInt((minPlayableLane + maxPlayableLane) / 2f);

        // Update position immediately (not animated)
        if (laneManager != null)
        {
            transform.position = laneManager.GetLanePosition(currentLane);
        }
        else
        {
            // Fallback calculation
            float laneCenter = (currentLane * laneWidth) - ((totalLanes * laneWidth) / 2) + (laneWidth / 2);
            float yPosition = Camera.main ? (Camera.main.orthographicSize * -0.5f) : -4f; // Position at bottom quarter of screen
            transform.position = new Vector3(laneCenter, yPosition, 0);
        }

        isMoving = false;
        targetPosition = transform.position;

        Debug.Log($"Reset position to lane {currentLane} at {transform.position}");
    }

    public void UpdateTruckAppearance(GameManager.TruckType truckType)
    {
        if (truckRenderer == null || gameManager == null) return;

        // Update sprite based on truck type
        switch (truckType)
        {
            case GameManager.TruckType.General:
                truckRenderer.sprite = gameManager.generalTruckSprite;
                break;
            case GameManager.TruckType.Paper:
                truckRenderer.sprite = gameManager.paperTruckSprite;
                break;
            case GameManager.TruckType.Plastic:
                truckRenderer.sprite = gameManager.plasticTruckSprite;
                break;
            case GameManager.TruckType.Glass:
                truckRenderer.sprite = gameManager.glassTruckSprite;
                break;
        }

        // Update collider size to match new sprite
        if (playerCollider != null && truckRenderer != null)
        {
            playerCollider.size = truckRenderer.bounds.size * 0.8f;
        }
    }

    // Called when level changes to update truck position and lane limits
    public void OnLevelChanged(int newLevel)
    {
        UpdateLaneBoundaries();

        // Ensure truck is in a valid lane
        if (currentLane < minPlayableLane || currentLane > maxPlayableLane)
        {
            // Move to middle lane if current lane is invalid
            ResetPosition();
        }
        else
        {
            // Stay in current lane but update position
            UpdateTargetPosition();
        }

        Debug.Log($"Level changed to {newLevel}. New lane boundaries: {minPlayableLane}-{maxPlayableLane}, Current lane: {currentLane}");
    }

    // Handler for LaneManager.OnLaneConfigurationChanged event
    private void OnLaneConfigurationChanged(int centerLaneCount)
    {
        Debug.Log($"Player controller notified of lane count change: {centerLaneCount}");
        UpdateLaneBoundaries();

        // Ensure current lane is valid
        if (currentLane < minPlayableLane || currentLane > maxPlayableLane)
        {
            ResetPosition();
        }
        else
        {
            // Stay in current lane but update target position
            UpdateTargetPosition();
        }
    }

    private void OnDestroy()
    {
        if (laneManager != null)
        {
            // Unsubscribe from event
            LaneManager.OnLaneConfigurationChanged -= OnLaneConfigurationChanged;
        }
    }
}