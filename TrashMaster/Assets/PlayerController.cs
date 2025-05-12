using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float laneWidth = 60f;
    public float moveSpeed = 10f;
    public int currentLane = 3; // Start in the middle lane (assuming 5 main lanes)
    public int totalLanes = 7; // Total lanes including side lanes

    [Header("References")]
    private Vector3 targetPosition;
    private bool isMoving = false;

    private void Awake()
    {
        // Initialize only local components
    }

    private void Start()
    {
        // Safe to access GameManager now
        ResetPosition();
    }

    private void Update()
    {
        // Only process input when game is active
        if (GameManager.Instance != null && !GameManager.Instance.gameOver && GameManager.Instance.isGameStarted)
        {
            // Handle keyboard input for movement
            HandleKeyboardInput();

            // Move towards target position
            MoveToTargetPosition();
        }
    }

    private void HandleKeyboardInput()
    {
        // Left arrow key moves left
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentLane > 1)
        {
            MoveLeft();
        }
        // Right arrow key moves right
        else if (Input.GetKeyDown(KeyCode.RightArrow) && currentLane < totalLanes - 2)
        {
            MoveRight();
        }
    }

    public void MoveLeft()
    {
        if (!isMoving && currentLane > 1)
        {
            currentLane--;
            UpdateTargetPosition();
        }
    }

    public void MoveRight()
    {
        if (!isMoving && currentLane < totalLanes - 2)
        {
            currentLane++;
            UpdateTargetPosition();
        }
    }

    private void UpdateTargetPosition()
    {
        // Calculate target X position based on lane number
        float laneCenter = (currentLane - 1) * laneWidth + (laneWidth / 2);
        targetPosition = new Vector3(laneCenter, transform.position.y, transform.position.z);
        isMoving = true;
    }

    private void MoveToTargetPosition()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    public void ResetPosition()
    {
        currentLane = 3; // Middle lane
        UpdateTargetPosition();
        transform.position = targetPosition;
        isMoving = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Safety check
        if (GameManager.Instance == null) return;

        if (other.CompareTag("Obstacle"))
        {
            GameManager.Instance.GameOver();
        }
        else if (other.CompareTag("Trash"))
        {
            TrashItem trash = other.GetComponent<TrashItem>();
            if (trash == null) return;

            // Check if the player can collect this type of trash
            bool canCollect = true;
            if (GameManager.Instance.currentLevel > 1)
            {
                GameManager.TruckType truckType = GameManager.Instance.currentTruckType;

                // Only collect trash of the same type as the truck (after level 1)
                if (truckType == GameManager.TruckType.Paper && !trash.isPaper)
                    canCollect = false;
                else if (truckType == GameManager.TruckType.Plastic && !trash.isPlastic)
                    canCollect = false;
                else if (truckType == GameManager.TruckType.Glass && !trash.isGlass)
                    canCollect = false;
            }

            if (canCollect)
            {
                GameManager.Instance.CollectTrash();
                trash.Collect();
            }
        }
    }
}