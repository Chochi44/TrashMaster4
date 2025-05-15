using UnityEngine;

public class SegmentScroller : MonoBehaviour
{
    public float scrollSpeed = 0.5f;
    public float segmentHeight = 1f;

    private Transform[] segments;
    private float segmentCount;

    void Start()
    {
        // Collect all segment children
        segments = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            segments[i] = transform.GetChild(i);
        }
        segmentCount = segments.Length;
    }

    void Update()
    {
        // Skip if game not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Get current speed from GameManager
        float gameSpeed = GameManager.Instance.currentSpeed;

        // Calculate scroll amount this frame
        float scrollAmount = Time.deltaTime * gameSpeed * scrollSpeed;

        // Move all segments down
        for (int i = 0; i < segments.Length; i++)
        {
            // Move segment down
            segments[i].Translate(0, -scrollAmount, 0);

            // If segment moves off the bottom
            if (segments[i].localPosition.y < -segmentHeight * segmentCount / 2)
            {
                // Move it to the top
                Vector3 pos = segments[i].localPosition;
                pos.y += segmentHeight * segmentCount;
                segments[i].localPosition = pos;
            }
        }
    }
}