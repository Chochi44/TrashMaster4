using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] trashPrefabs;
    public GameObject[] obstaclePrefabs;
    public GameObject[] sideObstaclePrefabs;

    [Header("Spawn Settings")]
    public float minObstacleSpacing = 0.2f;      // Extremely close spacing
    public float maxObstacleSpacing = 0.8f;      // Extremely close spacing
    public float sideObstacleSpacing = 3f;       // Reverted to original spacing

    [Header("Level Settings")]
    public float trashRatio = 0.6f;              // Changed from 0.7f to 0.6f (60% trash, 40% obstacles)
    public int baseObjectCount = 15;             // Changed from 10 to 15
    public int additionalObjectsPerLevel = 7;    // Increased from 5 to 7 (roughly maintains ratio)

    [Header("Level Completion")]
    public float levelCompletionDelay = 3f;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public bool forceNextLevel = false; // Set this to true in inspector to force next level

    [Header("References")]
    public Transform objectPoolParent;
    public Transform playerTransform;

    // Object pools
    private List<GameObject> activeObjects = new List<GameObject>();
    private List<GameObject> pooledTrash = new List<GameObject>();
    private List<GameObject> pooledObstacles = new List<GameObject>();
    private List<GameObject> pooledSideObstacles = new List<GameObject>();

    // Level tracking
    private int totalMainObjects = 0;
    private int remainingMainObjects = 0;
    private float maxObjectY = 0f; // Track the highest Y position of any spawned object
    private bool levelCompleting = false;
    private float levelCheckTimer = 0f;
    private float levelCheckDelay = 3f;

    // Store the last non-side object position
    private float highestObjectY = -100f;

    // Track if all objects have passed player
    private bool allObjectsPassedPlayer = false;
    private float allObjectsPassedTime = 0f;

    // Add counters to track what we actually spawn
    private Dictionary<string, int> spawnedTrashCounts = new Dictionary<string, int>();

    private void Awake()
    {
        // Create pool parent if needed
        if (objectPoolParent == null)
        {
            GameObject poolParent = new GameObject("ObjectPool");
            objectPoolParent = poolParent.transform;
        }

        // Initialize object pools
        InitializePools();
    }

    private void Start()
    {
        // Find player if not assigned
        if (playerTransform == null)
        {
            PlayerController player = FindObjectOfType<PlayerController>();
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    private void InitializePools()
    {
        // Pre-populate pools with some objects
        if (trashPrefabs == null || trashPrefabs.Length == 0)
        {
            Debug.LogError("No trash prefabs assigned to LevelManager!");
        }

        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogError("No obstacle prefabs assigned to LevelManager!");
        }

        for (int i = 0; i < 20; i++)
        {
            if (trashPrefabs != null && trashPrefabs.Length > 0)
            {
                CreatePooledObject(trashPrefabs[Random.Range(0, trashPrefabs.Length)], pooledTrash);
            }

            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
            {
                CreatePooledObject(obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)], pooledObstacles);
            }

            if (sideObstaclePrefabs != null && sideObstaclePrefabs.Length > 0)
            {
                CreatePooledObject(sideObstaclePrefabs[Random.Range(0, sideObstaclePrefabs.Length)], pooledSideObstacles);
            }
        }
    }

    private GameObject CreatePooledObject(GameObject prefab, List<GameObject> pool)
    {
        if (prefab == null)
        {
            Debug.LogWarning("Null prefab passed to CreatePooledObject");
            return null;
        }

        if (objectPoolParent == null)
        {
            Debug.LogWarning("No objectPoolParent for pooling");
            return null;
        }

        GameObject obj = Instantiate(prefab, objectPoolParent);
        obj.SetActive(false);
        pool.Add(obj);

        return obj;
    }

    private GameObject GetPooledObject(List<GameObject> pool, GameObject prefab)
    {
        // Try to find an inactive object in the pool
        foreach (GameObject obj in pool)
        {
            if (obj != null && !obj.activeInHierarchy)
            {
                return obj;
            }
        }

        // If no inactive object found, create a new one
        return CreatePooledObject(prefab, pool);
    }

    public void GenerateLevel(int level)
    {
        // Reset level completion flags and counters
        levelCompleting = false;
        levelCheckTimer = 0f;
        highestObjectY = -100f;
        allObjectsPassedPlayer = false;
        allObjectsPassedTime = 0f;

        // Reset spawn counters
        spawnedTrashCounts.Clear();

        // Clear any active objects
        ClearActiveObjects();

        // Calculate total objects based on level
        int totalObjects = baseObjectCount + (level - 1) * additionalObjectsPerLevel;
        totalMainObjects = totalObjects;
        remainingMainObjects = totalObjects;

        // Calculate trash vs obstacles ratio
        int trashCount = Mathf.RoundToInt(totalObjects * trashRatio);
        int obstacleCount = totalObjects - trashCount;

        Debug.Log($"[LevelManager] Generating level {level} with {trashCount} trash items and {obstacleCount} obstacles (total: {totalObjects})");

        // Get the total lane count (excluding side lanes)
        int totalLanes = 5; // Default
        if (LaneManager.Instance != null)
        {
            totalLanes = LaneManager.Instance.GetCenterLaneCount();
        }

        // Start spawning from outside the visible screen
        float spawnY = 10f;
        maxObjectY = spawnY;

        // List to track where we'll spawn trash and obstacles
        List<Vector2> mainPositions = new List<Vector2>();

        // Generate main lane positions with closer spacing
        for (int i = 0; i < totalObjects; i++)
        {
            // Randomly select lane (excluding side lanes)
            int lane = Random.Range(1, totalLanes + 1);

            // Get lane position
            float xPos = 0;
            if (LaneManager.Instance != null)
            {
                xPos = LaneManager.Instance.GetLanePosition(lane).x;
            }
            else
            {
                xPos = (lane - totalLanes / 2) * 1.5f;
            }

            mainPositions.Add(new Vector2(xPos, spawnY));

            // Track highest Y position
            if (spawnY > highestObjectY)
            {
                highestObjectY = spawnY;
            }

            // Increase spawn distance for next object (reduced spacing)
            float spacing = Random.Range(minObstacleSpacing, maxObstacleSpacing);
            spawnY += spacing;
            maxObjectY = Mathf.Max(maxObjectY, spawnY);
        }

        // Shuffle the positions
        for (int i = 0; i < mainPositions.Count; i++)
        {
            int randomIndex = Random.Range(i, mainPositions.Count);
            Vector2 temp = mainPositions[i];
            mainPositions[i] = mainPositions[randomIndex];
            mainPositions[randomIndex] = temp;
        }

        // Spawn trash in the first trashCount positions
        for (int i = 0; i < trashCount && i < mainPositions.Count; i++)
        {
            SpawnTrashAt(mainPositions[i].x, mainPositions[i].y, true);
        }

        // Spawn obstacles in the remaining positions
        for (int i = trashCount; i < mainPositions.Count; i++)
        {
            SpawnObstacleAt(mainPositions[i].x, mainPositions[i].y, true);
        }

        // Generate side obstacles - left side (lane 0)
        GenerateSideObstacles(0, spawnY);

        // Generate side obstacles - right side
        int rightSideLane = (LaneManager.Instance != null) ?
            LaneManager.Instance.GetTotalLaneCount() - 1 : 6;
        GenerateSideObstacles(rightSideLane, spawnY);

        Debug.Log($"[LevelManager] Level {level} generated with {activeObjects.Count} total objects. Tracking {remainingMainObjects} main objects for completion.");

        // Log actual spawned trash distribution
        Debug.Log($"[LevelManager] ACTUAL SPAWNED TRASH DISTRIBUTION:");
        foreach (var kvp in spawnedTrashCounts)
        {
            float percentage = (float)kvp.Value / trashCount * 100f;
            Debug.Log($"[LevelManager]   {kvp.Key}: {kvp.Value}/{trashCount} ({percentage:F1}%)");
        }
    }

    private void SpawnTrashAt(float x, float y, bool isMainObject)
    {
        if (trashPrefabs != null && trashPrefabs.Length > 0)
        {
            // Select a trash prefab based on current truck type
            GameObject prefab = SelectTrashPrefabByType();
            GameObject trash = GetPooledObject(pooledTrash, prefab);

            if (trash != null)
            {
                trash.transform.position = new Vector3(x, y, 0);

                // Make sure it has a TrashItem component
                TrashItem trashItem = trash.GetComponent<TrashItem>();
                if (trashItem == null)
                {
                    trashItem = trash.AddComponent<TrashItem>();
                }

                // IMPORTANT: Copy the trash type flags from the selected prefab
                TrashItem prefabItem = prefab.GetComponent<TrashItem>();
                if (prefabItem != null)
                {
                    trashItem.isPaper = prefabItem.isPaper;
                    trashItem.isPlastic = prefabItem.isPlastic;
                    trashItem.isGlass = prefabItem.isGlass;
                    trashItem.isGeneral = prefabItem.isGeneral;
                    trashItem.pointValue = prefabItem.pointValue;
                }

                // IMPORTANT: Update the visual sprite to match the selected prefab
                SpriteRenderer trashSpriteRenderer = trash.GetComponent<SpriteRenderer>();
                SpriteRenderer prefabSpriteRenderer = prefab.GetComponent<SpriteRenderer>();
                if (trashSpriteRenderer != null && prefabSpriteRenderer != null)
                {
                    trashSpriteRenderer.sprite = prefabSpriteRenderer.sprite;
                    trashSpriteRenderer.color = prefabSpriteRenderer.color;
                }

                // Set this flag to help with level completion tracking
                trashItem.isMainObject = isMainObject;
                trashItem.isCollected = false;

                // Make sure it's tagged properly
                trash.tag = "Trash";

                // Ensure collider is correctly set
                BoxCollider2D collider = trash.GetComponent<BoxCollider2D>();
                if (collider == null)
                {
                    collider = trash.AddComponent<BoxCollider2D>();
                }
                collider.isTrigger = true;

                // Set collider size from sprite if needed
                SpriteRenderer sr = trash.GetComponent<SpriteRenderer>();
                if (sr != null && (collider.size.x <= 0.001f || collider.size.y <= 0.001f))
                {
                    collider.size = sr.sprite.bounds.size;
                }

                // Activate and track
                trash.SetActive(true);
                activeObjects.Add(trash);

                // Count what type we actually spawned
                TrashItem spawnedItem = trash.GetComponent<TrashItem>();
                if (spawnedItem != null)
                {
                    string trashType = "Unknown";
                    if (spawnedItem.isPaper) trashType = "Paper";
                    else if (spawnedItem.isPlastic) trashType = "Plastic";
                    else if (spawnedItem.isGlass) trashType = "Glass";
                    else if (spawnedItem.isGeneral) trashType = "General";

                    if (!spawnedTrashCounts.ContainsKey(trashType))
                        spawnedTrashCounts[trashType] = 0;
                    spawnedTrashCounts[trashType]++;

                    // Debug log to verify the fix
                    Debug.Log($"[LevelManager] Spawned {trashType} trash from prefab {prefab.name} with sprite {trashSpriteRenderer?.sprite?.name}");
                }
            }
        }
    }

    private GameObject SelectTrashPrefabByType()
    {
        // Default behavior: random selection if no GameManager or no prefabs
        if (GameManager.Instance == null || trashPrefabs.Length == 0)
        {
            return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
        }

        // Get current truck type
        GameManager.TruckType currentType = GameManager.Instance.currentTruckType;

        // List all available trash prefabs by type
        List<GameObject> paperTrash = new List<GameObject>();
        List<GameObject> plasticTrash = new List<GameObject>();
        List<GameObject> glassTrash = new List<GameObject>();
        List<GameObject> generalTrash = new List<GameObject>();

        // Categorize trash prefabs
        foreach (GameObject prefab in trashPrefabs)
        {
            if (prefab == null) continue;

            TrashItem item = prefab.GetComponent<TrashItem>();
            if (item != null)
            {
                if (item.isPaper) paperTrash.Add(prefab);
                else if (item.isPlastic) plasticTrash.Add(prefab);
                else if (item.isGlass) glassTrash.Add(prefab);
                else if (item.isGeneral) generalTrash.Add(prefab);
            }
        }

        // NEW: Use 70% probability for matching type when truck is specialized
        float matchingTypeChance = 0.7f; // 70% chance for specific type
        float randomValue = Random.value;

        // Select trash based on current truck type with higher probability for matching type
        switch (currentType)
        {
            case GameManager.TruckType.Paper:
                if (paperTrash.Count > 0)
                {
                    if (randomValue < matchingTypeChance)
                    {
                        // 98% chance: return paper trash
                        return paperTrash[Random.Range(0, paperTrash.Count)];
                    }
                    else
                    {
                        // 2% chance: return other types
                        List<GameObject> otherTypes = new List<GameObject>();
                        otherTypes.AddRange(plasticTrash);
                        otherTypes.AddRange(glassTrash);
                        otherTypes.AddRange(generalTrash);
                        if (otherTypes.Count > 0)
                            return otherTypes[Random.Range(0, otherTypes.Count)];
                        else
                            return paperTrash[Random.Range(0, paperTrash.Count)];
                    }
                }
                break;

            case GameManager.TruckType.Plastic:
                if (plasticTrash.Count > 0)
                {
                    if (randomValue < matchingTypeChance)
                    {
                        // 98% chance: return plastic trash
                        return plasticTrash[Random.Range(0, plasticTrash.Count)];
                    }
                    else
                    {
                        // 2% chance: return other types
                        List<GameObject> otherTypes = new List<GameObject>();
                        otherTypes.AddRange(paperTrash);
                        otherTypes.AddRange(glassTrash);
                        otherTypes.AddRange(generalTrash);
                        if (otherTypes.Count > 0)
                            return otherTypes[Random.Range(0, otherTypes.Count)];
                        else
                            return plasticTrash[Random.Range(0, plasticTrash.Count)];
                    }
                }
                break;

            case GameManager.TruckType.Glass:
                if (glassTrash.Count > 0)
                {
                    if (randomValue < matchingTypeChance)
                    {
                        // 98% chance: return glass trash
                        return glassTrash[Random.Range(0, glassTrash.Count)];
                    }
                    else
                    {
                        // 2% chance: return other types
                        List<GameObject> otherTypes = new List<GameObject>();
                        otherTypes.AddRange(paperTrash);
                        otherTypes.AddRange(plasticTrash);
                        otherTypes.AddRange(generalTrash);
                        if (otherTypes.Count > 0)
                            return otherTypes[Random.Range(0, otherTypes.Count)];
                        else
                            return glassTrash[Random.Range(0, glassTrash.Count)];
                    }
                }
                break;

            case GameManager.TruckType.General:
                // For general truck, distribute evenly among all types
                List<GameObject> allTrashTypes = new List<GameObject>();
                allTrashTypes.AddRange(paperTrash);
                allTrashTypes.AddRange(plasticTrash);
                allTrashTypes.AddRange(glassTrash);
                allTrashTypes.AddRange(generalTrash);

                if (allTrashTypes.Count > 0)
                    return allTrashTypes[Random.Range(0, allTrashTypes.Count)];
                break;
        }

        // Final fallback if something went wrong
        return trashPrefabs[Random.Range(0, trashPrefabs.Length)];
    }

    private void SpawnObstacleAt(float x, float y, bool isMainObject)
    {
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            GameObject obstacle = GetPooledObject(pooledObstacles, prefab);

            if (obstacle != null)
            {
                obstacle.transform.position = new Vector3(x, y, 0);

                // Check if this is a complex obstacle (Island)
                IslandController islandController = obstacle.GetComponent<IslandController>();
                if (islandController != null)
                {
                    // This is a complex obstacle, set it up
                    islandController.SetMainObjectParts(isMainObject);

                    // Make sure the entire island is positioned correctly
                    islandController.SetPosition(new Vector3(x, y, 0));
                }
                else
                {
                    // Regular obstacle, add the normal ObstacleItem component
                    ObstacleItem obstacleItem = obstacle.GetComponent<ObstacleItem>();
                    if (obstacleItem == null)
                    {
                        obstacleItem = obstacle.AddComponent<ObstacleItem>();
                    }

                    // Set this flag to help with level completion tracking
                    obstacleItem.isMainObject = isMainObject;
                }

                // Make sure it's tagged properly
                obstacle.tag = "Obstacle";

                // Ensure collider is correctly set if not a complex obstacle
                if (islandController == null)
                {
                    BoxCollider2D collider = obstacle.GetComponent<BoxCollider2D>();
                    if (collider == null)
                    {
                        collider = obstacle.AddComponent<BoxCollider2D>();
                    }
                    collider.isTrigger = true;

                    // Set collider size from sprite if needed
                    SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();
                    if (sr != null && (collider.size.x <= 0.001f || collider.size.y <= 0.001f))
                    {
                        collider.size = sr.sprite.bounds.size;
                    }
                }

                // Activate and track
                obstacle.SetActive(true);
                activeObjects.Add(obstacle);
            }
        }
    }

    private void GenerateSideObstacles(int lane, float maxSpawnY)
    {
        float spawnY = 0f;

        while (spawnY < maxSpawnY + 10f) // Extra for offscreen content
        {
            if (sideObstaclePrefabs != null && sideObstaclePrefabs.Length > 0)
            {
                GameObject prefab = sideObstaclePrefabs[Random.Range(0, sideObstaclePrefabs.Length)];
                GameObject obstacle = GetPooledObject(pooledSideObstacles, prefab);

                if (obstacle != null)
                {
                    // Get lane position
                    float xPos = 0;
                    if (LaneManager.Instance != null)
                    {
                        xPos = LaneManager.Instance.GetLanePosition(lane).x;
                    }
                    else
                    {
                        xPos = (lane == 0) ? -5f : 5f;
                    }

                    obstacle.transform.position = new Vector3(xPos, spawnY, 0);

                    // Make sure it has correct component
                    ObstacleItem obstacleItem = obstacle.GetComponent<ObstacleItem>();
                    if (obstacleItem == null)
                    {
                        obstacleItem = obstacle.AddComponent<ObstacleItem>();
                    }

                    // Mark as not a main object
                    obstacleItem.isMainObject = false;

                    // Tag properly
                    obstacle.tag = "Obstacle";

                    // Ensure collider is set
                    BoxCollider2D collider = obstacle.GetComponent<BoxCollider2D>();
                    if (collider == null)
                    {
                        collider = obstacle.AddComponent<BoxCollider2D>();
                    }
                    collider.isTrigger = true;

                    // Set size from sprite if needed
                    SpriteRenderer sr = obstacle.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // Add spacing based on obstacle height (normal side spacing)
                        float height = sr.bounds.size.y;
                        spawnY += height + Random.Range(1f, sideObstacleSpacing);

                        // Set collider size
                        if (collider.size.x <= 0.001f || collider.size.y <= 0.001f)
                        {
                            collider.size = sr.bounds.size;
                        }
                    }
                    else
                    {
                        spawnY += sideObstacleSpacing;
                    }

                    // Activate and track
                    obstacle.SetActive(true);
                    activeObjects.Add(obstacle);
                }
                else
                {
                    spawnY += sideObstacleSpacing;
                }
            }
            else
            {
                spawnY += sideObstacleSpacing;
            }
        }
    }

    public void MoveObjects(float speed)
    {
        if (GameManager.Instance == null || GameManager.Instance.gameOver || !GameManager.Instance.isGameStarted)
            return;

        List<GameObject> objectsToRemove = new List<GameObject>();
        int mainObjectsProcessed = 0;
        bool foundActiveMainObject = false;

        // Cache player position for missed trash detection
        float playerY = playerTransform != null ? playerTransform.position.y : -5f;

        foreach (GameObject obj in activeObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                // Move object down
                obj.transform.position += Vector3.down * speed * Time.deltaTime;

                // Check if this is a main object and if it's above the player
                bool isMainObject = false;
                TrashItem trashItem = obj.GetComponent<TrashItem>();
                if (trashItem != null)
                {
                    isMainObject = trashItem.isMainObject;
                }
                else
                {
                    ObstacleItem obstacleItem = obj.GetComponent<ObstacleItem>();
                    if (obstacleItem != null)
                    {
                        isMainObject = obstacleItem.isMainObject;
                    }
                }

                // Track if any main objects are still active and above the player
                if (isMainObject && obj.transform.position.y > playerY)
                {
                    foundActiveMainObject = true;
                }

                // Check if trash was missed (passed player position)
                if (trashItem != null && !trashItem.isCollected && obj.transform.position.y < playerY)
                {
                    // Only count for main objects
                    if (trashItem.isMainObject)
                    {
                        mainObjectsProcessed++;
                        remainingMainObjects--;
                    }

                    trashItem.Missed();
                    objectsToRemove.Add(obj);
                    continue;
                }

                // Check if the object has moved off screen
                if (obj.transform.position.y < -10f)
                {
                    // Count if it's a main object
                    bool isMain = false;

                    if (trashItem != null)
                    {
                        isMain = trashItem.isMainObject;
                    }
                    else
                    {
                        ObstacleItem obstacleItem = obj.GetComponent<ObstacleItem>();
                        if (obstacleItem != null)
                        {
                            isMain = obstacleItem.isMainObject;

                            // Notify obstacle that it was passed
                            obstacleItem.ObstaclePassed();
                        }
                    }

                    if (isMain)
                    {
                        mainObjectsProcessed++;
                        remainingMainObjects--;
                    }

                    obj.SetActive(false);
                    objectsToRemove.Add(obj);
                }
            }
        }

        // Remove deactivated objects from active list
        foreach (GameObject obj in objectsToRemove)
        {
            activeObjects.Remove(obj);
        }

        // Check if all main objects have passed the player
        if (!foundActiveMainObject && !allObjectsPassedPlayer && remainingMainObjects <= 0)
        {
            allObjectsPassedPlayer = true;
            allObjectsPassedTime = Time.time;
            Debug.Log("[LevelManager] All main objects have passed the player. Starting level completion countdown.");
        }

        // Debug info
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[LevelManager] Objects: {activeObjects.Count} active, {remainingMainObjects}/{totalMainObjects} main remaining");
        }

        // Check if level is nearly complete - traditional method
        if (!levelCompleting && remainingMainObjects <= 0)
        {
            Debug.Log("[LevelManager] All main objects processed. Starting level completion countdown.");
            levelCompleting = true;
            levelCheckTimer = 0f;
        }
    }

    private void ClearActiveObjects()
    {
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        activeObjects.Clear();
        remainingMainObjects = 0;
        totalMainObjects = 0;
        allObjectsPassedPlayer = false;
        allObjectsPassedTime = 0f;
    }

    public bool IsLevelComplete()
    {
        // Manual override for testing
        if (forceNextLevel)
        {
            forceNextLevel = false;
            Debug.Log("[LevelManager] FORCED level completion");
            return true;
        }

        // New method: all objects passed player
        if (allObjectsPassedPlayer && Time.time - allObjectsPassedTime >= levelCompletionDelay)
        {
            Debug.Log("[LevelManager] Level complete! All objects have passed the player.");
            return true;
        }

        // Backup method: check remaining objects
        if (levelCompleting)
        {
            levelCheckTimer += Time.deltaTime;

            if (levelCheckTimer >= levelCheckDelay)
            {
                Debug.Log("[LevelManager] Level complete! All objects processed.");
                return true;
            }
        }

        return false;
    }

    // Method for TrashItem to notify when collected
    public void NotifyObjectProcessed(bool isMainObject)
    {
        if (isMainObject)
        {
            remainingMainObjects--;

            if (showDebugInfo)
            {
                Debug.Log($"[LevelManager] Main object processed, {remainingMainObjects}/{totalMainObjects} remain");
            }
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameStarted && !GameManager.Instance.gameOver)
        {
            // Move objects at same speed as road scrolling
            float speed = GameManager.Instance.currentSpeed;
            if (LaneManager.Instance != null)
            {
                speed = LaneManager.Instance.GetScrollSpeed();
            }

            MoveObjects(speed);

            // Check if level is complete
            if (IsLevelComplete())
            {
                // Reset flags
                levelCompleting = false;
                allObjectsPassedPlayer = false;
                allObjectsPassedTime = 0f;

                // Notify game manager
                GameManager.Instance.CheckLevelCompletion();
            }
        }

        // For debugging
        if (Input.GetKeyDown(KeyCode.F5))
        {
            forceNextLevel = true;
        }
    }
}