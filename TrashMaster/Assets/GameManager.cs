using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float baseSpeed = 5f;
    public float speedIncreasePerLevel = 0.5f;
    public int pointsPerTrash = 100;
    public int penaltyForMissedTrash = 50;

    // Using FindObjectOfType to avoid circular references
    private PlayerController _player;
    private LevelManager _levelManager;
    private UIManager _uiManager;
    private AudioManager _audioManager;

    // Public properties to access these components
    public PlayerController player { get { return _player; } }
    public LevelManager levelManager { get { return _levelManager; } }
    public UIManager uiManager { get { return _uiManager; } }
    public AudioManager audioManager { get { return _audioManager; } }

    [Header("Game State")]
    public int currentLevel = 1;
    public int score = 0;
    public float currentSpeed;
    public bool gameOver = false;
    public bool isPaused = false;
    public bool isGameStarted = false;

    // Define the enum without a header
    public enum TruckType { General, Paper, Plastic, Glass }

    [Header("Truck Types")]
    public TruckType currentTruckType = TruckType.General;

    [Header("Truck Sprites")]
    public Sprite generalTruckSprite;
    public Sprite paperTruckSprite;
    public Sprite plasticTruckSprite;
    public Sprite glassTruckSprite;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;

            // Ensure it's a root GameObject
            transform.parent = null;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Initialize default values
        currentSpeed = baseSpeed;
    }

    private void Start()
    {
        // Find references after all objects are initialized
        _player = Object.FindFirstObjectByType<PlayerController>();
        _levelManager = Object.FindFirstObjectByType<LevelManager>();
        _uiManager = Object.FindFirstObjectByType<UIManager>();
        _audioManager = Object.FindFirstObjectByType<AudioManager>();

        // Show the title screen when game starts
        ShowTitleScreen();
    }

    public void StartGame()
    {
        isGameStarted = true;
        gameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        score = 0;
        currentLevel = 1;
        currentSpeed = baseSpeed;

        // Reset player position
        if (_player != null)
        {
            // Make sure player is active
            _player.gameObject.SetActive(true);

            // Reset position
            _player.ResetPosition();

            // Notify about initial level
            _player.OnLevelChanged(currentLevel);
        }

        // Start with general truck
        SetTruckType(TruckType.General);

        // Generate level
        if (_levelManager != null)
        {
            _levelManager.GenerateLevel(currentLevel);
        }

        // Update UI
        if (_uiManager != null)
        {
            _uiManager.UpdateLevelText(currentLevel);
            _uiManager.UpdateScoreText(score);
            _uiManager.HideTitleScreen();
            _uiManager.HideGameOverScreen();

            // Make sure pause screen is hidden too
            if (_uiManager.pauseScreen != null)
            {
                _uiManager.HidePauseScreen();
            }
        }
    }

    public void GameOver()
    {
        if (!gameOver)
        {
            gameOver = true;

            if (_audioManager != null)
            {
                _audioManager.PlaySound("crash");
            }

            if (_uiManager != null)
            {
                _uiManager.ShowGameOverScreen();
            }
        }
    }

    public void CollectTrash()
    {
        score += pointsPerTrash;

        if (_audioManager != null)
        {
            _audioManager.PlaySound("collect");
        }

        if (_uiManager != null)
        {
            _uiManager.UpdateScoreText(score);
        }
    }

    public void MissTrash()
    {
        score -= penaltyForMissedTrash;

        if (_audioManager != null)
        {
            _audioManager.PlaySound("missed");
        }

        if (_uiManager != null)
        {
            _uiManager.UpdateScoreText(score);
        }
    }

    public void CheckLevelCompletion()
    {
        if (_levelManager != null && _levelManager.IsLevelComplete())
        {
            Debug.Log("[GameManager] Level completion confirmed from LevelManager!");

            // Call this to proceed to next level
            LevelUp();

            // Add a backup trigger for good measure
            Invoke("EnsureLevelUp", 1.0f);
        }
    }

    private void EnsureLevelUp()
    {
        // This is a backup in case the first call doesn't take
        if (_levelManager.IsLevelComplete())
        {
            Debug.Log("[GameManager] Using backup level completion trigger!");
            LevelUp();
        }
    }

    public void LevelUp()
    {
        currentLevel++;
        currentSpeed += speedIncreasePerLevel;

        Debug.Log("Leveling up to level " + currentLevel + " with speed " + currentSpeed);

        // Update lanes for new level if LaneManager exists
        if (LaneManager.Instance != null)
        {
            LaneManager.Instance.UpdateForLevel(currentLevel);
        }

        // Determine new truck type
        TruckType newTruckType;
        if (currentLevel % 4 == 1)
            newTruckType = TruckType.General;
        else if (currentLevel % 4 == 2)
            newTruckType = TruckType.Paper;
        else if (currentLevel % 4 == 3)
            newTruckType = TruckType.Plastic;
        else
            newTruckType = TruckType.Glass;

        // Set truck type
        SetTruckType(newTruckType);

        // Notify player controller about level change
        if (_player != null)
        {
            _player.OnLevelChanged(currentLevel);
        }

        if (_audioManager != null)
        {
            _audioManager.PlaySound("levelup");
        }

        // Option 1: Show separate notifications
        /*
        if (_uiManager != null)
        {
            _uiManager.ShowLevelUpText(currentLevel);
            _uiManager.UpdateLevelText(currentLevel);
        }
        */

        // Option 2: Show combined notification
        if (_uiManager != null)
        {
            _uiManager.ShowLevelUpAndTruckType(currentLevel, currentTruckType);
            _uiManager.UpdateLevelText(currentLevel);
        }

        // Generate new level
        if (_levelManager != null)
        {
            _levelManager.GenerateLevel(currentLevel);
        }
    }

    public void SetTruckType(TruckType type)
    {
        // Check if this is actually a change
        bool isTypeChange = (currentTruckType != type);

        // Set the new type
        currentTruckType = type;

        // Update the player's truck sprite and appearance
        if (_player != null)
        {
            // Update truck appearance
            _player.UpdateTruckAppearance(type);

            // Get truck type controller if available
            TruckTypeController typeController = _player.GetComponent<TruckTypeController>();
            if (typeController != null)
            {
                typeController.SetTruckType(type);
            }
        }

        // Show notification about truck type change (but only if it's during gameplay)
        if (isTypeChange && isGameStarted && !gameOver && _uiManager != null)
        {
            _uiManager.ShowTruckTypeChangeText(type);
        }
    }

    public void ShowTitleScreen()
    {
        isGameStarted = false;
        isPaused = false;
        Time.timeScale = 1f;

        if (_uiManager != null)
        {
            _uiManager.ShowTitleScreen();
        }
    }


    [Header("Penalties")]
    public int wrongTypePenalty = 25; // Set to 25 points

    public void ApplyWrongTypePenalty()
    {
        // Add explicit debug to understand what's happening
        int originalScore = score;
        int penalty = wrongTypePenalty;

        // Deduct penalty from score
        score -= penalty;

        Debug.Log($"Wrong type penalty applied: Score changed from {originalScore} to {score} (penalty: {penalty})");

        // Play sound effect
        if (_audioManager != null)
        {
            _audioManager.PlaySound("missed");
        }

        // Update UI
        if (_uiManager != null)
        {
            _uiManager.UpdateScoreText(score);
        }
    }

    public void RestartGame()
    {
        StartGame();
    }

    public void TogglePause()
    {
        if (!isGameStarted || gameOver) return;

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (_uiManager != null)
        {
            if (isPaused)
                _uiManager.ShowPauseScreen();
            else
                _uiManager.HidePauseScreen();
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Handle keyboard input for game state
    private void Update()
    {
        // Start game with any key
        if (!isGameStarted && Input.anyKeyDown)
        {
            StartGame();
        }

        // Restart game with any key when game over
        if (gameOver && Input.anyKeyDown)
        {
            RestartGame();
        }

        // Pause/unpause with Escape key
        if (Input.GetKeyDown(KeyCode.Escape) && isGameStarted && !gameOver)
        {
            TogglePause();
        }
    }
}