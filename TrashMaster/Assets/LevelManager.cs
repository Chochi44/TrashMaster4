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

    [Header("Seamless Level Transition")]
    public float newLevelSpawnOffset = 5f;       // REDUCED: How far ahead to spawn new level objects (was 15f)
    public float maxSpawnDistance = 20f;         // NEW: Maximum distance from camera to spawn objects
    public bool enableSeamlessTransition = true; // NEW: Toggle for seamless mode

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

    // NEW: Seamless transition tracking
    private bool levelTransitionTriggered = false;
    private bool newLevelObjectsSpawned = false;
    private int currentLevel = 1;
    private float highestSideObjectY = 0f; // Track highest side obstacle position

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

        // For islands, create fewer pooled objects since they're complex
        if (obstaclePrefabs != null)
        {
            foreach (GameObject prefab in obstaclePrefabs)
            {
                if (prefab.name.ToLower().Contains("island") || prefab.GetComponent<IslandController>() != null)
                {
                    // Create only 2-3 island objects in pool since they're complex
                    for (int i = 0; i < 3; i++)
                    {
                        CreatePooledObject(prefab, pooledObstacles);
                    }
                }
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
        Debug.Log($"[LevelManager] ===== GenerateLevel called for level {level} =====");
        Debug.Log($"[LevelManager] enableSeamlessTransition: {enableSeamlessTransition}");
        Debug.Log($"[LevelManager] Current activeObjects count: {activeObjects.Count}");

        // Store current level
        currentLevel = level;

        // Always reset these flags when generating a new level
        levelTransitionTriggered = false;
        newLevelObjectsSpawned = false;

        // Only clear objects for first level or non-seamless mode
        if (!enableSeamlessTransition || level == 1)
        {
            Debug.Log($"[LevelManager] Clearing active objects (seamless: {enableSeamlessTransition}, level: {level})");
            ClearActiveObjects();
        }
        else
        {
            Debug.Log($"[LevelManager] Seamless mode: Keeping existing {activeObjects.Count} objects");
        }

        // Reset spawn counters
        spawnedTrashCounts.Clear();

        // Calculate total objects based on level
        int totalObjects = baseObjectCount + (level - 1) * additionalObjectsPerLevel;

        // NEW: In seamless mode, add to existing counts rather than reset
        if (enableSeamlessTransition && level > 1)
        {
            totalMainObjects += totalObjects;
            remainingMainObjects += totalObjects;
        }
        else
        {
            totalMainObjects = totalObjects;
            remainingMainObjects = totalObjects;
        }

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

        // NEW: Calculate spawn start position for seamless transition
        float spawnStartY = 10f; // Default for first level
        if (enableSeamlessTransition && level > 1)
        {
            // For seamless transition, spawn just above the visible area
            float cameraY = Camera.main ? Camera.main.transform.position.y : 0f;
            float cameraHeight = Camera.main ? Camera.main.orthographicSize : 5f;

            // Spawn just above the top of the camera view
            spawnStartY = cameraY + cameraHeight + newLevelSpawnOffset;

            // Clamp spawn distance to prevent objects from being too far away
            float maxAllowedY = cameraY + maxSpawnDistance;
            if (spawnStartY > maxAllowedY)
            {
                spawnStartY = maxAllowedY;
                Debug.Log($"[LevelManager] Clamped spawn start to max distance: {spawnStartY}");
            }

            Debug.Log($"[LevelManager] Seamless spawn start: {spawnStartY} (camera: {cameraY}, height: {cameraHeight}, offset: {newLevelSpawnOffset})");
        }
        else
        {
            Debug.Log($"[LevelManager] Standard spawn start: {spawnStartY}");
        }

        float spawnY = spawnStartY;
        float originalMaxObjectY = maxObjectY;
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
        Debug.Log($"[LevelManager] Spawning {trashCount} trash items...");
        for (int i = 0; i < trashCount && i < mainPositions.Count; i++)
        {
            SpawnTrashAt(mainPositions[i].x, mainPositions[i].y, true);
        }

        // Spawn obstacles in the remaining positions
        Debug.Log($"[LevelManager] Spawning {obstacleCount} obstacles...");
        for (int i = trashCount; i < mainPositions.Count; i++)
        {
            SpawnObstacleAt(mainPositions[i].x, mainPositions[i].y, true);
        }

        // NEW: Calculate side obstacle spawn position for seamless transition
        float sideSpawnStartY = 0f;
        if (enableSeamlessTransition && level > 1)
        {
            // In seamless mode, continue side obstacles from current camera position
            float cameraY = Camera.main ? Camera.main.transform.position.y : 0f;
            float cameraHeight = Camera.main ? Camera.main.orthographicSize : 5f;

            // Start side obstacles from just above camera view
            sideSpawnStartY = cameraY + cameraHeight;
            Debug.Log($"[LevelManager] Seamless side obstacle spawn start: {sideSpawnStartY}");
        }
        else
        {
            Debug.Log($"[LevelManager] Standard side obstacle spawn start: {sideSpawnStartY}");
        }

        // Generate side obstacles - left side (lane 0)
        Debug.Log($"[LevelManager] Generating left side obstacles from Y {sideSpawnStartY} to {spawnY + 10f}");
        GenerateSideObstacles(0, spawnY, sideSpawnStartY);

        // Generate side obstacles - right side
        int rightSideLane = (LaneManager.Instance != null) ?
            LaneManager.Instance.GetTotalLaneCount() - 1 : 6;
        Debug.Log($"[LevelManager] Generating right side obstacles from Y {sideSpawnStartY} to {spawnY + 10f}");
        GenerateSideObstacles(rightSideLane, spawnY, sideSpawnStartY);

        Debug.Log($"[LevelManager] Level {level} generated with {activeObjects.Count} total objects. Tracking {remainingMainObjects} main objects for completion.");
        Debug.Log($"[LevelManager] Spawn range: {spawnStartY} to {maxObjectY}, Total main objects: {totalMainObjects}");
        Debug.Log($"[LevelManager] Side obstacle prefabs available: {(sideObstaclePrefabs?.Length ?? 0)}");

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
        Debug.Log($"[LevelManager] SpawnTrashAt called: x={x}, y={y}, isMain={isMainObject}");

        if (trashPrefabs != null && trashPrefabs.Length > 0)
        {
            // Select a trash prefab based on current truck type
            GameObject prefab = SelectTrashPrefabByType();
            Debug.Log($"[LevelManager] Selected trash prefab: {prefab?.name}");

            GameObject trash = GetPooledObject(pooledTrash, prefab);

            if (trash != null)
            {
                trash.transform.position = new Vector3(x, y, 0);
                Debug.Log($"[LevelManager] Trash positioned at: {trash.transform.position}");

                // Make sure it has a TrashItem component
                TrashItem trashItem = trash.GetComponent<TrashItem>();
                if (trashItem == null)
                {
                    trashItem = trash.AddComponent<TrashItem>();
                    Debug.Log($"[LevelManager] Added TrashItem component to: {trash.name}");
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

                Debug.Log($"[LevelManager] Trash activated and added to activeObjects. Total active: {activeObjects.Count}");

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
                    Debug.Log($"[LevelManager] Successfully spawned {trashType} trash at {trash.transform.position}");
                }
            }
            else
            {
                Debug.LogError($"[LevelManager] Failed to get pooled trash object!");
            }
        }
        else
        {
            Debug.LogError($"[LevelManager] No trash prefabs available!");
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
                        // 70% chance: return paper trash
                        return paperTrash[Random.Range(0, paperTrash.Count)];
                    }
                    else
                    {
                        // 30% chance: return other types
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
                        // 70% chance: return plastic trash
                        return plasticTrash[Random.Range(0, plasticTrash.Count)];
                    }
                    else
                    {
                        // 30% chance: return other types
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
                        // 70% chance: return glass trash
                        return glassTrash[Random.Range(0, glassTrash.Count)];
                    }
                    else
                    {
                        // 30% chance: return other types
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
        Debug.Log($"[LevelManager] SpawnObstacleAt called: x={x}, y={y}, isMain={isMainObject}");

        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];


            // Check if this is an island prefab
            bool isIslandPrefab = prefab.name.ToLower().Contains("island") ||
                                 prefab.GetComponent<IslandController>() != null;

            // Skip island if there's already one active or if it would be too big for current level
            if (isIslandPrefab && ShouldSkipIsland())
            {
                // Select a different obstacle instead
                GameObject[] nonIslandPrefabs = System.Array.FindAll(obstaclePrefabs,
                    p => !p.name.ToLower().Contains("island") && p.GetComponent<IslandController>() == null);

                if (nonIslandPrefabs.Length > 0)
                {
                    prefab = nonIslandPrefabs[Random.Range(0, nonIslandPrefabs.Length)];
                    isIslandPrefab = false;
                }
            }

            Debug.Log($"[LevelManager] Selected obstacle prefab: {prefab?.name}");


            GameObject obstacle = GetPooledObject(pooledObstacles, prefab);

            if (obstacle != null)
            {
                obstacle.transform.position = new Vector3(x, y, 0);
                Debug.Log($"[LevelManager] Obstacle positioned at: {obstacle.transform.position}");

                if (isIslandPrefab)
                {
                    SetupIslandObstacle(obstacle, isMainObject);
                }
                else
                {
                    SetupRegularObstacle(obstacle, isMainObject);
                }

                obstacle.SetActive(true);
                activeObjects.Add(obstacle);

                Debug.Log($"[LevelManager] Obstacle activated and added to activeObjects. Total active: {activeObjects.Count}");
            }
            else
            {
                Debug.LogError($"[LevelManager] Failed to get pooled obstacle object!");
            }
        }
        else
        {
            Debug.LogError($"[LevelManager] No obstacle prefabs available!");
        }
    }


    private bool ShouldSkipIsland()
    {
        // Check if there's already an active island
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null && obj.activeInHierarchy &&
                (obj.name.ToLower().Contains("island") || obj.GetComponent<IslandController>() != null))
            {
                return true; // Skip - already have an island
            }
        }

        // Also consider screen height limitations
        float screenHeight = Camera.main ? Camera.main.orthographicSize * 2 : 10f;
        float maxIslandHeight = screenHeight * 0.6f; // Island shouldn't exceed 60% of screen height

        // You can add more logic here based on current level, etc.
        return false;
    }

    private void SetupIslandObstacle(GameObject obstacle, bool isMainObject)
    {
        IslandController controller = obstacle.GetComponent<IslandController>();
        if (controller != null)
        {
            // Randomize middle part count based on screen size
            float screenHeight = Camera.main ? Camera.main.orthographicSize * 2 : 10f;
            float maxIslandHeight = screenHeight * 0.5f; // 50% of screen height max

            // Estimate heights (you may need to adjust these values)
            float estimatedPartHeight = 0.8f; // Approximate height of each part
            int maxMiddleParts = Mathf.Max(0, Mathf.FloorToInt(maxIslandHeight / estimatedPartHeight) - 2); // -2 for top and bottom

            // Randomize but clamp to reasonable size
            controller.middlePartCount = Random.Range(0, Mathf.Min(maxMiddleParts + 1, 4)); // Max 4 middle parts

            controller.SetMainObjectParts(isMainObject);
        }

        // Make sure it's tagged properly
        obstacle.tag = "Obstacle";
    }

    private void SetupRegularObstacle(GameObject obstacle, bool isMainObject)
    {
        // Your existing regular obstacle setup code
        ObstacleItem obstacleItem = obstacle.GetComponent<ObstacleItem>();
        if (obstacleItem == null)
        {
            obstacleItem = obstacle.AddComponent<ObstacleItem>();
        }

        obstacleItem.isMainObject = isMainObject;
        obstacle.tag = "Obstacle";

        // Setup collider as before...
    }

    private void GenerateSideObstacles(int lane, float maxSpawnY)

    private void GenerateSideObstacles(int lane, float maxSpawnY, float startY = 0f)

    {
        Debug.Log($"[LevelManager] GenerateSideObstacles: lane {lane}, startY {startY}, maxSpawnY {maxSpawnY}");

        float spawnY = startY;
        int sideObstacleCount = 0;

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
                    float obstacleHeight = 1f; // Default height
                    if (sr != null)
                    {
                        obstacleHeight = sr.bounds.size.y;

                        // Set collider size
                        if (collider.size.x <= 0.001f || collider.size.y <= 0.001f)
                        {
                            collider.size = sr.bounds.size;
                        }
                    }

                    // FIXED: Better spacing calculation to prevent stacking
                    float spacing = Mathf.Max(sideObstacleSpacing, obstacleHeight + 1f); // Ensure minimum gap
                    spawnY += obstacleHeight + Random.Range(spacing * 0.5f, spacing * 1.5f);

                    // NEW: Track highest side obstacle position
                    highestSideObjectY = Mathf.Max(highestSideObjectY, spawnY);

                    // Activate and track
                    obstacle.SetActive(true);
                    activeObjects.Add(obstacle);
                    sideObstacleCount++;

                    Debug.Log($"[LevelManager] Side obstacle spawned at {obstacle.transform.position}, next spawn Y: {spawnY}");
                }
                else
                {
                    // FIXED: Use proper spacing even when no object spawned
                    spawnY += sideObstacleSpacing;
                }
            }
            else
            {
                Debug.LogError($"[LevelManager] No side obstacle prefabs available!");
                break;
            }
        }

        Debug.Log($"[LevelManager] Generated {sideObstacleCount} side obstacles for lane {lane}");
    }

    public void MoveObjects(float speed)
    {
        if (GameManager.Instance == null || GameManager.Instance.gameOver || !GameManager.Instance.isGameStarted)
            return;

        // NEW: Get the actual lane scroll speed to sync objects with road
        float laneScrollSpeed = speed;
        if (LaneManager.Instance != null)
        {
            laneScrollSpeed = LaneManager.Instance.GetScrollSpeed();
        }

        List<GameObject> objectsToRemove = new List<GameObject>();
        int mainObjectsProcessed = 0;
        bool foundActiveMainObject = false;

        // Cache player position for missed trash detection
        float playerY = playerTransform != null ? playerTransform.position.y : -5f;

        foreach (GameObject obj in activeObjects)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                // Move ALL objects at the exact same synchronized speed
                obj.transform.position += Vector3.down * laneScrollSpeed * Time.deltaTime;

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

                // Separate penalty application from visual disappearance
                // Check if trash passed behind player (for penalty application)
                if (trashItem != null && !trashItem.isCollected && obj.transform.position.y < playerY)
                {
                    // Call the new method that applies penalty but doesn't deactivate
                    trashItem.PassedBehindPlayer();
                    // Don't add to objectsToRemove yet - let it continue to bottom of screen
                }

                // Check if the object has moved completely off screen (bottom)
                if (obj.transform.position.y < -10f)
                {
                    // Count if it's a main object for level completion
                    bool isMain = false;

                    if (trashItem != null)
                    {
                        isMain = trashItem.isMainObject;

                        // Call OffScreen method to handle final cleanup
                        trashItem.OffScreen();
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

        // NEW: Check if all main objects have passed and trigger level progression
        if (!levelTransitionTriggered && !foundActiveMainObject && remainingMainObjects <= 0)
        {
            levelTransitionTriggered = true;
            Debug.Log("[LevelManager] ===== ALL MAIN OBJECTS PASSED - TRIGGERING LEVEL PROGRESSION =====");
            Debug.Log($"[LevelManager] Current level: {currentLevel}, enableSeamlessTransition: {enableSeamlessTransition}");

            if (enableSeamlessTransition)
            {
                // In seamless mode, generate new level immediately, then notify GameManager
                int nextLevel = currentLevel + 1;
                Debug.Log($"[LevelManager] Seamless mode: Generating level {nextLevel} objects immediately...");
                Debug.Log($"[LevelManager] Current maxObjectY before generation: {maxObjectY}");

                GenerateLevel(nextLevel);

                Debug.Log($"[LevelManager] After generation - activeObjects: {activeObjects.Count}, maxObjectY: {maxObjectY}");

                // Then notify GameManager for UI updates, truck changes, etc.
                if (GameManager.Instance != null)
                {
                    Debug.Log("[LevelManager] Notifying GameManager for UI updates...");
                    GameManager.Instance.CheckLevelCompletion();
                }
            }
            else
            {
                // In traditional mode, let GameManager handle everything
                Debug.Log("[LevelManager] Traditional mode: Letting GameManager handle level completion...");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckLevelCompletion();
                }
            }
        }

        // Debug info
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[LevelManager] Objects: {activeObjects.Count} active, {remainingMainObjects}/{totalMainObjects} main remaining");

            if (levelTransitionTriggered)
            {
                Debug.Log($"[LevelManager] Level transition active - seamless road continuing...");
            }
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
        levelTransitionTriggered = false;
        newLevelObjectsSpawned = false;

        // Only reset these if we're actually clearing everything
        maxObjectY = 0f;
        highestSideObjectY = 0f;

        Debug.Log("[LevelManager] All active objects cleared and counters reset");
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

        // NEW: In seamless mode, level completes immediately when main objects are done
        if (enableSeamlessTransition && levelTransitionTriggered)
        {
            Debug.Log("[LevelManager] Level complete! (Seamless transition mode)");
            return true;
        }

        // Fallback: Traditional completion check
        if (remainingMainObjects <= 0)
        {
            Debug.Log("[LevelManager] Level complete! (Traditional method)");
            return true;
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
            // NEW: Always use lane scroll speed to keep objects synchronized with road
            float speed = GameManager.Instance.currentSpeed;
            if (LaneManager.Instance != null)
            {
                speed = LaneManager.Instance.GetScrollSpeed();
            }

            MoveObjects(speed);

            // In seamless mode, don't use traditional level completion check
            // The level advances immediately when main objects are done
            if (!enableSeamlessTransition && IsLevelComplete())
            {
                // Reset flags for non-seamless mode
                levelTransitionTriggered = false;
                newLevelObjectsSpawned = false;

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