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

    // NEW: Seamless level transition flags
    private bool levelUpInProgress = false;
    private bool pendingLevelGeneration = false;

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

        // Reset level up flags
        levelUpInProgress = false;
        pendingLevelGeneration = false;

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
        // ALWAYS use exactly the penaltyForMissedTrash value (25)
        int penalty = penaltyForMissedTrash;

        // For debugging
        int originalScore = score;

        // Apply EXACTLY one penalty
        score -= penalty;

        Debug.Log($"Missed trash penalty applied: Score changed from {originalScore} to {score} (penalty: {penalty})");

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

    public void CheckLevelCompletion()
    {
        // NEW: Prevent duplicate level completions
        if (levelUpInProgress)
        {
            Debug.Log("[GameManager] Level up already in progress, ignoring duplicate call");
            return;
        }

        // NEW: In seamless mode, we want to level up immediately
        if (_levelManager != null && _levelManager.enableSeamlessTransition)
        {
            Debug.Log("[GameManager] Seamless level completion detected! LevelManager handles object generation.");
            // In seamless mode, LevelManager already generated the objects
            // We just need to do the UI/truck updates
            LevelUpUIOnly();
        }
        else
        {
            // Traditional mode - check if level is actually complete
            if (_levelManager != null && _levelManager.IsLevelComplete())
            {
                Debug.Log("[GameManager] Level completion confirmed from LevelManager!");
                LevelUp();
            }
        }
    }

    // NEW: Level up UI and truck changes only (for seamless mode)
    public void LevelUpUIOnly()
    {
        // NEW: Prevent duplicate level ups
        if (levelUpInProgress)
        {
            Debug.Log("[GameManager] Level up already in progress, skipping duplicate");
            return;
        }

        levelUpInProgress = true;

        currentLevel++;
        currentSpeed += speedIncreasePerLevel;

        Debug.Log("[GameManager] Level up UI only - level " + currentLevel + " with speed " + currentSpeed);

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

        // Show combined notification
        if (_uiManager != null)
        {
            _uiManager.ShowLevelUpAndTruckType(currentLevel, currentTruckType);
            _uiManager.UpdateLevelText(currentLevel);
        }

        Debug.Log("[GameManager] Seamless mode - objects already generated by LevelManager");

        // Reset the level up flag after a short delay
        StartCoroutine(ResetLevelUpFlag());
    }

    public void LevelUp()
    {
        // NEW: Prevent duplicate level ups
        if (levelUpInProgress)
        {
            Debug.Log("[GameManager] Level up already in progress, skipping duplicate");
            return;
        }

        levelUpInProgress = true;

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

        // Show combined notification
        if (_uiManager != null)
        {
            _uiManager.ShowLevelUpAndTruckType(currentLevel, currentTruckType);
            _uiManager.UpdateLevelText(currentLevel);
        }

        // NEW: In seamless mode, the level generation is handled by LevelManager
        // In traditional mode, we still need to generate the level here
        if (_levelManager != null && !_levelManager.enableSeamlessTransition)
        {
            _levelManager.GenerateLevel(currentLevel);
        }
        else
        {
            Debug.Log("[GameManager] Seamless mode - level objects already generated by LevelManager");
        }

        // Reset the level up flag after a short delay to prevent rapid multiple calls
        StartCoroutine(ResetLevelUpFlag());
    }

    // NEW: Coroutine to reset level up flag
    private IEnumerator ResetLevelUpFlag()
    {
        yield return new WaitForSeconds(0.5f); // Half second delay
        levelUpInProgress = false;
        Debug.Log("[GameManager] Level up flag reset");
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

        // Reset level up flags
        levelUpInProgress = false;
        pendingLevelGeneration = false;

        if (_uiManager != null)
        {
            _uiManager.ShowTitleScreen();
        }
    }

    [Header("Penalties")]
    public int wrongTypePenalty = 25; // Set to 25 points

    public void ApplyWrongTypePenalty()
    {
        // ALWAYS use exactly the wrongTypePenalty value (25)
        int penalty = wrongTypePenalty;

        // For debugging
        int originalScore = score;

        // Apply EXACTLY one penalty
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