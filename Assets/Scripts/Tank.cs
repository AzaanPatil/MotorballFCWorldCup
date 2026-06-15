using UnityEngine;

public class Tank : Vehicle
{
    public Transform turret;

    [Header("Goalie Settings")]
    public Transform ownGoal;
    public float defendRadius = 5f;
    public float engageRadius = 4f;
    public float moveSpeed = 4f;

    protected override void Update()
    {
        RotateTurret();
    }

    void FixedUpdate()
    {
        if (ball == null || ownGoal == null) return;

        float distToBall = Vector2.Distance(transform.position, ball.position);
        float distBallToGoal = Vector2.Distance(ball.position, ownGoal.position);

        Vector2 targetPosition;

        // Stay near goal normally
        if (distBallToGoal > defendRadius)
        {
            targetPosition = ownGoal.position;
        }
        else
        {
            // Engage when ball is close
            targetPosition = ball.position;
        }

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * moveSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle - 90;

        if (distToBall < engageRadius)
        {
            Rigidbody2D ballRb = ball.GetComponent<Rigidbody2D>();
            if (ballRb != null)
            {
                Vector2 clearDirection = (ball.position - ownGoal.position).normalized;

                ballRb.linearVelocity = Vector2.zero;
                ballRb.AddForce(clearDirection * 10f, ForceMode2D.Impulse);
            }
        }
    }

    void RotateTurret()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePos - turret.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        turret.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
}   