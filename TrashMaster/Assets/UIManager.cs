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

    [Header("Scrolling Text Manager")]
    public SimpleScrollingTextManager scrollingTextManager;

    [Header("Wrong Type Message")]
    public TextMeshProUGUI wrongTypeText;
    public float wrongTypeTextDuration = 1f;
    private Coroutine wrongTypeTextCoroutine;

    [Header("Animation Settings")]
    public float wrongTypeVerticalOffset = 0f;

    private void Awake()
    {
        // Initialize local components
        InitializeTextComponents();

        if (pauseScreen != null)
        {
            pauseScreen.SetActive(false);
        }

        // Find scrolling text manager if not assigned
        if (scrollingTextManager == null)
        {
            scrollingTextManager = FindObjectOfType<SimpleScrollingTextManager>();
        }
    }

    private void InitializeTextComponents()
    {
        // Setup wrongTypeText
        if (wrongTypeText != null)
        {
            wrongTypeText.gameObject.SetActive(false);
            wrongTypeText.alignment = TextAlignmentOptions.Center;

            // Set anchors to center of screen
            RectTransform rectTransform = wrongTypeText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, wrongTypeVerticalOffset);
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
        // Use the scrolling text manager instead of UI animations
        if (scrollingTextManager != null)
        {
            scrollingTextManager.ShowLevelUpText(level);
        }
        else
        {
            Debug.LogWarning("ScrollingTextManager not found! Cannot show level up text on road.");
        }
    }

    public void ShowTruckTypeChangeText(GameManager.TruckType truckType)
    {
        // Use the scrolling text manager instead of UI animations
        if (scrollingTextManager != null)
        {
            scrollingTextManager.ShowTruckTypeText(truckType);
        }
        else
        {
            Debug.LogWarning("ScrollingTextManager not found! Cannot show truck type text on road.");
        }
    }

    public void ShowLevelUpAndTruckType(int level, GameManager.TruckType truckType)
    {
        // Use the scrolling text manager to show both texts on the road
        if (scrollingTextManager != null)
        {
            scrollingTextManager.ShowLevelUpAndTruckType(level, truckType);
        }
        else
        {
            Debug.LogWarning("ScrollingTextManager not found! Cannot show level up and truck type text on road.");
        }
    }

    public void ShowWrongTypeMessage()
    {
        // Cancel any existing animation
        if (wrongTypeTextCoroutine != null)
        {
            StopCoroutine(wrongTypeTextCoroutine);
        }

        // Start new animation
        if (wrongTypeText != null)
        {
            wrongTypeText.text = "WRONG TRUCK TYPE!";
            wrongTypeText.color = Color.red;

            // Position at center of screen
            RectTransform rectTransform = wrongTypeText.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = new Vector2(0, wrongTypeVerticalOffset);
            }

            wrongTypeTextCoroutine = StartCoroutine(AnimateWrongTypeText());
        }
    }

    private IEnumerator AnimateWrongTypeText()
    {
        // Ensure text is active
        wrongTypeText.gameObject.SetActive(true);

        // Simple display and fade
        float startTime = Time.time;
        float duration = wrongTypeTextDuration;

        // Full visibility for most of the duration
        wrongTypeText.color = new Color(Color.red.r, Color.red.g, Color.red.b, 1f);

        yield return new WaitForSeconds(duration * 0.7f);

        // Fade out at the end
        float fadeTime = duration * 0.3f;
        float endTime = startTime + duration;

        while (Time.time < endTime)
        {
            float alpha = 1 - (Time.time - (startTime + duration * 0.7f)) / fadeTime;
            wrongTypeText.color = new Color(Color.red.r, Color.red.g, Color.red.b, alpha);
            yield return null;
        }

        // Hide text
        wrongTypeText.gameObject.SetActive(false);

        // Reset color
        wrongTypeText.color = Color.red;
    }
}