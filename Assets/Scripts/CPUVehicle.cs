using UnityEngine;

// CPU-controlled vehicle logic, including opponent checking and friendly support behavior.
public class CPUVehicle : Vehicle
{
    [Header("CPU Settings")]
    public float CPUMoveSpeed = 5f;
    public float checkingDistance = 4f; // How close to engage opponent
    public float protectionDistance = 3f; // How close to protect teammate
    public float laneSearchDistance = 6f; // How far to search for open lanes

    private Vector2 CPUInput = Vector2.zero;

    protected override void Update()
    {
        base.Update();

        // Only use AI if this is NOT the active player.
        if (!IsActivePlayer() && ball != null)
        {
            if (team == Team.Opponent)
            {
                CPUInput = DecideOpponentBehavior();
            }
            else
            {
                CPUInput = DecidefriendlyBehavior();
            }
        }
        else
        {
            CPUInput = Vector2.zero;
        }
    }

    Vector2 DecideOpponentBehavior()
    {
        if (gameManager?.activePlayer == null)
            return ChaseballDirection();

        Vehicle activeOpponent = gameManager.activePlayer;
        float distToOpponent = Vector3.Distance(transform.position, activeOpponent.transform.position);
        float distToBall = Vector3.Distance(transform.position, ball.position);
        float distOpponentToBall = Vector3.Distance(activeOpponent.transform.position, ball.position);

        // Calculate threat score: opponent nearby + has/controls ball
        float threatScore = (checkingDistance - distToOpponent) / checkingDistance + (1f - distOpponentToBall / 10f);
        threatScore = Mathf.Max(0, threatScore);

        // Calculate ball chase score
        float ballScore = (laneSearchDistance - distToBall) / laneSearchDistance;
        ballScore = Mathf.Max(0, ballScore);

        // If threat is stronger than ball opportunity, check the player.
        if (distToOpponent < checkingDistance && threatScore > ballScore)
        {
            // Move toward opponent to check/block
            return DirectionTowards(activeOpponent.transform.position);
        }
        else
        {
            // Chase the ball
            return ChaseballDirection();
        }
    }

    Vector2 DecidefriendlyBehavior()
    {
        if (gameManager?.activePlayer == null)
            return FindOpenLaneDirection();

        Vehicle activePlayer = gameManager.activePlayer;
        Vehicle nearestThreat = GetNearestOpponent(protectionDistance);

        // Protect the active player when an opponent is too close.
        if (nearestThreat != null)
        {
            float distPlayerToThreat = Vector3.Distance(activePlayer.transform.position, nearestThreat.transform.position);

            if (distPlayerToThreat < protectionDistance)
            {
                // Move between threat and active player
                return DirectionTowards(activePlayer.transform.position + (activePlayer.transform.position - nearestThreat.transform.position).normalized * 2f);
            }
        }

        // No immediate threat - find an open lane for passing
        return FindOpenLaneDirection();
    }

    Vector2 FindOpenLaneDirection()
    {
        // Try to move to an open area away from opponents
        Vehicle nearestOpponent = GetNearestOpponent();

        if (nearestOpponent == null)
        {
            // No opponents - move to middle of field
            return new Vector2(0, transform.position.y > 0 ? -1 : 1);
        }

        // Move away from nearest opponent on x-axis, find good y-position
        float dirX = transform.position.x > nearestOpponent.transform.position.x ? 1f : -1f;
        float dirY = transform.position.y > 0 ? -0.5f : 0.5f;
        return new Vector2(dirX, dirY).normalized;
    }

    Vector2 ChaseballDirection()
    {
        // Simple ball chase on x-axis
        float dirX = ball.position.x > transform.position.x ? 1f : -1f;
        return new Vector2(dirX, 0);
    }

    Vector2 DirectionTowards(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        return new Vector2(direction.x, direction.y);
    }

    void FixedUpdate()
    {
        // Apply movement using the CPU's preferred speed.
        float originalSpeed = speed;
        speed = CPUMoveSpeed;
        Move(CPUInput);
        speed = originalSpeed;
    }
}