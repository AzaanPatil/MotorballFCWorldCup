using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public float speed = 10f; // Speed of the vehicle
    public float rotationSpeed = 100f; // Turning speed of the vehicle

    protected Rigidbody2D rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        rb.MovePosition(rb.position + input * speed * Time.fixedDeltaTime);

        if (input != Vector2.zero)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            rb.rotation = angle - 90;
        }
    }
}