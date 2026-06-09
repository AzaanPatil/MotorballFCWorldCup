using UnityEngine;

/// Unified vehicle controller that handles both player-controlled and AI movement.
/// - If isPlayerControlled = true: responds to WASD / arrow key input
/// - If isPlayerControlled = false: AI moves toward the ball
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    public enum Team { Friendly, Opponent }
    [Header("Team")]
    public Team team;

    [Header("Movement")]
    public float maxSpeed = 6f;
    public float acceleration = 30f;
    public float aiSpeed = 4.5f; // AI is slightly slower than player
    public float ballDetectionRadius = 3f;

    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";

    [Header("Control")]
    public bool isPlayerControlled = false;

    private Rigidbody2D rb;
    private GameManager gameManager;
    private Transform ball;
    private Vector2 playerInput = Vector2.zero;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            ball = gameManager.ball;
    }

    void Update()
    {
        // Only read player input if this vehicle is player-controlled
        if (isPlayerControlled)
        {
            playerInput.x = Input.GetAxisRaw(horizontalAxis);
            playerInput.y = Input.GetAxisRaw(verticalAxis);

            // Auto-switch: if the player is far from this vehicle, allow switching
            if (IsNearBall() && gameManager != null)
            {
                gameManager.SetActivePlayer(this);
            }
        }
    }

    void FixedUpdate()
    {
        if (isPlayerControlled)
        {
            MovePlayer();
        }
        else
        {
            MoveAI();
        }
    }

    /// Player-controlled movement: smooth acceleration toward target direction.
    private void MovePlayer()
    {
        if (playerInput.sqrMagnitude > 0.01f)
        {
            // Calculate target velocity in the input direction
            Vector2 targetVelocity = playerInput.normalized * maxSpeed;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            // Rotate to face movement direction
            RotateToward(playerInput);
        }
        else
        {
            // Smooth deceleration when no input
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * 0.5f * Time.fixedDeltaTime);
        }
    }

    /// AI movement: moves toward the ball at a slightly reduced speed.
    private void MoveAI()
    {
        if (ball == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Calculate direction to ball
        Vector2 directionToBall = (ball.position - transform.position).normalized;

        // Move toward ball at AI speed
        rb.linearVelocity = directionToBall * aiSpeed;

        // Rotate to face the ball
        RotateToward(directionToBall);
    }

    /// Rotates this vehicle to face a given direction.
    private void RotateToward(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle - 90;
    }

    /// Check if this vehicle is close enough to the ball for auto-switching.
    public bool IsNearBall()
    {
        if (ball == null)
            return false;

        return Vector3.Distance(transform.position, ball.position) <= ballDetectionRadius;
    }

    /// Get distance to the ball.
    public float DistanceToBall()
    {
        if (ball == null)
            return float.MaxValue;

        return Vector3.Distance(transform.position, ball.position);
    }

    /// Enable player control on this vehicle.
    public void SetPlayerControlled(bool controlled)
    {
        isPlayerControlled = controlled;
        playerInput = Vector2.zero;
    }
}
