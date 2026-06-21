using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AI : MonoBehaviour
{
    [Header("Behaviour")]
    public bool isGoalie = false;
    public float defendRadius = 5f; // Goalie stays at own goal until ball comes within this distance

    [Header("Goals")]
    public Transform ownGoal;      // Goal this vehicle defends
    public Transform opponentGoal; // Goal this vehicle attacks (used for clearing/shooting)

    public Transform ball;
    public float speed = 12f;
    public float acceleration = 50f;

    private Rigidbody2D rb;

    void Start()
    {
        // VehicleController handles all movement when present — don't fight it
        if (TryGetComponent<VehicleController>(out _)) { enabled = false; return; }

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        // Auto-find ball from GameManager if not assigned in Inspector
        if (ball == null)
        {
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) ball = gm.ball;
        }
    }

    void FixedUpdate()
    {
        rb.angularVelocity = 0f;

        if (ball == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isGoalie)
        {
            MoveGoalie();
            return;
        }

        // Field player: always chase the ball — simple and avoids midfield zigzagging
        Move(ball.position);
        TryPlayBall();
    }

    void MoveGoalie()
    {
        if (ownGoal == null) return;

        float distBallToGoal = Vector2.Distance(ball.position, ownGoal.position);

        // Stay at goal when ball is far; charge to intercept when ball gets close
        Vector2 targetPosition = distBallToGoal > defendRadius ? ownGoal.position : ball.position;

        Move(targetPosition);
        TryPlayBall();
    }

    void Move(Vector2 targetPosition)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 targetVelocity = direction * speed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle + 90;
    }

    void TryPlayBall()
    {
        if (ball == null || opponentGoal == null) return;

        float distance = Vector2.Distance(transform.position, ball.position);
        if (distance > 2f) return;

        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null) return;

        Vector2 toGoal = (opponentGoal.position - ball.position).normalized;

        // Try to pass to a teammate if one is in a better position
        VehicleController teammate = GetNearestTeammate();
        if (teammate != null)
        {
            float teammateDist = Vector2.Distance(transform.position, teammate.transform.position);
            if (teammateDist < 6f && teammate.transform.position.x > transform.position.x)
            {
                Vector2 futurePos = (Vector2)teammate.transform.position;
                if (teammate.TryGetComponent<Rigidbody2D>(out var teammateRb))
                    futurePos += teammateRb.linearVelocity * 0.3f;

                Vector2 passDir = (futurePos - (Vector2)ball.position).normalized;
                ballRb.linearVelocity = Vector2.zero;
                ballRb.AddForce(passDir * 6f, ForceMode2D.Impulse);
                return;
            }
        }

        // Shoot/clear toward opponent goal
        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(toGoal * 8f, ForceMode2D.Impulse);
    }

    VehicleController GetNearestTeammate()
    {
        if (!TryGetComponent<VehicleController>(out var myVC)) return null;

        VehicleController[] all = FindObjectsByType<VehicleController>();
        VehicleController closest = null;
        float closestDist = float.MaxValue;

        foreach (var v in all)
        {
            if (v.transform == transform || v.team != myVC.team) continue;
            float dist = Vector2.Distance(transform.position, v.transform.position);
            if (dist < closestDist) { closestDist = dist; closest = v; }
        }

        return closest;
    }
}
