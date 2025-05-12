using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadScroller : MonoBehaviour
{
    [Header("Scrolling Settings")]
    public float scrollSpeed = 1f;
    public bool useGlobalSpeed = true;

    [Header("References")]
    public SpriteRenderer roadSprite;

    private Vector2 startPosition;
    private float spriteHeight;

    // No GameManager access in Awake
    private void Awake()
    {
        // Initialize only local components
    }

    private void Start()
    {
        if (roadSprite == null)
        {
            roadSprite = GetComponent<SpriteRenderer>();
        }

        if (roadSprite != null)
        {
            startPosition = transform.position;
            spriteHeight = roadSprite.bounds.size.y / 2; // Divided by 2 because we'll reset at half height
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
            if (transform.position.y <= -spriteHeight)
            {
                // Reset position
                transform.position = startPosition;
            }
        }
    }
}