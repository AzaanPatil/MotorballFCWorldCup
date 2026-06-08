using UnityEngine;

public class Vehicle : MonoBehaviour
{
    public enum Team { Friendly, Opponent }    // Friendly = Home (1), Opponent = Away (0)

    public float speed = 10f;
    public float rotationSpeed = 100f;
    public float ballDetectionRadius = 2f; // Distance to auto-switch control
    public Team team = Team.Friendly;      // Friendly = Home, Opponent = Away

    protected Rigidbody2D rb;
    protected GameManager gameManager;
    protected Transform ball;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
            ball = gameManager.ball;
    }

    protected virtual void Update()
    {
        // Child classes override this
    }

    protected void Move(Vector2 input)
    {
        if (input.sqrMagnitude > 0.01f)
        {
            rb.velocity = input.normalized * speed;

            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            rb.rotation = angle - 90;
        }
        else
        {
            rb.velocity = Vector2.MoveTowards(rb.velocity, Vector2.zero, speed * Time.fixedDeltaTime);
        }
    }

    protected bool IsNearBall()
    {
        if (ball == null)
            return false;

        return Vector3.Distance(transform.position, ball.position) <= ballDetectionRadius;
    }

    protected bool IsActivePlayer()
    {
        if (gameManager == null)
            return false;

        return gameManager.activePlayer == this;
    }

    protected Vehicle[] GetAllVehicles()
    {
        return FindObjectsOfType<Vehicle>();
    }

    protected Vehicle GetNearestOpponent(float maxDistance = float.MaxValue)
    {
        Vehicle[] allVehicles = GetAllVehicles();
        Vehicle nearest = null;
        float closestDist = maxDistance;

        foreach (var vehicle in allVehicles)
        {
            if (vehicle == this || vehicle.team == this.team)
                continue;

            float dist = Vector3.Distance(transform.position, vehicle.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = vehicle;
            }
        }

        return nearest;
    }

    protected Vehicle GetNearestTeammate(float maxDistance = float.MaxValue)
    {
        Vehicle[] allVehicles = GetAllVehicles();
        Vehicle nearest = null;
        float closestDist = maxDistance;

        foreach (var vehicle in allVehicles)
        {
            if (vehicle == this || vehicle.team != this.team)
                continue;

            float dist = Vector3.Distance(transform.position, vehicle.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = vehicle;
            }
        }

        return nearest;
    }
}