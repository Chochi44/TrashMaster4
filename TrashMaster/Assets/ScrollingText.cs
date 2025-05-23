using UnityEngine;
using TMPro;

public class ScrollingText : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public bool useGlobalSpeed = true;

    [Header("Debug")]
    public bool debugMode = true;

    private float lifetime = 30f;
    private float timeAlive = 0f;

    private void Start()
    {
        // Make sure this text object has the correct layer ordering
        TextMeshPro textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            // Set sorting order to be above road (5) but below truck (10)
            textMesh.sortingOrder = 6;

            if (debugMode)
            {
                Debug.Log($"[ScrollingText] Started for '{textMesh.text}' at {transform.position}");
                Debug.Log($"[ScrollingText] Sorting order: {textMesh.sortingOrder}");
                Debug.Log($"[ScrollingText] Font: {textMesh.font?.name}");
                Debug.Log($"[ScrollingText] Color: {textMesh.color}");
                Debug.Log($"[ScrollingText] Font Size: {textMesh.fontSize}");
            }
        }
        else
        {
            Debug.LogError($"[ScrollingText] No TextMeshPro component found on {gameObject.name}!");
        }
    }

    private void Update()
    {
        // Skip if game not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Get the scroll speed to match lane movement
        float scrollSpeed = GetScrollSpeed();

        if (debugMode && Time.frameCount % 60 == 0) // Debug every second
        {
            Debug.Log($"[ScrollingText] Position: {transform.position}, Speed: {scrollSpeed}");
        }

        // Move the text down at the same speed as the lanes
        transform.position += Vector3.down * scrollSpeed * Time.deltaTime;

        // Track lifetime
        timeAlive += Time.deltaTime;

        // Destroy when lifetime expires or when far below camera
        float destroyPos = GetDestroyPosition();
        if (timeAlive >= lifetime || transform.position.y < destroyPos)
        {
            if (debugMode)
            {
                Debug.Log($"[ScrollingText] Destroying text at {transform.position} (timeAlive: {timeAlive}, destroyPos: {destroyPos})");
            }
            Destroy(gameObject);
        }
    }

    private float GetScrollSpeed()
    {
        if (useGlobalSpeed)
        {
            // Use the same speed as lanes for synchronized movement
            if (LaneManager.Instance != null)
            {
                return LaneManager.Instance.GetScrollSpeed();
            }
            else if (GameManager.Instance != null)
            {
                return GameManager.Instance.currentSpeed;
            }
        }

        return 1f; // Fallback speed
    }

    private float GetDestroyPosition()
    {
        float cameraY = 0f;
        float cameraHeight = 5f;

        if (Camera.main != null)
        {
            cameraY = Camera.main.transform.position.y;
            cameraHeight = Camera.main.orthographicSize;
        }

        // Destroy when well below camera view
        return cameraY - cameraHeight - 10f;
    }

    public void SetLifetime(float newLifetime)
    {
        lifetime = newLifetime;
    }
}