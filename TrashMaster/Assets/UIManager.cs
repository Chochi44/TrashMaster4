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
    public float levelUpVerticalOffset = 50f;
    public float truckTypeVerticalOffset = -50f;

    private Coroutine levelUpTextCoroutine;
    private Coroutine truckTypeTextCoroutine;

    private void Awake()
    {
        // Initialize local components
        InitializeTextComponents();

        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }
    }

    private void InitializeTextComponents()
    {
        // Setup levelUpText
        if (levelUpText != null)
        {
            levelUpText.gameObject.SetActive(false);
            levelUpText.alignment = TextAlignmentOptions.Center;

            // Set anchors to center of screen
            RectTransform rectTransform = levelUpText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }

        // Setup truckTypeChangeText
        if (truckTypeChangeText != null)
        {
            truckTypeChangeText.gameObject.SetActive(false);
            truckTypeChangeText.alignment = TextAlignmentOptions.Center;

            // Set anchors to center of screen
            RectTransform rectTransform = truckTypeChangeText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = Vector2.zero;
            }
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
            // Position at center of screen
            RectTransform rectTransform = levelUpText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

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
            // Position at center of screen
            RectTransform rectTransform = truckTypeChangeText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }

            truckTypeTextCoroutine = StartCoroutine(AnimateTruckTypeText(truckType));
        }
    }

    private IEnumerator AnimateLevelUpText(int level)
    {
        // Set text content
        levelUpText.text = "LEVEL " + level;

        // Reset color
        levelUpText.color = new Color(levelUpText.color.r, levelUpText.color.g, levelUpText.color.b, 1f);

        // Ensure text is active
        levelUpText.gameObject.SetActive(true);

        // Get rect transform
        RectTransform rectTransform = levelUpText.GetComponent<RectTransform>();

        // Store initial position
        Vector2 startPosition = rectTransform.anchoredPosition;

        // Animation duration
        float startTime = Time.time;
        float duration = levelUpTextDuration;

        while (Time.time - startTime < duration)
        {
            // Move text upward
            rectTransform.anchoredPosition += Vector2.up * levelUpTextSpeed * Time.deltaTime;

            // Fade out near the end
            if (Time.time - startTime > duration * 0.7f)
            {
                float alpha = 1 - ((Time.time - startTime) - (duration * 0.7f)) / (duration * 0.3f);
                levelUpText.color = new Color(levelUpText.color.r, levelUpText.color.g, levelUpText.color.b, alpha);
            }

            yield return null;
        }

        // Hide text
        levelUpText.gameObject.SetActive(false);

        // Reset position for next time
        rectTransform.anchoredPosition = startPosition;
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

        // Set text and color
        truckTypeChangeText.text = typeText;
        truckTypeChangeText.color = textColor;

        // Get rect transform
        RectTransform rectTransform = truckTypeChangeText.GetComponent<RectTransform>();

        // Make text visible
        truckTypeChangeText.gameObject.SetActive(true);

        // Store initial position
        Vector2 startPosition = rectTransform.anchoredPosition;

        // Animation
        float startTime = Time.time;
        float duration = truckTypeTextDuration;

        while (Time.time - startTime < duration)
        {
            // Move text upward
            rectTransform.anchoredPosition += Vector2.up * levelUpTextSpeed * Time.deltaTime;

            // Fade out near the end
            if (Time.time - startTime > duration * 0.7f)
            {
                float alpha = 1 - ((Time.time - startTime) - (duration * 0.7f)) / (duration * 0.3f);
                truckTypeChangeText.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
            }

            yield return null;
        }

        // Hide text
        truckTypeChangeText.gameObject.SetActive(false);

        // Reset color and position
        truckTypeChangeText.color = new Color(textColor.r, textColor.g, textColor.b, 1f);
        rectTransform.anchoredPosition = startPosition;
    }

    // Method to show both level up and truck type text together
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

        // Position level up text above center
        if (levelUpText != null)
        {
            RectTransform rectTransform = levelUpText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, levelUpVerticalOffset);
            }

            // Set content
            levelUpText.text = "LEVEL " + level;

            // Reset color
            levelUpText.color = new Color(levelUpText.color.r, levelUpText.color.g, levelUpText.color.b, 1f);

            // Make visible
            levelUpText.gameObject.SetActive(true);

            // Start animation
            levelUpTextCoroutine = StartCoroutine(AnimateTextUpward(levelUpText, rectTransform.anchoredPosition));
        }

        // Position truck type text below center
        if (truckTypeChangeText != null)
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

            // Set text and color
            truckTypeChangeText.text = typeText;
            truckTypeChangeText.color = textColor;

            RectTransform rectTransform = truckTypeChangeText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, truckTypeVerticalOffset);
            }

            // Make visible
            truckTypeChangeText.gameObject.SetActive(true);

            // Start animation
            truckTypeTextCoroutine = StartCoroutine(AnimateTextUpward(truckTypeChangeText, rectTransform.anchoredPosition));
        }
    }

    // Generic animation for moving text upward with fade out
    private IEnumerator AnimateTextUpward(TextMeshProUGUI textComponent, Vector2 startPosition)
    {
        // Store color for resetting
        Color originalColor = textComponent.color;

        // Animation duration
        float startTime = Time.time;
        float duration = levelUpTextDuration;

        // Get rect transform
        RectTransform rectTransform = textComponent.GetComponent<RectTransform>();

        while (Time.time - startTime < duration)
        {
            // Move text upward
            rectTransform.anchoredPosition += Vector2.up * levelUpTextSpeed * Time.deltaTime;

            // Fade out near the end
            if (Time.time - startTime > duration * 0.7f)
            {
                float alpha = 1 - ((Time.time - startTime) - (duration * 0.7f)) / (duration * 0.3f);
                textComponent.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            }

            yield return null;
        }

        // Hide text
        textComponent.gameObject.SetActive(false);

        // Reset color and position
        textComponent.color = originalColor;
        rectTransform.anchoredPosition = startPosition;
    }
}