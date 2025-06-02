using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;  // Horizontal speed
    public float forwardSpeed = 5f;  // Initial forward speed
    public float smoothTime = 0.1f;  // Smoothing time for movement
    public float maxHorizontalSpeed = 15f;  // Maximum horizontal speed

    [Header("Difficulty Settings")]
    [SerializeField] private float initialSpeed = 5f;
    [SerializeField] private float maxSpeed = 20f;
    [SerializeField] private float speedIncreaseInterval = 30f; // Time between speed increases
    [SerializeField] private float speedIncreaseAmount = 1f; // How much speed increases each interval
    [SerializeField] private float difficultyMultiplier = 1f; // Overall difficulty multiplier

    [Header("Ground Check")]
    public float groundCheckDistance = 0.1f;
    public LayerMask groundLayer;

    [Header("Score Settings")]
    [SerializeField] private float scoreMultiplier = 1f; // Score multiplier based on speed

    private Rigidbody rb;
    private Vector2 dragStartPos;
    private Vector2 dragCurrentPos;
    private bool isDragging = false;
    private Mouse mouse;
    private float currentHorizontalVelocity;
    private float targetHorizontalVelocity;
    private float smoothVelocity;
    private bool isGrounded;
    private Vector3 lastPosition;
    private float stoppedTimeThreshold = 0.5f;
    private float stoppedDistanceThreshold = 0.1f;
    private float timeStopped = 0f;
    private float nextSpeedIncreaseTime;
    private float gameTime;
    private int currentPhase = 1;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        mouse = Mouse.current;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        lastPosition = transform.position;
        forwardSpeed = initialSpeed;
        nextSpeedIncreaseTime = speedIncreaseInterval;
        gameTime = 0f;
    }

    private void Update()
    {
        CheckGrounded();
        HandleInput();
        UpdateDifficulty();
        UpdateScore();
    }

    private void UpdateDifficulty()
    {
        gameTime += Time.deltaTime;

        // Increase speed at intervals
        if (gameTime >= nextSpeedIncreaseTime && forwardSpeed < maxSpeed)
        {
            forwardSpeed += speedIncreaseAmount;
            nextSpeedIncreaseTime = gameTime + speedIncreaseInterval;
            
            // Update difficulty phase
            currentPhase = Mathf.FloorToInt(gameTime / 60f) + 1; // New phase every minute
            difficultyMultiplier = 1f + (currentPhase * 0.2f); // 20% increase per phase
            scoreMultiplier = 1f + (currentPhase * 0.1f); // 10% score increase per phase
            
            // Notify GameManager of phase change
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnDifficultyPhaseChange(currentPhase);
            }
        }
    }

    private void UpdateScore()
    {
        // Score increases based on distance traveled and current speed
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddDistanceScore(forwardSpeed * scoreMultiplier * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    private void CheckGrounded()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    private void HandleInput()
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isDragging = true;
            dragStartPos = mouse.position.ReadValue();
        }
        else if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            targetHorizontalVelocity = 0f;
        }

        if (isDragging)
        {
            dragCurrentPos = mouse.position.ReadValue();
            float deltaX = (dragCurrentPos.x - dragStartPos.x) / Screen.width;
            targetHorizontalVelocity = deltaX * moveSpeed;
            targetHorizontalVelocity = Mathf.Clamp(targetHorizontalVelocity, -maxHorizontalSpeed, maxHorizontalSpeed);
        }
    }

    private void ApplyMovement()
    {
        // Smooth horizontal movement
        currentHorizontalVelocity = Mathf.SmoothDamp(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            ref smoothVelocity,
            smoothTime
        );

        // Apply movement
        Vector3 newVelocity = new Vector3(
            currentHorizontalVelocity,
            rb.linearVelocity.y,
            forwardSpeed
        );

        rb.linearVelocity = newVelocity;

        // Check if player has stopped moving
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        if (distanceMoved < stoppedDistanceThreshold)
        {
            timeStopped += Time.deltaTime;
            if (timeStopped >= stoppedTimeThreshold)
            {
                GameManager.Instance.GameOver();
            }
        }
        else
        {
            timeStopped = 0f;
        }

        lastPosition = transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Coin"))
        {
            GameManager.Instance.AddScore(1);
            Destroy(other.gameObject);
        }
    }

    // Optional: Visualize ground check in editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }

    public float GetCurrentSpeed()
    {
        return forwardSpeed;
    }

    public float GetMaxSpeed()
    {
        return maxSpeed;
    }

    public int GetCurrentPhase()
    {
        return currentPhase;
    }

    public float GetDifficultyMultiplier()
    {
        return difficultyMultiplier;
    }
}