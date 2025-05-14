using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 1f;
    public bool useGlobalSpeed = true;

    // Reference to the sprite, can be assigned in editor or found automatically
    public SpriteRenderer roadSprite;

    private Vector2 initialPosition;
    private float spriteHeight;
    private bool isSideLane = false;

    private void Awake()
    {
        // Initialize only local components
        if (roadSprite == null)
        {
            roadSprite = GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        // Safe to access GameManager here
        if (roadSprite != null)
        {
            initialPosition = transform.position;

            // Determine if this is a side lane by name
            string name = gameObject.name.ToLower();
            isSideLane = name.Contains("left") || name.Contains("right") ||
                         name.Contains("side") || name.EndsWith("_0") || name.EndsWith("_6");

            if (isSideLane)
            {
                // Side lanes have taller sprites (630px)
                spriteHeight = roadSprite.bounds.size.y;
            }
            else if (roadSprite.drawMode == SpriteDrawMode.Tiled)
            {
                // For tiled center lanes, use the size property
                spriteHeight = roadSprite.size.y;
            }
            else
            {
                // Regular center lanes
                spriteHeight = roadSprite.bounds.size.y;
            }

            Debug.Log($"Lane {gameObject.name} - Sprite height: {spriteHeight}");
        }
        else
        {
            Debug.LogError($"No SpriteRenderer found on lane {gameObject.name}");
        }
    }

    private void Update()
    {
        // Safe to access GameManager here - it's not in Awake
        if (GameManager.Instance != null && GameManager.Instance.isGameStarted && !GameManager.Instance.gameOver)
        {
            float speed = useGlobalSpeed ? GameManager.Instance.currentSpeed : scrollSpeed;

            // Move road downward
            transform.Translate(Vector3.down * speed * Time.deltaTime);

            // Check if road needs to be reset
            if (transform.position.y <= initialPosition.y - spriteHeight / 2)
            {
                // Reset position
                Vector3 resetPosition = transform.position;
                resetPosition.y = initialPosition.y;
                transform.position = resetPosition;
            }
        }
    }
}