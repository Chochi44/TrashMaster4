using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    public bool isSideLane = false;

    private SpriteRenderer spriteRenderer;
    private float scrollSpeed = 0.1f;
    private float offset = 0f;
    private Material originalMaterial;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            return;

        // Create a unique material instance for this lane
        originalMaterial = spriteRenderer.material;
        spriteRenderer.material = new Material(originalMaterial);

        // Make texture repeat properly
        spriteRenderer.material.mainTextureScale = new Vector2(1, 5);
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Get scroll speed from GameManager
        float speed = GameManager.Instance.currentSpeed;

        // Update texture offset
        offset += speed * Time.deltaTime * scrollSpeed;
        if (offset > 1f)
            offset -= 1f;

        // Apply offset to material
        spriteRenderer.material.mainTextureOffset = new Vector2(0, offset);
    }
}