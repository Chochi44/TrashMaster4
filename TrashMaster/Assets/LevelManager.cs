using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] trashPrefabs;
    public GameObject[] obstaclePrefabs;
    public GameObject[] sideObstaclePrefabs;
    public GameObject islandPrefabTop;
    public GameObject islandPrefabMiddle;
    public GameObject islandPrefabBottom;

    [Header("Spawn Settings")]
    public float minObstacleSpacing = 2f;
    public float maxObstacleSpacing = 5f;
    public float sideObstacleSpacing = 3f;
    public float levelLength = 100f; // Total distance to travel in a level

    [Header("Lane Settings")]
    public int totalLanes = 7;
    public float laneWidth = 60f;

    [Header("References")]
    public Transform objectPoolParent;

    // Object pools
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<GameObject> pooledTrash = new List<GameObject>();
    private List<GameObject> pooledObstacles = new List<GameObject>();
    private List<GameObject> pooledSideObstacles = new List<GameObject>();
    private List<GameObject> pooledIslandTop = new List<GameObject>();
    private List<GameObject> pooledIslandMiddle = new List<GameObject>();
    private List<GameObject> pooledIslandBottom = new List<GameObject>();

    // Spawn tracking
    private float totalSpawnedDistance = 0f;

    private void Awake()
    {
        // Only initialize local components, not accessing GameManager
        InitializePools();
    }

    // Access GameManager in Start instead of Awake
    private void Start()
    {
        // Safe to access GameManager here
    }

    private void InitializePools()
    {
        // Pre-populate pools with some objects
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

            if (islandPrefabTop != null)
            {
                CreatePooledObject(islandPrefabTop, pooledIslandTop);
            }

            if (islandPrefabMiddle != null)
            {
                CreatePooledObject(islandPrefabMiddle, pooledIslandMiddle);
            }

            if (islandPrefabBottom != null)
            {
                CreatePooledObject(islandPrefabBottom, pooledIslandBottom);
            }
        }
    }

    private GameObject CreatePooledObject(GameObject prefab, List<GameObject> pool)
    {
        if (prefab == null || objectPoolParent == null)
        {
            Debug.LogWarning("Missing prefab or objectPoolParent for pooling");
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
        // Clear any active obstacles
        ClearActiveObstacles();

        // Reset spawn tracking
        totalSpawnedDistance = 0f;

        // Generate main lane obstacles
        int obstaclesPerLevel = level * 5;
        float spawnY = 10f; // Start spawning off-screen

        for (int i = 0; i < obstaclesPerLevel; i++)
        {
            // Randomly select lane between 1 and 5 (excluding side lanes 0 and 6)
            int lane = Random.Range(1, totalLanes - 1);

            // Spawn regular obstacle or trash
            if (Random.value < 0.3f)
            {
                SpawnObstacle(lane, spawnY);
            }
            else
            {
                SpawnTrash(lane, spawnY);
            }

            // Increase spawn distance
            float spacing = Random.Range(minObstacleSpacing, maxObstacleSpacing);
            spawnY += spacing;
            totalSpawnedDistance += spacing;

            // Increase difficulty at higher levels by adding more obstacles in parallel
            if (level > 2 && Random.value < 0.5f)
            {
                int parallelLane = Random.Range(1, totalLanes - 1);
                if (parallelLane != lane)
                {
                    if (Random.value < 0.3f)
                    {
                        SpawnObstacle(parallelLane, spawnY - spacing / 2);
                    }
                    else
                    {
                        SpawnTrash(parallelLane, spawnY - spacing / 2);
                    }
                }

                // Add even more obstacles at level 5+
                if (level > 4 && Random.value < 0.3f)
                {
                    int thirdLane = Random.Range(1, totalLanes - 1);
                    if (thirdLane != lane && thirdLane != parallelLane)
                    {
                        if (Random.value < 0.3f)
                        {
                            SpawnObstacle(thirdLane, spawnY - spacing * 0.75f);
                        }
                        else
                        {
                            SpawnTrash(thirdLane, spawnY - spacing * 0.75f);
                        }
                    }
                }
            }

            // Small chance to spawn a complex island obstacle
            if (Random.value < 0.1f)
            {
                int islandLane = Random.Range(1, totalLanes - 1);
                SpawnIsland(islandLane, spawnY + spacing);
                spawnY += spacing * 2;
                totalSpawnedDistance += spacing * 2;
            }
        }

        // Generate side obstacles - left side (lane 0)
        GenerateSideObstacles(0, levelLength);

        // Generate side obstacles - right side (lane 6 in a 7-lane setup)
        GenerateSideObstacles(totalLanes - 1, levelLength);
    }

    private void SpawnTrash(int lane, float spawnY)
    {
        if (trashPrefabs != null && trashPrefabs.Length > 0)
        {
            GameObject prefab = trashPrefabs[Random.Range(0, trashPrefabs.Length)];
            GameObject trash = GetPooledObject(pooledTrash, prefab);

            if (trash != null)
            {
                // Position at lane center
                float laneCenter = (lane * laneWidth) + (laneWidth / 2) - (totalLanes * laneWidth / 2);
                trash.transform.position = new Vector3(laneCenter, spawnY, 0);

                trash.SetActive(true);
                activeObstacles.Add(trash);
            }
        }
    }

    private void SpawnObstacle(int lane, float spawnY)
    {
        if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            GameObject obstacle = GetPooledObject(pooledObstacles, prefab);

            if (obstacle != null)
            {
                // Position at lane center
                float laneCenter = (lane * laneWidth) + (laneWidth / 2) - (totalLanes * laneWidth / 2);
                obstacle.transform.position = new Vector3(laneCenter, spawnY, 0);

                obstacle.SetActive(true);
                activeObstacles.Add(obstacle);
            }
        }
    }

    private void SpawnIsland(int lane, float spawnY)
    {
        if (islandPrefabTop != null && islandPrefabMiddle != null && islandPrefabBottom != null)
        {
            // Get island parts from pools
            GameObject top = GetPooledObject(pooledIslandTop, islandPrefabTop);

            if (top == null) return;

            int middleSectionCount = Random.Range(1, 4); // 1-3 middle sections
            GameObject[] middle = new GameObject[middleSectionCount];
            GameObject bottom = GetPooledObject(pooledIslandBottom, islandPrefabBottom);

            if (bottom == null) return;

            float laneCenter = (lane * laneWidth) + (laneWidth / 2) - (totalLanes * laneWidth / 2);
            float currentY = spawnY;

            // Position top part
            top.transform.position = new Vector3(laneCenter, currentY, 0);
            top.SetActive(true);
            activeObstacles.Add(top);
            currentY += top.GetComponent<SpriteRenderer>()?.bounds.size.y ?? 1f;

            // Position middle parts
            for (int i = 0; i < middle.Length; i++)
            {
                middle[i] = GetPooledObject(pooledIslandMiddle, islandPrefabMiddle);

                if (middle[i] == null) continue;

                middle[i].transform.position = new Vector3(laneCenter, currentY, 0);
                middle[i].SetActive(true);
                activeObstacles.Add(middle[i]);
                currentY += middle[i].GetComponent<SpriteRenderer>()?.bounds.size.y ?? 1f;
            }

            // Position bottom part
            bottom.transform.position = new Vector3(laneCenter, currentY, 0);
            bottom.SetActive(true);
            activeObstacles.Add(bottom);
        }
    }

    private void GenerateSideObstacles(int lane, float levelLength)
    {
        float spawnY = 0f;

        while (spawnY < levelLength)
        {
            if (sideObstaclePrefabs != null && sideObstaclePrefabs.Length > 0)
            {
                GameObject prefab = sideObstaclePrefabs[Random.Range(0, sideObstaclePrefabs.Length)];
                GameObject obstacle = GetPooledObject(pooledSideObstacles, prefab);

                if (obstacle != null)
                {
                    float laneCenter = (lane * laneWidth) + (laneWidth / 2) - (totalLanes * laneWidth / 2);
                    obstacle.transform.position = new Vector3(laneCenter, spawnY, 0);

                    obstacle.SetActive(true);
                    activeObstacles.Add(obstacle);

                    // Increase spawn position by obstacle height + spacing
                    spawnY += obstacle.GetComponent<SpriteRenderer>()?.bounds.size.y ?? 1f;
                    spawnY += Random.Range(1f, sideObstacleSpacing);
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

    public void MoveObstacles(float speed)
    {
        if (GameManager.Instance == null || GameManager.Instance.gameOver || !GameManager.Instance.isGameStarted)
            return;

        List<GameObject> objectsToRemove = new List<GameObject>();

        foreach (GameObject obj in activeObstacles)
        {
            if (obj != null && obj.activeInHierarchy)
            {
                // Move obstacle down
                obj.transform.position += Vector3.down * speed * Time.deltaTime;

                // Check if the obstacle has moved off screen
                if (obj.transform.position.y < -10f)
                {
                    // Check if it's a trash item that was missed
                    if (obj.CompareTag("Trash"))
                    {
                        TrashItem trashItem = obj.GetComponent<TrashItem>();
                        if (trashItem != null && !trashItem.isCollected)
                        {
                            // Now safe to use GameManager
                            GameManager.Instance?.MissTrash();
                        }
                    }

                    obj.SetActive(false);
                    objectsToRemove.Add(obj);
                }
            }
        }

        // Remove deactivated objects from active list
        foreach (GameObject obj in objectsToRemove)
        {
            activeObstacles.Remove(obj);
        }
    }

    private void ClearActiveObstacles()
    {
        foreach (GameObject obj in activeObstacles)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        activeObstacles.Clear();
    }

    public bool IsLevelComplete()
    {
        // Level is complete when all obstacles have been processed
        return activeObstacles.Count == 0;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameStarted && !GameManager.Instance.gameOver)
        {
            MoveObstacles(GameManager.Instance.currentSpeed);
            GameManager.Instance.CheckLevelCompletion();
        }
    }
}