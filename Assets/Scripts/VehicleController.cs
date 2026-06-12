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

    [Header("Hit")]
    public float hitForce = 10f;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -10f;
    
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

    [Header("Boost")]
    public float boostMultiplier = 2f;
    public float boostDrainRate = 1f;
    public float boostRechargeRate = 0.5f;
    public float maxBoost = 3f;

    private float currentBoost;
    private bool isBoosting = false;

    void Start()
    {
        currentBoost = maxBoost;
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
            isBoosting = Input.GetKey(KeyCode.LeftShift) && currentBoost > 0f;
            
            playerInput = Vector2.zero;

            if (Input.GetKey(KeyCode.UpArrow)) playerInput.y += 1;
            if (Input.GetKey(KeyCode.DownArrow)) playerInput.y -= 1;
            if (Input.GetKey(KeyCode.RightArrow)) playerInput.x += 1;
            if (Input.GetKey(KeyCode.LeftArrow)) playerInput.x -= 1;

            // Auto-switch: if the player is far from this vehicle, allow switching
            //if (IsNearBall() && gameManager != null)
            //{
            //    gameManager.SetActivePlayer(this);
            //}

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryHit();
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
            float speed = maxSpeed;

            if (isBoosting)
            {
                speed *= boostMultiplier;
                currentBoost -= boostDrainRate * Time.deltaTime;
                rb.AddForce(playerInput.normalized * 2f, ForceMode2D.Force);
            }
            else
            {
                currentBoost += boostRechargeRate * Time.deltaTime;
            }

            currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);
            
            // Calculate target velocity in the input direction
            Vector2 targetVelocity = playerInput.normalized * maxSpeed;
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

            // Rotate to face movement direction
            RotateToward(playerInput);
        }
        else
        {
            // Smooth deceleration when no input
            rb.linearVelocity = Vector2.MoveTowards(
                rb.linearVelocity,
                Vector2.zero,
                acceleration * 0.1f * Time.fixedDeltaTime
            );
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
        rb.rotation = angle + 90;
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


    void TryHit()
    {
        if (Time.time - lastHitTime < hitCooldown)
            return;

        if (ball == null)
            return;

        float distance = Vector2.Distance(transform.position, ball.position);

        // Only hit if close enough
        if (distance < 2f)
        {
            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                Vector2 forward = transform.up;
                Vector2 toBall = (ball.position - transform.position).normalized;

                // Blend forward direction with ball direction
                Vector2 direction = (forward + toBall).normalized;

                // Boost synergy 🔥
                float force = hitForce;
                if (isBoosting)
                    force *= 1.5f;

                ballRb.AddForce(direction * force, ForceMode2D.Impulse);
                rb.AddForce(-direction * 2f, ForceMode2D.Impulse);
                
                lastHitTime = Time.time;
            }
        }
    }
}