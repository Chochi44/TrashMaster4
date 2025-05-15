using UnityEngine;

public class LaneScroller : MonoBehaviour
{
    [Range(0.01f, 5f)]
    public float scrollSpeed = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Material material;
    private float offset = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.material != null)
        {
            material = spriteRenderer.material;
        }
    }

    void Update()
    {
        // Skip if game not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Get game speed
        float gameSpeed = GameManager.Instance.currentSpeed;

        // Update offset
        offset += Time.deltaTime * gameSpeed * scrollSpeed;

        // Loop offset between 0-1
        if (offset > 1f)
            offset %= 1f;

        // Apply scrolling
        if (material != null)
        {
            material.mainTextureOffset = new Vector2(0, -offset); // Negative to scroll downward
        }
    }
}