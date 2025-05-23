using UnityEngine;
using UnityEngine.UI;

public class SimpleScrollingTextManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float textSpawnY = 15f;
    public float textLifetime = 30f;

    [Header("Text Colors")]
    public Color generalTypeColor = Color.white;
    public Color paperTypeColor = Color.blue;
    public Color plasticTypeColor = Color.yellow;
    public Color glassTypeColor = Color.cyan;
    public Color levelUpColor = Color.green;

    [Header("Debug")]
    public bool debugMode = true;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        if (debugMode)
        {
            Debug.Log("[SimpleScrollingTextManager] Started - ready to show level and truck type texts.");
        }
    }

    public void ShowLevelUpText(int level)
    {
        GameObject textObj = CreateSimpleText("LEVEL " + level, levelUpColor, 13); // Larger base font size

        // Position at spawn location
        float spawnY = GetSpawnPosition();
        float centerX = GetCenterX();
        textObj.transform.position = new Vector3(centerX, spawnY, 0);

        Debug.Log($"[SimpleScrollingTextManager] Level text created at {textObj.transform.position}");
    }

    public void ShowTruckTypeText(GameManager.TruckType truckType)
    {
        string typeText = "";
        Color textColor = Color.white;

        switch (truckType)
        {
            case GameManager.TruckType.General:
                typeText = "GENERAL TRUCK";
                textColor = generalTypeColor;
                break;
            case GameManager.TruckType.Paper:
                typeText = "PAPER TRUCK";
                textColor = paperTypeColor;
                break;
            case GameManager.TruckType.Plastic:
                typeText = "PLASTIC TRUCK";
                textColor = plasticTypeColor;
                break;
            case GameManager.TruckType.Glass:
                typeText = "GLASS TRUCK";
                textColor = glassTypeColor;
                break;
        }

        GameObject textObj = CreateSimpleText(typeText, textColor, 12); // Smaller font size for truck type

        // Position at spawn location, offset below level text
        float spawnY = GetSpawnPosition() - 1.5f;
        float centerX = GetCenterX();
        textObj.transform.position = new Vector3(centerX, spawnY, 0);

        Debug.Log($"[SimpleScrollingTextManager] Truck type text created at {textObj.transform.position}");
    }

    public void ShowLevelUpAndTruckType(int level, GameManager.TruckType truckType)
    {
        ShowLevelUpText(level);
        ShowTruckTypeText(truckType);
    }

    private GameObject CreateSimpleText(string text, Color color, float fontSize)
    {
        // Create GameObject
        GameObject textObj = new GameObject("ScrollingText_" + text.Replace(" ", "_"));

        // Add Text Mesh component (not UI Text)
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.fontSize = (int)fontSize;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Add MeshRenderer for proper sorting
        MeshRenderer renderer = textObj.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 6; // Above road, below truck

            // Create a new material to avoid sharing and improve clarity
            Material textMaterial = new Material(textMesh.font.material);
            textMaterial.color = color;
            renderer.material = textMaterial;
        }

        // Calculate road width and scale text to fit across most of it
        float roadWidth = CalculateRoadWidth();
        float targetWidth = roadWidth * 0.6f; // Use 60% of road width for better readability

        // Calculate scale based on text length and target width
        // Use a more conservative scaling approach for better readability
        float textLength = text.Length;
        float baseCharWidth = 0.5f; // More conservative character width estimate
        float scaleX = targetWidth / (textLength * baseCharWidth);

        // Clamp the scale to avoid too much distortion
        scaleX = Mathf.Clamp(scaleX, 0.3f, 1.5f);

        // Keep Y scale proportional but not too stretched
        float scaleY = scaleX * 0.9f;

        textObj.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        // Add scrolling component
        SimpleScrollingText scrolling = textObj.AddComponent<SimpleScrollingText>();
        scrolling.SetLifetime(textLifetime);

        Debug.Log($"[SimpleScrollingTextManager] Created TextMesh: '{text}' with scale {textObj.transform.localScale}, road width: {roadWidth}");

        return textObj;
    }

    private float CalculateRoadWidth()
    {
        if (LaneManager.Instance != null)
        {
            // Get the positions of the leftmost and rightmost center lanes
            int totalLanes = LaneManager.Instance.GetTotalLaneCount();
            int leftmostLane = 1; // First center lane (after left side lane)
            int rightmostLane = totalLanes - 2; // Last center lane (before right side lane)

            float leftX = LaneManager.Instance.GetLanePosition(leftmostLane).x;
            float rightX = LaneManager.Instance.GetLanePosition(rightmostLane).x;
            float laneWidth = LaneManager.Instance.GetLaneWidth(leftmostLane);

            // Calculate total road width including half a lane on each side
            float roadWidth = (rightX - leftX) + laneWidth;

            Debug.Log($"[SimpleScrollingTextManager] Road width calculated: {roadWidth} (from lane {leftmostLane} to {rightmostLane})");
            return roadWidth;
        }

        // Fallback if no LaneManager
        return 6f; // Default road width
    }

    private float GetSpawnPosition()
    {
        float cameraY = 0f;
        float cameraHeight = 5f;

        if (mainCamera != null)
        {
            cameraY = mainCamera.transform.position.y;
            cameraHeight = mainCamera.orthographicSize;
        }

        return cameraY + cameraHeight + textSpawnY;
    }

    private float GetCenterX()
    {
        if (LaneManager.Instance != null)
        {
            int totalLanes = LaneManager.Instance.GetTotalLaneCount();
            int middleLane = totalLanes / 2;
            return LaneManager.Instance.GetLanePosition(middleLane).x;
        }
        return 0f;
    }


}

public class SimpleScrollingText : MonoBehaviour
{
    private float lifetime = 30f;
    private float timeAlive = 0f;

    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
    }

    private void Update()
    {
        // Skip if game not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Get scroll speed
        float scrollSpeed = 1f;
        if (LaneManager.Instance != null)
        {
            scrollSpeed = LaneManager.Instance.GetScrollSpeed();
        }
        else if (GameManager.Instance != null)
        {
            scrollSpeed = GameManager.Instance.currentSpeed;
        }

        // Move down
        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        // Track time
        timeAlive += Time.deltaTime;

        // Destroy when expired or off screen
        if (timeAlive >= lifetime || transform.position.y < -15f)
        {
            Destroy(gameObject);
        }
    }
}