using UnityEngine;

public class LevelCompletionFixer : MonoBehaviour
{
    private LevelManager levelManager;
    private GameManager gameManager;
    private PlayerController player;

    // Time to wait before checking if level is complete (in seconds)
    [Range(1f, 10f)]
    public float checkInterval = 2f;
    private float timeSinceLastCheck = 0f;

    // Extra delay after objects pass before triggering next level
    [Range(1f, 5f)]
    public float levelAdvanceDelay = 3f;
    private bool levelCompleteTriggered = false;
    private float levelCompletionTimer = 0f;

    // Track if we should log details
    public bool debugMode = true;

    void Start()
    {
        levelManager = GetComponent<LevelManager>();
        gameManager = FindObjectOfType<GameManager>();
        player = FindObjectOfType<PlayerController>();

        if (levelManager == null)
        {
            Debug.LogError("LevelCompletionFixer: No LevelManager component found on the same GameObject");
        }

        if (gameManager == null)
        {
            Debug.LogError("LevelCompletionFixer: No GameManager found in the scene");
        }
    }

    void Update()
    {
        // Don't check if game is over or not started
        if (gameManager == null || gameManager.gameOver || !gameManager.isGameStarted)
            return;

        // If level completion sequence has started, handle it
        if (levelCompleteTriggered)
        {
            levelCompletionTimer += Time.deltaTime;

            // After delay, trigger the level advancement
            if (levelCompletionTimer >= levelAdvanceDelay)
            {
                // Reset for next level
                levelCompleteTriggered = false;
                levelCompletionTimer = 0f;

                // Advance to next level
                TriggerNextLevel();
            }

            return; // Skip the rest when level completion is in progress
        }

        // Periodic check for level completion
        timeSinceLastCheck += Time.deltaTime;
        if (timeSinceLastCheck >= checkInterval)
        {
            timeSinceLastCheck = 0f;
            CheckLevelCompletion();
        }
    }

    private void CheckLevelCompletion()
    {
        // Get counts of main lane objects
        int trashCount = 0;
        int obstacleCount = 0;
        CountObjectsAbovePlayer(out trashCount, out obstacleCount);

        int totalCount = trashCount + obstacleCount;

        if (debugMode)
        {
            Debug.Log($"[LevelCompletionFixer] Objects above player: {totalCount} (Trash: {trashCount}, Obstacles: {obstacleCount})");
        }

        // Only trigger level completion when ALL objects have passed the player
        if (totalCount == 0)
        {
            Debug.Log("[LevelCompletionFixer] All objects have passed the player. Starting level completion sequence.");
            levelCompleteTriggered = true;
            levelCompletionTimer = 0f;
        }
    }

    private void CountObjectsAbovePlayer(out int trashCount, out int obstacleCount)
    {
        trashCount = 0;
        obstacleCount = 0;

        if (player == null) return;

        float playerY = player.transform.position.y;

        // Check trash objects
        GameObject[] trashObjects = GameObject.FindGameObjectsWithTag("Trash");
        foreach (GameObject obj in trashObjects)
        {
            // Only count objects in main lanes and above player
            if (IsInMainLanes(obj.transform.position) && obj.transform.position.y > playerY)
            {
                trashCount++;
            }
        }

        // Check obstacle objects
        GameObject[] obstacleObjects = GameObject.FindGameObjectsWithTag("Obstacle");
        foreach (GameObject obj in obstacleObjects)
        {
            // Only count objects in main lanes and above player
            if (IsInMainLanes(obj.transform.position) && obj.transform.position.y > playerY)
            {
                obstacleCount++;
            }
        }
    }

    private bool IsInMainLanes(Vector3 position)
    {
        // Determine the width of the main lanes - this may need adjustment for your specific game
        float sideMargin = 1.5f; // Adjust based on your lane widths
        float maxX = Camera.main.orthographicSize * Camera.main.aspect - sideMargin;

        // Check if position is in the center lanes (not on the sides)
        return Mathf.Abs(position.x) < maxX;
    }

    private void TriggerNextLevel()
    {
        if (gameManager != null)
        {
            // Use the GameManager's level up method
            Debug.Log("[LevelCompletionFixer] Triggering next level");
            gameManager.LevelUp();
        }
    }
}