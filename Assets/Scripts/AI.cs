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

        TryShoot();
    }

    void TryShoot()
    {
        if (ball == null) return;

        float distance = Vector2.Distance(transform.position, ball.position);

        if (distance < 2f)
        {
            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                Vector2 directionToGoal = (opponentGoal.position - ball.position).normalized;

                ballRb.AddForce(directionToGoal * 8f, ForceMode2D.Impulse);
            }
        }
    }
}