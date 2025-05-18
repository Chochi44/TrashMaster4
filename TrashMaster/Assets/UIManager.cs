using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject titleScreen;
    public GameObject gameOverScreen;
    public GameObject pauseScreen;
    public TextMeshProUGUI levelScoreText;
    public TextMeshProUGUI levelUpText;

    [Header("Truck Type Notification")]
    public TextMeshProUGUI truckTypeChangeText;
    public float truckTypeTextDuration = 2f;
    public Color generalTypeColor = Color.white;
    public Color paperTypeColor = Color.blue;
    public Color plasticTypeColor = Color.green;
    public Color glassTypeColor = Color.cyan;

    [Header("Animation Settings")]
    public float levelUpTextDuration = 2f;
    public float levelUpTextSpeed = 50f;

    private Coroutine levelUpTextCoroutine;
    private Coroutine truckTypeTextCoroutine;

    private void Awake()
    {
        // Initialize local components
        if (levelUpText != null)
        {
            levelUpText.gameObject.SetActive(false);
        }

        if (truckTypeChangeText != null)
        {
            truckTypeChangeText.gameObject.SetActive(false);
        }

        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
    }

    private void Start()
    {
        // Now it's safe to access GameManager
        if (GameManager.Instance != null)
        {
            UpdateLevelText(GameManager.Instance.currentLevel);
            UpdateScoreText(GameManager.Instance.score);
        }
    }

    public void ShowTitleScreen()
    {
        if (titleScreen != null)
        {
            titleScreen.SetActive(true);
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }

        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
    }

    public void HideTitleScreen()
    {
        if (titleScreen != null)
        {
            titleScreen.SetActive(false);
        }
    }

    public void ShowGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(true);
        }
    }

    public void HideGameOverScreen()
    {
        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    // Methods for pause screen
    public void ShowPauseScreen()
    {
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(true);
        }
    }

    public void HidePauseScreen()
    {
        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
    }

    public void UpdateLevelText(int level)
    {
        if (levelScoreText != null && GameManager.Instance != null)
        {
            levelScoreText.text = "Level: " + level + " Score: " + GameManager.Instance.score;
        }
    }

    public void UpdateScoreText(int score)
    {
        if (levelScoreText != null && GameManager.Instance != null)
        {
            levelScoreText.text = "Level: " + GameManager.Instance.currentLevel + " Score: " + score;
        }
    }

    public void ShowLevelUpText(int level)
    {
        // Cancel any existing animation
        if (levelUpTextCoroutine != null)
        {
            StopCoroutine(levelUpTextCoroutine);
        }

        // Start new animation
        if (levelUpText != null)
        {
            levelUpTextCoroutine = StartCoroutine(AnimateLevelUpText(level));
        }
    }

    // Call this to show the truck type change notification
    public void ShowTruckTypeChangeText(GameManager.TruckType truckType)
    {
        // Cancel any existing animation
        if (truckTypeTextCoroutine != null)
        {
            StopCoroutine(truckTypeTextCoroutine);
        }

        // Start new animation
        if (truckTypeChangeText != null)
        {
            truckTypeTextCoroutine = StartCoroutine(AnimateTruckTypeText(truckType));
        }
    }

    private IEnumerator AnimateLevelUpText(int level)
    {
        // Set text and position
        levelUpText.text = "LEVEL " + level;
        levelUpText.rectTransform.anchoredPosition = new Vector2(0, 0); // Center of screen
        levelUpText.gameObject.SetActive(true);

        // Store initial position
        Vector2 startPosition = levelUpText.rectTransform.anchoredPosition;

        // Animate up
        float startTime = Time.time;
        float duration = levelUpTextDuration;

        while (Time.time - startTime < duration)
        {
            // Move text upward
            levelUpText.rectTransform.anchoredPosition += Vector2.up * levelUpTextSpeed * Time.deltaTime;

            // Fade out near the end
            if (Time.time - startTime > duration * 0.7f)
            {
                float alpha = 1 - ((Time.time - startTime) - (duration * 0.7f)) / (duration * 0.3f);
                levelUpText.color = new Color(levelUpText.color.r, levelUpText.color.g, levelUpText.color.b, alpha);
            }

            yield return null;
        }

        // Hide text and reset color
        levelUpText.gameObject.SetActive(false);
        levelUpText.color = new Color(levelUpText.color.r, levelUpText.color.g, levelUpText.color.b, 1f);

        // Reset position for next time
        levelUpText.rectTransform.anchoredPosition = startPosition;
    }

    private IEnumerator AnimateTruckTypeText(GameManager.TruckType truckType)
    {
        // Set text and color based on truck type
        string typeText = "";
        Color textColor = Color.white;

        switch (truckType)
        {
            case GameManager.TruckType.General:
                typeText = "GENERAL TRUCK: Collects all trash";
                textColor = generalTypeColor;
                break;
            case GameManager.TruckType.Paper:
                typeText = "PAPER TRUCK: Collects paper only";
                textColor = paperTypeColor;
                break;
            case GameManager.TruckType.Plastic:
                typeText = "PLASTIC TRUCK: Collects plastic only";
                textColor = plasticTypeColor;
                break;
            case GameManager.TruckType.Glass:
                typeText = "GLASS TRUCK: Collects glass only";
                textColor = glassTypeColor;
                break;
        }

        // Set text and position
        truckTypeChangeText.text = typeText;
        truckTypeChangeText.color = textColor;

        // Center the text horizontally and position it vertically
        truckTypeChangeText.rectTransform.anchoredPosition = new Vector2(0, 0); // Center of screen

        // Set width to match the entire road width (all gray lanes combined)
        if (LaneManager.Instance != null && Camera.main != null)
        {
            // Get total lane count and screen width
            int totalLanes = LaneManager.Instance.GetTotalLaneCount();
            int centerLanes = LaneManager.Instance.GetCenterLaneCount();
            float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;

            // Get side lane width ratio from Lane Manager
            float sideLaneWidthRatio = LaneManager.Instance.sideLaneWidthRatio;

            // Calculate the width of all center lanes combined (the gray road area)
            float centerLanesTotalWidth = screenWidth - (2 * screenWidth * sideLaneWidthRatio);

            // Set the text width to match the center lanes width
            truckTypeChangeText.rectTransform.sizeDelta = new Vector2(centerLanesTotalWidth, truckTypeChangeText.rectTransform.sizeDelta.y);

            Debug.Log($"Screen width: {screenWidth}, Center lanes width: {centerLanesTotalWidth}");
        }
        else
        {
            // Fallback if LaneManager isn't available
            float screenWidth = Camera.main.orthographicSize * 2 * Camera.main.aspect;
            float roadWidth = screenWidth * 0.7f; // Assume road takes 70% of screen width
            truckTypeChangeText.rectTransform.sizeDelta = new Vector2(roadWidth, truckTypeChangeText.rectTransform.sizeDelta.y);
        }

        truckTypeChangeText.gameObject.SetActive(true);

        // Store initial position
        Vector2 startPosition = truckTypeChangeText.rectTransform.anchoredPosition;

        // Animate in the same way as levelUpText
        float startTime = Time.time;
        float duration = levelUpTextDuration; // Use the same duration as level up text

        while (Time.time - startTime < duration)
        {
            // Move text upward at the same speed as levelUpText
            truckTypeChangeText.rectTransform.anchoredPosition += Vector2.up * levelUpTextSpeed * Time.deltaTime;

            // Fade out near the end
            if (Time.time - startTime > duration * 0.7f)
            {
                float alpha = 1 - ((Time.time - startTime) - (duration * 0.7f)) / (duration * 0.3f);
                truckTypeChangeText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            }

            yield return null;
        }

        // Hide text and reset color
        truckTypeChangeText.gameObject.SetActive(false);
        truckTypeChangeText.color = new Color(textColor.r, textColor.g, textColor.b, 1f);

        // Reset position for next time
        truckTypeChangeText.rectTransform.anchoredPosition = startPosition;
    }

    // Optional: Combined method to show both texts with coordinated positioning
    public void ShowLevelUpAndTruckType(int level, GameManager.TruckType truckType)
    {
        // Cancel any existing animations
        if (levelUpTextCoroutine != null)
        {
            StopCoroutine(levelUpTextCoroutine);
        }

        if (truckTypeTextCoroutine != null)
        {
            StopCoroutine(truckTypeTextCoroutine);
        }

        // Position level text above and truck type text below
        if (levelUpText != null)
        {
            levelUpText.rectTransform.anchoredPosition = new Vector2(0, 50); // Above center
            levelUpTextCoroutine = StartCoroutine(AnimateLevelUpText(level));
        }

        if (truckTypeChangeText != null)
        {
            truckTypeChangeText.rectTransform.anchoredPosition = new Vector2(0, -50); // Below center
            truckTypeTextCoroutine = StartCoroutine(AnimateTruckTypeText(truckType));
        }
    }
}