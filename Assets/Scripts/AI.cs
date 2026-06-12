using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class AI : MonoBehaviour
{
    [Header("Goals")]
    public Transform ownGoal;
    public Transform opponentGoal;
    
    public Transform ball;
    public float speed = 20f;
    public float acceleration = 40f;

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (ball == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 targetPosition;

        float ballX = ball.position.x;

        // 🛡 DEFEND
        if (ballX < 0)
        {
            targetPosition = ownGoal.position;
        }
        // ⚔ ATTACK
        else
        {
            targetPosition = ball.position;
        }

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        Vector2 targetVelocity = direction * speed;

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        // Rotate
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle + 90;

        TryPlayBall();
    }

    void TryPlayBall()
    {
        if (ball == null) return;

        float distance = Vector2.Distance(transform.position, ball.position);

        if (distance > 2f) return;

        Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
        if (ballRb == null) return;

        Vector2 toGoal = (opponentGoal.position - ball.position).normalized;

        // 🧠 Check for teammate
        VehicleController teammate = GetNearestTeammate();

        if (teammate != null)
        {
            float teammateDist = Vector2.Distance(transform.position, teammate.transform.position);

            // 🎯 PASS if teammate is closer to goal
            if (teammateDist < 6f && teammate.transform.position.x > transform.position.x)
            {
                Rigidbody2D teammateRb = teammate.GetComponent<Rigidbody2D>();

                Vector2 futurePos = (Vector2)teammate.transform.position;

                if (teammateRb != null)
                {
                    futurePos += teammateRb.linearVelocity * 0.3f;
                }

                Vector2 passDir = (futurePos - (Vector2)ball.position).normalized;
                
                ballRb.linearVelocity = Vector2.zero;
                ballRb.AddForce(passDir * 6f, ForceMode2D.Impulse);

                return;
            }
        }

        // ⚽ Otherwise SHOOT
        ballRb.linearVelocity = Vector2.zero;
        ballRb.AddForce(toGoal * 8f, ForceMode2D.Impulse);
    }

    VehicleController GetNearestTeammate()
    {
        VehicleController[] all = FindObjectsOfType<VehicleController>();

        VehicleController closest = null;
        float closestDist = float.MaxValue;

        foreach (var v in all)
        {
            if (v.transform == transform) continue;
            if (v.team != GetComponent<VehicleController>().team) continue;

            float dist = Vector2.Distance(transform.position, v.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = v;
            }
        }

        return closest;
}
}