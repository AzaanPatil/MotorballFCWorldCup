using UnityEngine;

/// Unified vehicle controller that handles both player-controlled and AI movement.
/// - If isPlayerControlled = true: responds to WASD / arrow key input
/// - If isPlayerControlled = false: AI moves toward the ball
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    public enum Team { 
        Friendly, 
        Opponent 
    }
    
    [Header("Team")]
    public Team team;

    public enum VehicleType
    {
        Car,
        MonsterTruck,
        Quad,
        Tank
    }

    public enum PlayerPosition { Striker, Midfielder, Defender, Goalie }

    [Header("Vehicle Type")]
    public VehicleType vehicleType;
    
    [Header("Hit")]
    public float hitForce = 10f;
    public float hitCooldown = 0.5f;
    private float lastHitTime = -10f;
    
    [Header("Movement")]
    public float maxSpeed = 15f;
    public float acceleration = 50f;
    public float aiSpeed = 12f;
    public float ballDetectionRadius = 3f;

    [Header("Input")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";

    [Header("Control")]
    public bool isPlayerControlled = false;
    public bool isGoalie = false;

    [Header("Position")]
    public PlayerPosition position = PlayerPosition.Striker;

    [Header("AI Goals")]
    public Transform opponentGoal; // Assign the opponent's goal Transform so AI kicks toward it

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

    [Header("Passing")]
    public float passForce = 8f;
    public float passCooldown = 0.5f;
    private float lastPassTime = -10f;

    void Start()
    {
        currentBoost = maxBoost;
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
            ball = gameManager.ball;

        ApplyVehicleStats();
    }

    // Called by Start() for Inspector-set types, and by GameManager when overriding type from MatchSettings
    public void ApplyVehicleStats()
    {
        switch (vehicleType)
        {
            case VehicleType.Car:
                maxSpeed = 15f; acceleration = 50f; aiSpeed = 12f; hitForce = 10f;
                break;
            case VehicleType.Quad:
                maxSpeed = 22f; acceleration = 80f; aiSpeed = 16f; hitForce = 5f;
                break;
            case VehicleType.MonsterTruck:
                maxSpeed = 10f; acceleration = 30f; aiSpeed = 8f;  hitForce = 20f;
                break;
            case VehicleType.Tank:
                maxSpeed = 7f;  acceleration = 25f; aiSpeed = 6f;  hitForce = 25f;
                break;
        }
    }

    void Update()
    {
        // Only read player input if this vehicle is player-controlled
        if (isPlayerControlled)
        {
            isBoosting = Input.GetKey(KeyCode.LeftShift) && currentBoost > 0f;
            
            playerInput = Vector2.zero;

            if (Input.GetKey(KeyCode.UpArrow)    || Input.GetKey(KeyCode.W)) playerInput.y += 1;
            if (Input.GetKey(KeyCode.DownArrow)  || Input.GetKey(KeyCode.S)) playerInput.y -= 1;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) playerInput.x += 1;
            if (Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A)) playerInput.x -= 1;

            // Auto-switch: if the player is far from this vehicle, allow switching
            //if (IsNearBall() && gameManager != null)
            //{
            //    gameManager.SetActivePlayer(this);
            //}

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TryHit();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryPass();
            }
        }
    }

    void FixedUpdate()
    {
        // Cancel any spin imparted by collisions — rotation is set directly via RotateToward
        rb.angularVelocity = 0f;

        if (isPlayerControlled)
            MovePlayer();
        else
            MoveAI();
    }

    private void MovePlayer()
    {
        if (playerInput.sqrMagnitude > 0.01f)
        {
            if (isBoosting)
                currentBoost -= boostDrainRate * Time.fixedDeltaTime;
            else
                currentBoost += boostRechargeRate * Time.fixedDeltaTime;
            currentBoost = Mathf.Clamp(currentBoost, 0f, maxBoost);

            // Direct velocity assignment: car instantly goes the direction you press
            float speed = isBoosting ? maxSpeed * boostMultiplier : maxSpeed;
            rb.linearVelocity = playerInput.normalized * speed;
            RotateToward(playerInput);
        }
        else
        {
            currentBoost = Mathf.Min(currentBoost + boostRechargeRate * Time.fixedDeltaTime, maxBoost);
            // Decelerate to stop when no input
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
        }
    }

    private void MoveAI()
    {
        // Goalies have their own movement script (Tank.cs) — don't override it
        if (isGoalie) return;

        if (gameManager != null && gameManager.currentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (ball == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distToBall = Vector2.Distance(transform.position, ball.position);
        Vector2 moveTarget;

        if (distToBall > 1.5f)
        {
            // Chase the ball
            moveTarget = ball.position;
        }
        else if (opponentGoal != null)
        {
            // Close to ball: drive through it toward the goal so it gets pushed there
            moveTarget = opponentGoal.position;
        }
        else
        {
            // No goal assigned: keep pushing the ball in the same direction we arrived from
            moveTarget = (Vector2)ball.position + (Vector2)(ball.position - transform.position).normalized * 3f;
        }

        Vector2 direction = (moveTarget - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * aiSpeed;
        RotateToward(direction);
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

        if (distance < 2.5f)
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

    void TryPass()
    {
        if (Time.time - lastPassTime < passCooldown)
            return;

        if (ball == null)
            return;

        // Find nearest teammate
        VehicleController[] allVehicles = FindObjectsByType<VehicleController>();

        VehicleController nearestTeammate = null;
        float closestDist = float.MaxValue;

        foreach (var v in allVehicles)
        {
            if (v == this) continue;
            if (v.team != this.team) continue;

            float dist = Vector2.Distance(transform.position, v.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                nearestTeammate = v;
            }
        }

        if (nearestTeammate == null)
            return;

        // Make sure we're close enough to the ball
        float ballDist = Vector2.Distance(transform.position, ball.position);
        if (ballDist > 2f)
            return;

        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null)
            return;

        // Pass toward teammate
        Rigidbody2D teammateRb = nearestTeammate.GetComponent<Rigidbody2D>();

        Vector2 futurePos = (Vector2)nearestTeammate.transform.position;

        if (teammateRb != null)
        {
            futurePos += teammateRb.linearVelocity * 0.3f;
        }

        Vector2 direction = (futurePos - (Vector2)ball.position).normalized;

        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(direction * passForce, ForceMode2D.Impulse);

        lastPassTime = Time.time;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (ball == null || col.transform != ball) return;

        Rigidbody2D ballRb = col.rigidbody;
        if (ballRb == null) return;

        // AI kicks toward the opponent goal; player collision just deflects naturally
        Vector2 direction = (!isPlayerControlled && opponentGoal != null)
            ? ((Vector2)opponentGoal.position - (Vector2)ball.position).normalized
            : ((Vector2)ball.position - (Vector2)transform.position).normalized;

        float force = hitForce * 0.4f;
        if (isBoosting) force *= 1.5f;
        ballRb.AddForce(direction * force, ForceMode2D.Impulse);
    }

    public void SetTeamColor(Color color)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
    }
}