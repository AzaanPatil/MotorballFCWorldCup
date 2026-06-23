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
    public Transform opponentGoal;
    public Transform ownGoal;


    private Rigidbody2D rb;
    private GameManager gameManager;
    private GameAudio   gameAudio;
    private Transform ball;
    private Vector2 playerInput = Vector2.zero;

    [Header("Boost")]
    public float boostMultiplier = 2f;
    public float boostDrainRate = 1f;
    public float boostRechargeRate = 0.5f;
    public float maxBoost = 3f;
    public ParticleSystem boostParticles;

    private float currentBoost;
    private bool isBoosting = false;

    public float BoostRatio => maxBoost > 0 ? currentBoost / maxBoost : 0f;

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
        gameAudio = FindAnyObjectByType<GameAudio>();

        ApplyVehicleStats();
    }

    // Called by Start() for Inspector-set types, and by GameManager when overriding type from MatchSettings
    public void ApplyVehicleStats()
    {
        switch (vehicleType)
        {
            case VehicleType.Car:
                maxSpeed = 15f; acceleration = 50f; aiSpeed = 12f; hitForce = 10f;
                boostMultiplier = 2f;   maxBoost = 3f; boostDrainRate = 1f;   boostRechargeRate = 0.5f;
                break;
            case VehicleType.Quad:
                maxSpeed = 22f; acceleration = 80f; aiSpeed = 16f; hitForce = 5f;
                boostMultiplier = 1.4f; maxBoost = 2f; boostDrainRate = 1.5f; boostRechargeRate = 0.3f;
                break;
            case VehicleType.MonsterTruck:
                maxSpeed = 10f; acceleration = 30f; aiSpeed = 8f;  hitForce = 20f;
                boostMultiplier = 2.8f; maxBoost = 5f; boostDrainRate = 0.6f; boostRechargeRate = 0.8f;
                break;
            case VehicleType.Tank:
                maxSpeed = 7f;  acceleration = 25f; aiSpeed = 6f;  hitForce = 25f;
                boostMultiplier = 1f;   maxBoost = 1f; boostDrainRate = 2f;   boostRechargeRate = 0.2f;
                break;
        }
    }

    void Update()
    {
        // Only read player input if this vehicle is player-controlled
        if (isPlayerControlled)
        {
            isBoosting = Input.GetKey(KeyCode.Space) && currentBoost > 0f;
            
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

            if (Input.GetKeyDown(KeyCode.LeftShift))
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

            if (boostParticles != null)
            {
                if (isBoosting && !boostParticles.isPlaying) boostParticles.Play();
                if (!isBoosting && boostParticles.isPlaying) boostParticles.Stop();
            }

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

        Vector2 selfPos    = transform.position;
        Vector2 ballPos    = ball.position;
        float   distToBall = Vector2.Distance(selfPos, ballPos);

        Vector2 moveTarget = position switch
        {
            PlayerPosition.Striker    => StrikerTarget(selfPos, ballPos, distToBall),
            PlayerPosition.Midfielder => MidfielderTarget(selfPos, ballPos, distToBall),
            PlayerPosition.Defender   => DefenderTarget(ballPos),
            _                         => ballPos
        };

        Vector2 direction = (moveTarget - selfPos).normalized;
        rb.linearVelocity = direction * aiSpeed;
        RotateToward(direction);
    }

    // Striker: pure aggression — get behind ball and drive toward goal with no hesitation.
    private Vector2 StrikerTarget(Vector2 selfPos, Vector2 ballPos, float distToBall)
    {
        if (opponentGoal == null) return ballPos;

        Vector2 goalPos    = opponentGoal.position;
        Vector2 goalToBall = (ballPos - goalPos).normalized;
        Vector2 behindBall = ballPos + goalToBall * 2f;

        if (distToBall > 0.8f)
        {
            float alignment = Vector2.Dot((ballPos - selfPos).normalized, -goalToBall);
            return alignment > 0.5f ? ballPos : behindBall;
        }
        return goalPos;
    }

    // Midfielder: supports attack when ball is in attacking half; drops to a hold line when defending.
    private Vector2 MidfielderTarget(Vector2 selfPos, Vector2 ballPos, float distToBall)
    {
        if (opponentGoal == null) return ballPos;
        if (ownGoal == null)      return StrikerTarget(selfPos, ballPos, distToBall);

        Vector2 ownGoalPos = ownGoal.position;
        Vector2 attackGoal = opponentGoal.position;
        Vector2 midfield   = (ownGoalPos + attackGoal) * 0.5f;
        Vector2 attackDir  = (attackGoal - ownGoalPos).normalized;

        float ballSide = Vector2.Dot(ballPos - midfield, attackDir);

        if (ballSide > 0f)
        {
            // Ball in attacking half: support the striker but let them lead
            if (distToBall < 3f)
                return StrikerTarget(selfPos, ballPos, distToBall);

            // Hang back slightly behind the ball so we don't crowd the striker
            Vector2 supportPos = ballPos - attackDir * 2f;
            supportPos += new Vector2(-attackDir.y, attackDir.x) *
                          Mathf.Clamp(Vector2.Dot(selfPos - ballPos, new Vector2(-attackDir.y, attackDir.x)), -2f, 2f);
            return supportPos;
        }
        else
        {
            // Ball in defensive half: hold just ahead of midfield, track ball laterally
            if (distToBall < 4f) return ballPos;

            Vector2 lateral   = new(-attackDir.y, attackDir.x);
            float   latOffset = Mathf.Clamp(Vector2.Dot(ballPos - midfield, lateral), -3f, 3f);
            return midfield + attackDir * 1.5f + lateral * latOffset;
        }
    }

    // Defender: physical and territorial — stays behind midfield, shadows opponents near own goal.
    private Vector2 DefenderTarget(Vector2 ballPos)
    {
        if (ownGoal == null) return ballPos;

        Vector2 ownGoalPos = ownGoal.position;
        Vector2 attackGoal = opponentGoal != null ? (Vector2)opponentGoal.position : -ownGoalPos;
        Vector2 midfield   = (ownGoalPos + attackGoal) * 0.5f;
        Vector2 attackDir  = (attackGoal - ownGoalPos).normalized;
        Vector2 lateral    = new(-attackDir.y, attackDir.x);

        float ballSide          = Vector2.Dot(ballPos - midfield, attackDir);
        float ballDistToOwnGoal = Vector2.Distance(ballPos, ownGoalPos);

        // Shadow nearest opponent in defensive zone before chasing ball
        VehicleController threat = NearestOpponentNearGoal(ownGoalPos, 5f);
        if (threat != null && ballSide < 1f)
        {
            // Position between the threat and own goal (physical blocking)
            return ((Vector2)threat.transform.position + ownGoalPos) * 0.5f;
        }

        if (ballSide < 0f)
        {
            // Ball in defensive half
            if (ballDistToOwnGoal < 5f)
            {
                // Ball very close to goal — go for it urgently
                return ballPos;
            }
            else
            {
                // Get between ball and goal
                Vector2 toGoal = (ownGoalPos - ballPos).normalized;
                return ballPos + toGoal * 1.5f;
            }
        }
        else
        {
            // Ball in attacking half — hold defensive line just behind midfield, don't cross
            float   latOffset = Mathf.Clamp(Vector2.Dot(ballPos - midfield, lateral), -3f, 3f);
            Vector2 holdPos   = midfield - attackDir * 1f + lateral * latOffset * 0.4f;
            return holdPos;
        }
    }

    private VehicleController NearestOpponentNearGoal(Vector2 goalPos, float radius)
    {
        if (gameManager == null || gameManager.allVehicles == null) return null;

        VehicleController nearest  = null;
        float             bestDist = radius;

        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v.team == team || v == this || v.isGoalie) continue;
            float d = Vector2.Distance(v.transform.position, goalPos);
            if (d < bestDist) { bestDist = d; nearest = v; }
        }
        return nearest;
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
        if (Time.time - lastPassTime < passCooldown) return;
        if (ball == null) return;
        if (gameManager == null || gameManager.allVehicles == null) return;

        float ballDist = Vector2.Distance(transform.position, ball.position);
        if (ballDist > 2f) return;

        // Find nearest teammate (excluding self and goalies)
        VehicleController nearestTeammate = null;
        float closestDist = float.MaxValue;

        foreach (var v in gameManager.allVehicles)
        {
            if (v == this || v.team != team || v.isGoalie) continue;
            float dist = Vector2.Distance(transform.position, v.transform.position);
            if (dist < closestDist) { closestDist = dist; nearestTeammate = v; }
        }

        if (nearestTeammate == null) return;

        if (!nearestTeammate.TryGetComponent<Rigidbody2D>(out var ballRb2)) return;
        if (!ball.TryGetComponent<Rigidbody2D>(out var ballRb)) return;

        // Lead the pass slightly toward where the teammate is moving
        Vector2 futurePos = (Vector2)nearestTeammate.transform.position + ballRb2.linearVelocity * 0.3f;
        Vector2 direction = (futurePos - (Vector2)ball.position).normalized;

        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(direction * passForce, ForceMode2D.Impulse);

        lastPassTime = Time.time;

        // Hand off control once the ball reaches the receiver
        gameManager.BeginPassTracking(nearestTeammate);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        // Vehicle-to-vehicle collision sound
        if (col.gameObject.TryGetComponent<VehicleController>(out _))
        {
            if (gameAudio != null) gameAudio.PlayVehicleCollision();
        }

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