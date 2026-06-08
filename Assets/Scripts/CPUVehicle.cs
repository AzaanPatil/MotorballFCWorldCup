using UnityEngine;

public class CPUVehicle : Vehicle
{
    public Transform ball; // Reference to the ball's transform
    public float moveSpeed = 5f; // Speed at which the CPU moves

    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (ball != null)
        {
            // Move towards the ball's position on the x-axis
            Vector3 direction = new Vector3(ball.position.x, transform.position.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, direction, moveSpeed * Time.deltaTime);
        }
    }
}