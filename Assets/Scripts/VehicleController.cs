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
    public float aiTurnSpeed = 300f; // degrees per second max rotation for AI
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

    // Returns the world-space centre of a goal's BoxCollider2D trigger, not just the transform origin.
    // This allows the GoalA/GoalB root objects to sit at (0,0) while the actual trigger is offset.
    private static Vector2 GoalCenter(Transform t)
    {
        if (t == null) return Vector2.zero;
        var col = t.GetComponent<BoxCollider2D>();
        return col != null ? (Vector2)t.TransformPoint(col.offset) : (Vector2)t.position;
    }
    private Vector2 OwnGoalCenter      => GoalCenter(ownGoal);
    private Vector2 OpponentGoalCenter => GoalCenter(opponentGoal);


    private Rigidbody2D      rb;
    private GameManager      gameManager;
    private GameAudio        gameAudio;
    private Transform        ball;
    private TurretController turret;
    private Vector2 playerInput = Vector2.zero;

    [Header("Collider Sizes (tune per vehicle in Inspector)")]
    public Vector2 carColliderSize          = new(8f,  20f);
    public Vector2 quadColliderSize         = new(5f,  12f);
    public Vector2 monsterTruckColliderSize = new(10f, 24f);
    public Vector2 tankColliderSize         = new(9f,   9f);

    [Header("Engine Sounds")]
    public AudioClip carEngineClip;
    [Range(0f, 1f)] public float carEngineVolume = 0.4f;

    public AudioClip quadEngineClip;
    [Range(0f, 1f)] public float quadEngineVolume = 0.35f;

    public AudioClip monsterTruckEngineClip;
    [Range(0f, 1f)] public float monsterTruckEngineVolume = 0.5f;

    public AudioClip tankEngineClip;
    [Range(0f, 1f)] public float tankEngineVolume = 0.45f;

    private AudioSource engineSource;

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
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.spatialBlend = 0f;
        engineSource.volume = 0.4f;
        gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
            ball = gameManager.ball;
        gameAudio = FindAnyObjectByType<GameAudio>();
        turret = GetComponentInChildren<TurretController>();

        // Hide any child SpriteRenderers that have no sprite assigned (e.g. unset turret pivot)
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
            if (sr.gameObject != gameObject && sr.sprite == null)
                sr.enabled = false;

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
                SetColliderSize(carColliderSize);
                SetEngineSound(carEngineClip, carEngineVolume);
                break;
            case VehicleType.Quad:
                maxSpeed = 22f; acceleration = 80f; aiSpeed = 16f; hitForce = 5f;
                boostMultiplier = 1.4f; maxBoost = 2f; boostDrainRate = 1.5f; boostRechargeRate = 0.3f;
                SetColliderSize(quadColliderSize);
                SetEngineSound(quadEngineClip, quadEngineVolume);
                break;
            case VehicleType.MonsterTruck:
                maxSpeed = 10f; acceleration = 30f; aiSpeed = 8f;  hitForce = 20f;
                boostMultiplier = 2.8f; maxBoost = 5f; boostDrainRate = 0.6f; boostRechargeRate = 0.8f;
                SetColliderSize(monsterTruckColliderSize);
                SetEngineSound(monsterTruckEngineClip, monsterTruckEngineVolume);
                break;
            case VehicleType.Tank:
                maxSpeed = 7f;  acceleration = 25f; aiSpeed = 6f;  hitForce = 25f;
                boostMultiplier = 1f;   maxBoost = 1f; boostDrainRate = 2f;   boostRechargeRate = 0.2f;
                SetColliderSize(tankColliderSize);
                SetEngineSound(tankEngineClip, tankEngineVolume);
                break;
        }
    }

    void SetColliderSize(Vector2 size)
    {
        if (TryGetComponent<CapsuleCollider2D>(out var capsule))
            capsule.size = size;
        else if (TryGetComponent<CircleCollider2D>(out var circle))
            circle.radius = size.x * 0.5f;
        else if (TryGetComponent<BoxCollider2D>(out var box))
            box.size = size;
    }

    void SetEngineSound(AudioClip clip, float volume = 0.4f)
    {
        if (engineSource == null || clip == null) return;
        if (engineSource.clip == clip) return;
        if (gameManager == null || !gameManager.IsGameActive) return;
        engineSource.clip   = clip;
        engineSource.volume = volume;
        engineSource.Play();
    }

    void OnDisable()
    {
        if (engineSource != null) engineSource.Stop();
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

    private bool _aiDiagLogged = false;

    private void MoveAI()
    {
        if (gameManager != null && gameManager.currentState != GameManager.GameState.Playing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // One-shot diagnostic — fires once per vehicle when the game reaches Playing state
        if (!_aiDiagLogged)
        {
            _aiDiagLogged = true;
            string goalInfo   = $"own={(ownGoal      != null ? ownGoal.name      + "@" + OwnGoalCenter      : "NULL ⚠")} " +
                                $"opp={(opponentGoal != null ? opponentGoal.name + "@" + OpponentGoalCenter : "NULL ⚠")}";
            string branchInfo = (isGoalie || position == PlayerPosition.Goalie)
                                ? "→ MoveGoalie"
                                : $"→ {position} switch";
            string stateStr = gameManager != null ? gameManager.currentState.ToString() : "NULL";
            Debug.Log($"[AI-DIAG] {name}: team={team} pos={position} isGoalie={isGoalie} ball={(ball!=null?ball.name:"NULL ⚠")} state={stateStr} {goalInfo} {branchInfo}");
        }

        // Tanks handle their own turret regardless of field position
        if (vehicleType == VehicleType.Tank) UpdateTurretTarget();

        if (isGoalie || position == PlayerPosition.Goalie) { MoveGoalie(); return; }

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

        Vector2 baseDir   = (moveTarget - selfPos).normalized;
        Vector2 direction = (baseDir + TeamSeparation(selfPos) + OpponentAvoidance(selfPos, baseDir)).normalized;
        rb.linearVelocity = direction * aiSpeed;
        RotateToward(direction, smooth: true);
    }

    // Striker: primary chases ball aggressively; others flank to avoid clustering.
    private Vector2 StrikerTarget(Vector2 selfPos, Vector2 ballPos, float distToBall)
    {
        if (opponentGoal == null) return ballPos;

        Vector2 goalPos    = OpponentGoalCenter;
        Vector2 goalToBall = (ballPos - goalPos).normalized;
        Vector2 lateral    = new(-goalToBall.y, goalToBall.x);

        if (!IsPrimaryForBall())
        {
            float side = GetLaneSide();
            return ballPos + lateral * side * 4f + goalToBall * 1.5f;
        }

        // Primary striker: approach ball from our own side, not dead-on toward the opponent
        // This prevents head-on collisions with the opposing primary striker
        Vector2 fromHome = ownGoal != null
            ? (ballPos - OwnGoalCenter).normalized
            : -goalToBall;

        Vector2 behindBall = ballPos + goalToBall * 2f;
        if (distToBall > 0.8f)
        {
            float alignment = Vector2.Dot((ballPos - selfPos).normalized, -goalToBall);
            // Approach from our side of the ball rather than straight through center
            return alignment > 0.5f ? ballPos - fromHome * 0.8f : behindBall;
        }
        return goalPos;
    }

    private bool IsPrimaryForBall()
    {
        if (gameManager == null || gameManager.allVehicles == null) return true;
        float myDist = Vector2.Distance(transform.position, ball.position);
        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v.team != team || v.position != position || v == this) continue;
            if (Vector2.Distance(v.transform.position, ball.position) < myDist - 0.5f) return false;
        }
        return true;
    }

    private float GetLaneSide() => transform.GetSiblingIndex() % 2 == 0 ? 1f : -1f;

    private Vector2 TeamSeparation(Vector2 selfPos)
    {
        Vector2 sep = Vector2.zero;
        if (gameManager == null || gameManager.allVehicles == null) return sep;
        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v == this || v.team != team) continue;
            float dist = Vector2.Distance(selfPos, v.transform.position);
            if (dist < 5f && dist > 0.01f)
                sep += (selfPos - (Vector2)v.transform.position) / dist;
        }
        return sep * 0.9f;
    }

    // Steers perpendicular to our path when an opponent is directly ahead — prevents head-on collisions.
    private Vector2 OpponentAvoidance(Vector2 selfPos, Vector2 moveDir)
    {
        const float avoidRadius = 5f;
        Vector2     steer       = Vector2.zero;
        if (gameManager == null || gameManager.allVehicles == null) return steer;

        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v.team == team) continue;

            Vector2 toOther  = (Vector2)v.transform.position - selfPos;
            float   dist     = toOther.magnitude;
            if (dist > avoidRadius || dist < 0.01f) continue;

            // Only dodge if the opponent is roughly in our forward path
            if (Vector2.Dot(toOther.normalized, moveDir) < 0.2f) continue;

            // Linear falloff: full strength at 0, zero at avoidRadius
            float   strength = Mathf.Clamp01(1f - dist / avoidRadius) * 2f;
            Vector2 perp     = new(-moveDir.y, moveDir.x);
            float   sideSign = Vector2.Dot(toOther, perp) >= 0f ? -1f : 1f;
            steer += perp * sideSign * strength;
        }
        return steer;
    }

    // Midfielder: presses when opponent has ball, cuts passing lanes, supports attack.
    private Vector2 MidfielderTarget(Vector2 selfPos, Vector2 ballPos, float distToBall)
    {
        if (opponentGoal == null || ownGoal == null) return ballPos;

        Vector2 ownGoalPos = OwnGoalCenter;
        Vector2 attackGoal = OpponentGoalCenter;
        Vector2 midfield   = (ownGoalPos + attackGoal) * 0.5f;
        Vector2 attackDir  = (attackGoal - ownGoalPos).normalized;
        Vector2 lateral    = new(-attackDir.y, attackDir.x);

        VehicleController carrier      = GetBallCarrier();
        bool              opponentBall = carrier != null && carrier.team != team;

        // Ball is loose (kickoff / no possessor) — hold position in own half, scaled to field size
        float halfDist = Vector2.Distance(midfield, ownGoalPos);
        if (carrier == null)
        {
            float latOffset = Vector2.Dot(ballPos - midfield, lateral);
            return midfield - attackDir * (halfDist * 0.35f) + lateral * latOffset;
        }

        if (opponentBall)
        {
            // Check for an open opponent BEHIND us we should cut off
            VehicleController openOpponent = FindOpenOpponentBehind(selfPos, attackDir);
            if (openOpponent != null)
            {
                // Position ourselves ON the passing lane between carrier and open opponent
                Vector2 carrierPos  = carrier.transform.position;
                Vector2 receiverPos = openOpponent.transform.position;
                Vector2 passDir     = (receiverPos - carrierPos).normalized;
                float   myProj      = Vector2.Dot(selfPos - carrierPos, passDir);
                myProj = Mathf.Clamp(myProj, 2f, Vector2.Distance(carrierPos, receiverPos) - 1f);
                return carrierPos + passDir * myProj;
            }

            // No open opponent behind us — decide: defend deep or press
            float ballDistToOwnGoal = Vector2.Distance(ballPos, ownGoalPos);

            // Ball is DEEP in defensive zone → drop into defender role
            if (ballDistToOwnGoal < halfDist * 0.65f)
            {
                Vector2 toGoal  = (ownGoalPos - ballPos).normalized;
                float   sideSgn = Vector2.Dot(selfPos - ballPos, lateral) >= 0f ? 1f : -1f;
                return ballPos + toGoal * 1.5f + lateral * sideSgn * 1f;
            }

            // Ball near midfield → press from whichever side we're already on
            if (ballDistToOwnGoal < halfDist * 1.6f)
            {
                float sideSgn = Vector2.Dot(selfPos - ballPos, lateral) >= 0f ? 1f : -1f;
                return ballPos + lateral * sideSgn * 1.5f;
            }

            // Ball far in opponent's half — hold advanced midfield line (scaled)
            float latOff = Vector2.Dot(ballPos - midfield, lateral);
            return midfield - attackDir * (halfDist * 0.35f) + lateral * latOff;
        }
        else
        {
            // Friendly has ball — support attack
            float ballSide = Vector2.Dot(ballPos - midfield, attackDir);
            if (ballSide > 0f)
            {
                if (distToBall < 3f && !IsPrimaryForBall())
                    return ballPos + lateral * GetLaneSide() * 4f;

                // Get into space just behind the striker
                return ballPos - attackDir * 2.5f + lateral * GetLaneSide() * 2f;
            }
            else
            {
                // Ball in own half — push up to midfield line, scaled
                float latOff = Vector2.Dot(ballPos - midfield, lateral);
                return midfield - attackDir * (halfDist * 0.35f) + lateral * latOff;
            }
        }
    }

    // Defender: tackles carriers near goal, blocks passing lanes, holds line.
    private Vector2 DefenderTarget(Vector2 ballPos)
    {
        if (ownGoal == null) return ballPos;

        Vector2 ownGoalPos = OwnGoalCenter;
        Vector2 attackGoal = opponentGoal != null ? OpponentGoalCenter : -ownGoalPos;
        Vector2 midfield   = (ownGoalPos + attackGoal) * 0.5f;
        Vector2 attackDir  = (attackGoal - ownGoalPos).normalized;
        Vector2 lateral    = new(-attackDir.y, attackDir.x);

        VehicleController carrier      = GetBallCarrier();
        bool              opponentBall = carrier != null && carrier.team != team;

        if (opponentBall)
        {
            float carrierDistToGoal = Vector2.Distance(carrier.transform.position, ownGoalPos);

            // PRIORITY 1: Carrier is close to our goal — tackle them
            if (carrierDistToGoal < 8f)
                return carrier.transform.position;

            // PRIORITY 2: Block the most dangerous open passing lane
            VehicleController dangerousTarget = FindMostDangerousPassTarget(carrier);
            if (dangerousTarget != null)
            {
                Vector2 carrierPos  = carrier.transform.position;
                Vector2 receiverPos = dangerousTarget.transform.position;
                return (carrierPos + receiverPos) * 0.5f;
            }

            // PRIORITY 3: Ball in defensive half — get between ball and goal
            float ballSide = Vector2.Dot(ballPos - midfield, attackDir);
            if (ballSide < 0f)
            {
                float distToGoal = Vector2.Distance(ballPos, ownGoalPos);
                if (distToGoal < 5f) return ballPos;
                return ballPos + (ownGoalPos - ballPos).normalized * 1.5f;
            }

            // PRIORITY 4: Hold defensive line (scaled to field size)
            float halfDistD  = Vector2.Distance(midfield, ownGoalPos);
            float latOffset  = Mathf.Clamp(Vector2.Dot(ballPos - midfield, lateral), -halfDistD * 0.3f, halfDistD * 0.3f);
            return midfield - attackDir * (halfDistD * 0.55f) + lateral * latOffset * 0.4f;
        }
        else
        {
            // Friendly has ball
            float halfDistD = Vector2.Distance(midfield, ownGoalPos);
            float ballSide  = Vector2.Dot(ballPos - midfield, attackDir);
            if (ballSide < 0f) return ballPos; // Support clearance in own half

            float latOff = Mathf.Clamp(Vector2.Dot(ballPos - midfield, lateral), -halfDistD * 0.3f, halfDistD * 0.3f);
            return midfield - attackDir * (halfDistD * 0.55f) + lateral * latOff * 0.4f;
        }
    }

    // Goalie: slides laterally along the goal line tracking the ball; turret auto-targets the threat.
    private void MoveGoalie()
    {
        if (ownGoal == null) return;

        Vector2 ownGoalPos = OwnGoalCenter;
        Vector2 attackGoal = opponentGoal != null ? OpponentGoalCenter : -ownGoalPos;
        Vector2 attackDir  = (attackGoal - ownGoalPos).normalized;
        Vector2 lateral    = new(-attackDir.y, attackDir.x);

        Vector2 ballPos = ball != null ? (Vector2)ball.position : ownGoalPos;

        // Scale guard position to field size so it works on any size pitch
        float goalHalfDist = Vector2.Distance((ownGoalPos + attackGoal) * 0.5f, ownGoalPos);
        float lateralClamp = Mathf.Clamp(goalHalfDist * 0.15f, 1.5f, 3f);
        float forwardStep  = Mathf.Clamp(goalHalfDist * 0.12f, 1.2f, 2.5f);

        float ballLateral = Mathf.Clamp(Vector2.Dot(ballPos - ownGoalPos, lateral), -lateralClamp, lateralClamp);
        Vector2 guardPos  = ownGoalPos + attackDir * forwardStep + lateral * ballLateral;

        // Hard clamp: never cross into the attacking half
        Vector2 midfield = (ownGoalPos + attackGoal) * 0.5f;
        float   fwdProj  = Vector2.Dot(guardPos - midfield, attackDir);
        if (fwdProj > 0f) guardPos -= attackDir * fwdProj;

        Vector2 selfPos = transform.position;
        Vector2 moveDir = guardPos - selfPos;

        if (moveDir.magnitude > 0.2f)
        {
            rb.linearVelocity = moveDir.normalized * aiSpeed * 0.75f;
            RotateToward(moveDir, smooth: true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            // Face the ball when stationary
            if (ball != null) RotateToward(ballPos - selfPos, smooth: true);
        }

        // Turret is updated centrally in MoveAI() for all tank types
    }

    private void UpdateTurretTarget()
    {
        if (turret == null) return;

        // Only fire when ball is in our own half — tank acts as a defensive weapon
        if (ball == null || ownGoal == null || opponentGoal == null)
        {
            turret.SetTarget(null);
            return;
        }

        Vector2 ownGoalCenter = OwnGoalCenter;
        Vector2 oppGoalCenter = OpponentGoalCenter;
        Vector2 midfield      = (ownGoalCenter + oppGoalCenter) * 0.5f;
        Vector2 attackDir     = (oppGoalCenter - ownGoalCenter).normalized;

        // Dot < 0 means the ball is on our side of the midfield line
        bool ballInOwnHalf = Vector2.Dot((Vector2)ball.position - midfield, attackDir) < 0f;
        if (!ballInOwnHalf)
        {
            turret.SetTarget(null);
            return;
        }

        // Pick the closer of: nearest opponent vehicle or the ball itself
        float distToBall = Vector2.Distance(transform.position, ball.position);

        VehicleController nearestOpp  = null;
        float             nearestDist = float.MaxValue;
        if (gameManager != null && gameManager.allVehicles != null)
        {
            foreach (var v in gameManager.allVehicles)
            {
                if (v == null || v.team == team) continue;
                float d = Vector2.Distance(transform.position, v.transform.position);
                if (d < nearestDist) { nearestDist = d; nearestOpp = v; }
            }
        }

        turret.SetTarget(
            nearestOpp != null && nearestDist < distToBall
                ? nearestOpp.transform
                : ball
        );
    }

    // Returns the vehicle currently closest to the ball (possessor).
    private VehicleController GetBallCarrier()
    {
        if (gameManager == null || gameManager.allVehicles == null || ball == null) return null;

        VehicleController nearest = null;
        float closestDist = float.MaxValue;

        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v.isGoalie) continue;
            float d = Vector2.Distance(v.transform.position, ball.position);
            if (d < closestDist) { closestDist = d; nearest = v; }
        }
        return closestDist < 4f ? nearest : null;
    }

    // Finds an opponent behind this midfielder with no friendly marking them.
    private VehicleController FindOpenOpponentBehind(Vector2 selfPos, Vector2 attackDir)
    {
        if (gameManager == null || gameManager.allVehicles == null) return null;

        VehicleController mostDangerous = null;
        float             closestToGoal = float.MaxValue;

        foreach (var opp in gameManager.allVehicles)
        {
            if (opp == null || opp.team == team || opp.isGoalie) continue;

            float behindness = Vector2.Dot((Vector2)opp.transform.position - selfPos, -attackDir);
            if (behindness < 1f) continue; // Not behind us

            bool marked = false;
            foreach (var friendly in gameManager.allVehicles)
            {
                if (friendly == null || friendly.team != team || friendly == this) continue;
                if (Vector2.Distance(friendly.transform.position, opp.transform.position) < 3f)
                { marked = true; break; }
            }
            if (marked) continue;

            if (ownGoal == null) continue;
            float distToGoal = Vector2.Distance(opp.transform.position, OwnGoalCenter);
            if (distToGoal < closestToGoal) { closestToGoal = distToGoal; mostDangerous = opp; }
        }
        return mostDangerous;
    }

    // Finds the most dangerous unblocked passing target for a given carrier.
    private VehicleController FindMostDangerousPassTarget(VehicleController carrier)
    {
        if (gameManager == null || gameManager.allVehicles == null || ownGoal == null) return null;

        VehicleController bestTarget  = null;
        float             highestThreat = 0f;
        Vector2           carrierPos  = carrier.transform.position;
        Vector2           ownGoalPos  = OwnGoalCenter;

        foreach (var opp in gameManager.allVehicles)
        {
            if (opp == null || opp == carrier || opp.team == team || opp.isGoalie) continue;

            float distToGoal  = Vector2.Distance(opp.transform.position, ownGoalPos);
            float passDist    = Vector2.Distance(carrierPos, opp.transform.position);
            Vector2 passDir   = ((Vector2)opp.transform.position - carrierPos).normalized;

            // Is there a friendly blocking this pass?
            bool blocked = false;
            foreach (var friendly in gameManager.allVehicles)
            {
                if (friendly == null || friendly.team != team) continue;
                Vector2 toFriendly = (Vector2)friendly.transform.position - carrierPos;
                float   proj       = Vector2.Dot(toFriendly, passDir);
                if (proj > 0f && proj < passDist)
                {
                    float perpDist = Vector2.Distance(carrierPos + passDir * proj, friendly.transform.position);
                    if (perpDist < 2f) { blocked = true; break; }
                }
            }
            if (blocked) continue;

            float threat = (20f - distToGoal) / Mathf.Max(passDist, 1f);
            if (threat > highestThreat) { highestThreat = threat; bestTarget = opp; }
        }
        return highestThreat > 0.5f ? bestTarget : null;
    }


    /// Rotates this vehicle to face a given direction.
    /// smooth=true caps the rotation rate to aiTurnSpeed (used for AI); smooth=false snaps instantly (used for player).
    private void RotateToward(Vector2 direction, bool smooth = false)
    {
        if (direction.sqrMagnitude < 0.01f)
            return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
        if (smooth)
            rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, targetAngle, aiTurnSpeed * Time.fixedDeltaTime);
        else
            rb.rotation = targetAngle;
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
        if (turret != null) turret.SetPlayerControlled(controlled);
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
                if (isBoosting) StartCoroutine(HitStop(0.08f));
                else            StartCoroutine(HitStop(0.04f));
            }
        }
    }

    System.Collections.IEnumerator HitStop(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    public void TrySmartPass()
    {
        if (ball == null || gameManager == null || gameManager.allVehicles == null) return;
        if (Vector2.Distance(transform.position, ball.position) > 2f) return;
        if (!ball.TryGetComponent<Rigidbody2D>(out var ballRb)) return;

        Vector2 attackDir = opponentGoal != null
            ? (OpponentGoalCenter - (Vector2)transform.position).normalized
            : Vector2.right;

        VehicleController bestReceiver = null;
        float             bestScore    = -1f;

        foreach (var v in gameManager.allVehicles)
        {
            if (v == null || v == this || v.team != team || v.isGoalie) continue;

            Vector2 toTeammate = (Vector2)v.transform.position - (Vector2)transform.position;
            float   forwardness = Vector2.Dot(toTeammate.normalized, attackDir);
            if (forwardness < 0.1f) continue; // not ahead of us

            // How far is the nearest opponent from this teammate?
            float openness = float.MaxValue;
            foreach (var opp in gameManager.allVehicles)
            {
                if (opp == null || opp.team == team) continue;
                float d = Vector2.Distance(v.transform.position, opp.transform.position);
                if (d < openness) openness = d;
            }

            float score = forwardness * Mathf.Min(openness, 10f);
            if (score > bestScore) { bestScore = score; bestReceiver = v; }
        }

        if (bestReceiver == null) return;

        if (!bestReceiver.TryGetComponent<Rigidbody2D>(out var receiverRb)) return;
        Vector2 futurePos = (Vector2)bestReceiver.transform.position + receiverRb.linearVelocity * 0.3f;
        Vector2 direction = (futurePos - (Vector2)ball.position).normalized;

        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(direction * passForce, ForceMode2D.Impulse);

        gameManager.BeginPassTracking(bestReceiver);
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
            ? (OpponentGoalCenter - (Vector2)ball.position).normalized
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