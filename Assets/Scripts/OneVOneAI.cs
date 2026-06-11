using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OneVOneAI : MonoBehaviour
{
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

        if (Vector2.Distance(transform.position, ball.position) < 0.5f)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Direction toward ball
        Vector2 direction = (ball.position - transform.position).normalized;

        // Move using physics
        Vector2 targetVelocity = direction * speed;

        rb.linearVelocity = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime // acceleration value
        );

        // Rotate toward ball
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.rotation = angle + 90; // adjust depending on sprite orientation
    }
}