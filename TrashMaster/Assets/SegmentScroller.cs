using UnityEngine;

public class SegmentScroller : MonoBehaviour
{
    public float scrollSpeed = 0.5f;
    public float segmentHeight = 1f;

    private Transform[] segments;
    private float totalHeight;

    void Start()
    {
        // Collect all segments
        segments = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            segments[i] = transform.GetChild(i);
        }

        if (segments.Length > 0)
        {
            // Calculate total height of all segments
            totalHeight = segmentHeight * segments.Length;
        }
    }

    void Update()
    {
        // Skip if game not active
        if (GameManager.Instance == null || !GameManager.Instance.isGameStarted || GameManager.Instance.gameOver)
            return;

        // Calculate scroll amount
        float scrollAmount = Time.deltaTime * scrollSpeed;

        foreach (Transform segment in segments)
        {
            // Move segment down
            segment.Translate(0, -scrollAmount, 0);

            // If segment has moved off the bottom
            if (segment.localPosition.y < -totalHeight / 2)
            {
                // Reposition to top
                Vector3 newPos = segment.localPosition;
                newPos.y += totalHeight;
                segment.localPosition = newPos;
            }
        }
    }
}